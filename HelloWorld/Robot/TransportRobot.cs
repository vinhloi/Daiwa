using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Daiwa
{
    public class TransportRobot : Robot
    {
        public const int _maxItem = 5;
        public Queue<string> _loadedItems;
        public Queue<string> _expectedReceiveItems;
        public bool _isLoading = false;
        public bool _isUnloading = false;

        public TransportRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _loadedItems = new Queue<string>();
            _expectedReceiveItems = new Queue<string>();
            type = "trans";
        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = _id.ToString(); // add id at sec 0
            }

            // when picking, stop 1 tile before the pickup point
            if (_path.Count == 1 && 
                _path.Peek().Equals(_destination_point)
                && (_state == robot_state.pick || _state == robot_state.slot))
            {
                _path.Pop();
            }

            if (_state == robot_state.free) // no action
            {
                _actionString += " n";
            }          
            else if (_path.Count > 0) // moving
            {
                Byte robot_id = Warehouse.ValueAt(_path.Peek());
                if (robot_id == 0) // No robot standing at this tile, road is clear
                {
                    MoveToNextTile();
                }
                else // new location is obstructed
                {
                    Robot another_robot = Warehouse._AllMovingRobots[robot_id];
                    if (another_robot._path.Count == 0)// anther robot is stopping
                    {
                        Reroute();
                    }
                    else // anther robot is moving
                    {
                        if (IsFacing(another_robot))
                        {
                            if (another_robot.Reroute() == false)
                                this.Reroute();
                        }
                    }
                    _actionString += " n";//vinh: should check if 2 robot facing each other
                }
            }
            else // we arrive at the destination
            {
                if (_state == robot_state.returning)
                {
                    if (Rotate(Direction.Up) == false)
                    {
                        _state = robot_state.free;
                        _actionString += " n";
                    }
                }
                else
                {
                    _actionString += " n";
                }
            }
        }

        public override bool Reroute()
        {
            switch(_state)
            {
                case robot_state.pick:
                case robot_state.slot:
                    return FindNewRouteToPick();
                case robot_state.ship:
                    if (_path.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        _path = AStarPathfinding.FindPath(_location, _destination_point);
                        return true;
                    }
                case robot_state.returning:
                    if (_path.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        _path = AStarPathfinding.FindPath(_location, _chargingPoint);
                        return true;
                    }
                default:
                    return false;
            }
        }

        public bool FindNewRouteToPick()
        {
            int x = _destination_point.X;
            int y = _destination_point.Y;

            var proposedLocations = new List<Point>()
            {
                new Point ( x - 1, y ),
                new Point ( x + 1, y ),
                new Point ( x, y - 1 ),
                new Point ( x, y + 1 ),
            };

            foreach (Point newgoal in proposedLocations)
            {
                if (Warehouse.ValueAt(newgoal) == 0)
                {
                    _path = AStarPathfinding.FindPath(_location, newgoal);
                    return true;
                }
            }
            return false;
        }

        public void PrepareToShip()
        {
            Product product = new Product(Warehouse._DicItems[_loadedItems.Peek()]);
            ShippingRobot shipper = (ShippingRobot)Warehouse._Shippers[product._shipperID];
            _destination_point = shipper.GetShipPoint();

            _path = AStarPathfinding.FindPath(_location, _destination_point);
            _state = robot_state.ship;
        }

        public void UpdateExpectedReceiveItem(Order order)
        {
            for (int i = _expectedReceiveItems.Count; i < TransportRobot._maxItem; i++)
            {
                if (order._quantity == 0)
                    break;

                _expectedReceiveItems.Enqueue(order._productID);
                order._quantity--;
            }
        }

        public void PrepareToReceive()
        {
            _destination_point = Warehouse._Receiver.GetReceivePoint(); ;
            _path = AStarPathfinding.FindPath(_location, _destination_point);
            _state = robot_state.receive;
        }

        public override void PrepareToReturn()
        {
            if (_state != robot_state.returning && _state != robot_state.free && _isLoading == false && _isUnloading == false)
            {
                _path = AStarPathfinding.FindPath(_location, _chargingPoint);
                _state = robot_state.returning;
                _destination_point = _chargingPoint;
            }
        }

        public override void ForceReturnChargingPoint()
        {
            if (_state != robot_state.returning && _state != robot_state.free && _isLoading == false && _isUnloading == false)
            {
                Program.Print("Forceb " + _id + " " + _state + " " + _destination_point + "\n");
                _path = AStarPathfinding.FindPath(_location, _chargingPoint);
                _backUpState = _state;
                _state = robot_state.returning;
            }
        }

        public void FinishReceiving()
        {
            string item = _expectedReceiveItems.Dequeue();
            _loadedItems.Enqueue(item);
            _isLoading = false;
            if (_expectedReceiveItems.Count == 0)
            {
                Rack rack = Warehouse.FindRackToSlot(_loadedItems.Peek(), _loadedItems.Count);
                if(rack == null)
                {
                    Program.Print("Racks are full");
                    return;
                }

                //Program.Print(rack.GetXXYYDH() + " " + rack._num_items + " " + rack._expectedSlotQuantity + " " + rack._max_item + "\n");

                PrepareToSlot(rack.GetPickUpPoint(), rack, _loadedItems.Count);

                Robot picker = Warehouse.FindPickerToPick(rack);
                if(picker == null)
                {
                    Program.Print("can not find picker"); // bug here. or we increase number of picker?
                    return;
                }
                picker.PrepareToSlot(rack.GetPickUpPoint(), rack, _loadedItems.Count);
            }
        }

        public void FinishShipping()
        {
            _loadedItems.Dequeue();
            _isUnloading = false;
            if (_loadedItems.Count == 0) // finish shipping
            {
                if (_order._quantity == 0) // no more item to pick, return to charging point
                    PrepareToReturn();
                else
                {
                    _state = robot_state.pick; // return to rack and continue loading item to ship
                    FindNewRouteToPick();
                }
            }
        }

        public void FinishPicking()
        {
            _isLoading = false;
            _loadedItems.Enqueue(_order._productID);
            _order._quantity--;

            if (IsFull())
            {
                PrepareToShip();
            }

        }

        public void FinishSlotting()
        {
            _isUnloading = false;
            _loadedItems.Dequeue();
            _order._quantity--;

            if (_loadedItems.Count == 0)
            {
                PrepareToReturn();
            }

        }

        public bool IsFull()
        {
            return (_loadedItems.Count >= _maxItem) ? true : false;
        }
    }
}
