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
        protected string type;

        public PickingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _pickingTime = 0;
            transporter = null;
            type = "picking";
        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = _id.ToString(); // add id at sec 0
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
                        if (Warehouse._Transporters.ContainsKey(another_robot._id))
                            another_robot.Reroute();
                        else
                            this.Reroute();
                    }
                    else // anther robot is moving
                    {
                        if (IsFacing(another_robot))
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
            }
        }

        public override void PrepareToReturn()
        {
            if (_state != robot_state.returning && _state != robot_state.free && _pickingTime == 0)
            {
                _path = AStarPathfinding.FindPath(_location, _chargingPoint);
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
                }
                _pickingTime++;
            }
            else // finish picking
            {
                _pickingTime = 0;
                transporter._loadedItems.Enqueue(_order._productID);
                _order._quantity--;
                _order._rack.RemoveItem(_order._productID);
                if (transporter.IsFull())
                {
                    transporter.PrepareToShip();
                }

                if (_order._quantity == 0)
                {
                    transporter.PrepareToShip();
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
                    if (robot._order._productID.Equals(this._order._productID)
                        && robot._state == robot_state.pick
                        && robot.IsFull() == false)
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
                    if (_path.Count == 0)
                        return false;
                    _path = AStarPathfinding.FindPath(_location, _pickup_point);
                    return true;
                case robot_state.returning:
                    if (_path.Count == 0)
                        return false;
                    _path = AStarPathfinding.FindPath(_location, _chargingPoint);
                    return true;
                default:
                    return false;
            }
        }
    }
}
