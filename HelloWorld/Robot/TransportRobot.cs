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
                if (_noPath)
                {
                    Program.Print("Refind path from " + _location + " to " + _destination_point + "\n");
                    _path = AStarPathfinding.FindPath(_location, _destination_point, out _noPath);
                }
            }

            if (_noPath)
            {
                _actionString += " n";
                return;
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
                    if (Rotate() == true)
                        return;
                    MoveToNextTile();
                }
                else // new location is obstructed
                {
                    _actionString += " n";
                    Robot another_robot = Warehouse._AllMovingRobots[robot_id];
                    if (another_robot._state == robot_state.free || another_robot._path.Count == 0)
                    {
                        Program.Print(_id + " is obstructed by " + robot_id + " at " + _path.Peek() + "\n");
                        Reroute();
                    }
                    else if (IsCollideWith(another_robot))
                    {
                        Program.Print(_id + " is collide with " + robot_id + " at " + _path.Peek() + "\n");
                        if (another_robot._state != robot_state.slot && another_robot._state != robot_state.pick)
                            another_robot.Reroute();
                        else
                            Reroute();
                    }
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
            if (_isLoading == true || _isUnloading == true)
                return false;

            switch(_state)
            {
                case robot_state.pick:
                case robot_state.slot:
                    _path = AStarPathfinding.FindPath(_location, _destination_point, out _noPath);
                    if(_path.Count > 0)
                        return true;
                    return false;
                case robot_state.ship:
                    if (_path.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        Program.Print(_id + "ship reroute to " + _destination_point + "\n");
                        PrepareToShip();
                        return true;
                    }
                case robot_state.receive:
                    if (_path.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        Program.Print(_id + "recieve reroute to " + _destination_point + "\n");
                        PrepareToReceive();
                        return true;
                    }
                case robot_state.returning:
                    if (_path.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        Program.Print(_id + "return reroute to " + _chargingPoint + "\n");
                        _path = AStarPathfinding.FindPath(_location, _chargingPoint, out _noPath);
                        return true;
                    }
                default:
                    Program.Print("Reroute: Unkown state");
                    return false;
            }
        }

        public bool LeavePathForPicker()
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
                    Program.PrintLine(_id + "pick or slot reroute to " + newgoal);
                    _path = AStarPathfinding.FindPath(_location, newgoal, out _noPath);
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

            _path = AStarPathfinding.FindPath(_location, _destination_point, out _noPath);
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
            _destination_point = Warehouse._Receiver.GetReceivePoint();
            _path = AStarPathfinding.FindPath(_location, _destination_point, out _noPath);
            _state = robot_state.receive;
        }

        public override void PrepareToReturn()
        {
            if (_state != robot_state.returning && _state != robot_state.free && _isLoading == false && _isUnloading == false)
            {
                _path = AStarPathfinding.FindPath(_location, _chargingPoint, out _noPath);
                _state = robot_state.returning;
                _destination_point = _chargingPoint;
            }
        }

        public override void ForceReturnChargingPoint()
        {
            if (_state != robot_state.returning && _state != robot_state.free && _isLoading == false && _isUnloading == false)
            {
                Program.Print("Force return " + _id + " " + _state + " " + _destination_point + "\n");
                _path = AStarPathfinding.FindPath(_location, _chargingPoint, out _noPath);
                if (_noPath == true)
                    return;
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
                    _destination_point = _order._rack.GetPickUpPoint();
                    _path = AStarPathfinding.FindPath(_location, _destination_point, out _noPath);
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
