using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Daiwa
{
    public class PickingRobot : Robot
    {
        protected int _pickingTime;
        protected TransportRobot transporter;

        public PickingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _pickingTime = 0;
            transporter = null;
            type = "picker";
        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = _id.ToString(); // add id at sec 0
                if(_noPath)
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

            if (_state == robot_state.free) // no action
            {
                _actionString += " n";
            }
            else if (_path.Count > 0) // moving
            {
                if (Rotate() == true)
                    return;

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
                        if (Warehouse._Transporters.ContainsKey(another_robot._id))
                            another_robot.Reroute();
                        else
                            this.Reroute();
                    }
                    else // another robot is moving
                    {
                        if (IsCollideWith(another_robot))
                        {
                            if (another_robot.Reroute() == false)
                                this.Reroute();
                        }
                    }

                    _actionString += " n";
                }
            }
            else // we arrive at the destination
            {
                if (_state == robot_state.returning) // robot return to charging point
                {
                    if (Rotate(Direction.Up) == false) // rotate to upward position
                    {
                        _state = robot_state.free;
                        _actionString += " n";
                    }
                }
                else if (_state == robot_state.pick && _order._quantity > 0)
                {
                    Pick(sec);
                }
                else if (_state == robot_state.slot && _order._quantity > 0)
                {
                    Slot(sec);
                }
            }
        }

        public override void PrepareToReturn()
        {
            if (_state != robot_state.returning && _state != robot_state.free && _pickingTime == 0)
            {
                _path = AStarPathfinding.FindPath(_location, _chargingPoint, out _noPath);
                _state = robot_state.returning;
                _destination_point = _chargingPoint;
            }
        }

        public override void ForceReturnChargingPoint()
        {
            if (_state != robot_state.returning && _state != robot_state.free && _pickingTime == 0)
            {
                Program.Print("Forceb " + _id + " " + _state + " " + _destination_point + "\n");
                _path = AStarPathfinding.FindPath(_location, _chargingPoint, out _noPath);
                _destination_point = _chargingPoint;
                _backUpState = _state;
                _state = robot_state.returning;
            }
        }

        protected void Pick(int sec)
        {
            if (_pickingTime < 9)
            {
                if (_pickingTime == 0) // start picking
                {
                    if (sec + 10 > 59)
                    {
                        _actionString += " n";
                        return;
                    }

                    transporter = GetAdjacentTransporter();
                    if (transporter == null)
                    {
                        _actionString += " n";
                        return;
                    }

                    _actionString = _actionString + " p " + transporter._id + " " + _order._rack.GetXXYYDH() + " " + _order._productID;
                    transporter._isLoading = true;
                }
                _pickingTime++;
            }
            else // finish picking
            {
                _pickingTime = 0;
                _order._quantity--;
                _order._rack.RemoveItem(_order._productID);
                transporter.FinishPicking();
                if (_order._quantity == 0)
                {
                    transporter.PrepareToShip();
                    PrepareToReturn();
                }
            }
        }

        protected void Slot(int sec)
        {
            if (_pickingTime < 9)
            {
                if (_pickingTime == 0) // start picking
                {
                    if (sec + 10 > 59)
                    {
                        _actionString += " n";
                        return;
                    }

                    transporter = GetAdjacentTransporter();
                    if (transporter == null)
                    {
                        _actionString += " n";
                        return;
                    }

                    _actionString = _actionString + " s " + transporter._id + " " + _order._rack.GetXXYYDH() + " " + transporter._loadedItems.Peek();
                    transporter._isUnloading = true;
                }
                _pickingTime++;
            }
            else // finish slot
            {
                _pickingTime = 0;
                _order._quantity--;
                _order._rack.AddItem(_order._productID);
                transporter.FinishSlotting();
                if (_order._quantity == 0)
                {
                    PrepareToReturn();
                }
            }
        }

        protected TransportRobot GetAdjacentTransporter()
        {
            int x = _location.X;
            int y = _location.Y;

            var proposedLocations = new List<Point>()
            {
                new Point ( x - 1, y ),
                new Point ( x + 1, y ),
                new Point ( x, y - 1 ),
                new Point ( x, y + 1 ),
            };

            foreach (Point location in proposedLocations)
            {
                Byte id = Warehouse.ValueAt(location);
                if (id > 9 && Warehouse._Transporters.ContainsKey(id))
                {
                    TransportRobot robot = (TransportRobot)Warehouse._Transporters[id];
                    if (robot._path.Count == 0 && robot._state == this._state)
                        return robot;
                }
            }

            return null;
        }

        public override bool Reroute()
        {
            switch (_state)
            {
                case robot_state.pick:
                case robot_state.slot:
                    if (_path.Count == 0)
                        return false;
                    _path = AStarPathfinding.FindPath(_location, _destination_point, out _noPath);
                    return true;
                case robot_state.returning:
                    if (_path.Count == 0)
                        return false;
                    _path = AStarPathfinding.FindPath(_location, _chargingPoint, out _noPath);
                    return true;
                default:
                    return false;
            }
        }
    }
}
