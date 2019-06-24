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
        public Point _ship_point;

        public TransportRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _loadedItems = new Queue<string>();
        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = _id.ToString(); // add id at sec 0
            }

            // The last point is the pickup point, we need to stop adjacent to it
            if (_path.Count == 1 && _state == robot_state.pick && _path.Peek().Equals(_pickup_point))
            {
                _path.Pop();
            }

            if (_state == robot_state.free) // no action
            {
                _actionString += " n";
            }          
            else if (_path.Count > 0) // moving
            {
                Point new_location = _path.Peek();
                if (Warehouse.ValueAt(new_location) == 0) // new location is clear
                {
                    MoveToNextTile();
                }
                else // new location is obstructed
                {
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
                    return FindNewRouteToPick();
                case robot_state.ship:
                    if (_path.Count == 0)
                    {
                        return false;
                    }
                    else
                    {
                        _path = AStarPathfinding.FindPath(_location, _ship_point);
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
            int x = _pickup_point.X;
            int y = _pickup_point.Y;

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
            _ship_point = shipper.GetShipPoint();

            _path = AStarPathfinding.FindPath(_location, _ship_point);
            _state = robot_state.ship;
        }

        public override void PrepareToReturn()
        {
            if (_state != robot_state.returning && _state != robot_state.free && _path.Count != 0)
            {
                _path = AStarPathfinding.FindPath(_location, _chargingPoint);
                _state = robot_state.returning;
            }
        }

        public bool IsFull()
        {
            return (_loadedItems.Count >= _maxItem) ? true : false;
        }
    }
}
