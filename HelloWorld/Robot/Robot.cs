﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public enum robot_state
    {
        free = 0,
        slot = 1,
        pick = 2,
        ship = 3,
        receive = 4,
        returning = 5
    }

    public enum Direction
    {
        Error = -1,
        Fix = 0,
        Up = 1,
        Right = 2,
        Down = 3,
        Left = 4
    }

    public class Robot
    {
        public Byte _id;
        public Point _location;
        public Point _chargingPoint;
        public Point _destination_point;
        public Direction _direction;
        public string _actionString;  // List of action in 60 seconds
        public robot_state _backUpState;
        public robot_state _state;
        public Stack<Point> _path;
        public Order _order;
        public string type;
        public bool _noPath; // Indicate no path to go from _location to _destination_point
        public bool _isLoading = false;
        public bool _isUnloading = false;

        public Robot(int x, int y, Byte id)
        {
            _id = id;
            _chargingPoint.X = x;
            _chargingPoint.Y = y;
            _location = _chargingPoint;
            _destination_point = _chargingPoint;
            _direction = Direction.Up;
            _actionString = "";
            _backUpState = robot_state.free;
            _state = robot_state.free;
            _path = new Stack<Point>();
            Warehouse.Map[y, x] = 1;

            _order = new Order();
            _noPath = false;
        }

        public string GetHexaPosition()
        {
            string XX = _location.X.ToString("X2");
            string YY = _location.Y.ToString("X2");
            return XX + YY;
        }

        protected virtual void MoveToNextTile()
        {
            SetLocation(_path.Pop());
            _actionString += " f";

            if (_path.Count > 0)
            {
                Byte robot_id = Warehouse.ValueAt(_path.Peek());
                if (robot_id == 0 || robot_id == 1) // No robot standing at this tile, road is clear
                {
                    return;
                }

                Robot another_robot = Warehouse._AllMovingRobots[robot_id];
                if (IsCollideWith(another_robot))
                {
                    Program.Print(_id + " is collide with " + robot_id + " at " + _path.Peek() + "\n");
                    if (another_robot._state == robot_state.slot || another_robot._state == robot_state.pick)
                        another_robot.AvoidToLeavePath();
                }
            }
        }

        public bool AvoidToLeavePath()
        {
            if (_isLoading == true || _isUnloading == true)
                return false;

            Point LeftTile;
            Point RightTile;
            Point BackTile;
            Point FrontTile;
            switch (_direction)
            {
                case Direction.Up:
                    LeftTile = new Point(_location.X - 1, _location.Y);
                    RightTile = new Point(_location.X + 1, _location.Y);
                    BackTile = new Point(_location.X, _location.Y + 1);
                    FrontTile = new Point(_location.X, _location.Y - 1);
                    break;
                case Direction.Down:
                    LeftTile = new Point(_location.X + 1, _location.Y);
                    RightTile = new Point(_location.X - 1, _location.Y);
                    BackTile = new Point(_location.X, _location.Y - 1);
                    FrontTile = new Point(_location.X, _location.Y + 1);
                    break;
                case Direction.Left:
                    LeftTile = new Point(_location.X, _location.Y + 1);
                    RightTile = new Point(_location.X, _location.Y - 1);
                    BackTile = new Point(_location.X + 1, _location.Y);
                    FrontTile = new Point(_location.X - 1, _location.Y);
                    break;
                case Direction.Right:
                    LeftTile = new Point(_location.X, _location.Y - 1);
                    RightTile = new Point(_location.X, _location.Y + 1);
                    BackTile = new Point(_location.X - 1, _location.Y);
                    FrontTile = new Point(_location.X + 1, _location.Y);
                    break;
                default:
                    return false;
            }
           
            if (Warehouse.ValueAt(LeftTile) == 0)
            {
                _path.Push(_location);
                _path.Push(LeftTile);
                Program.PrintLine( _id + "Move to left side" + _location + LeftTile);
                return true;
            }
            else if (Warehouse.ValueAt(RightTile) == 0)
            {
                _path.Push(_location);
                _path.Push(RightTile);
                Program.PrintLine(_id + "Move to right side" + _location + RightTile);
                return true;
            }
            else if (Warehouse.ValueAt(BackTile) == 0)
            {
                _path.Push(_location);
                _path.Push(BackTile);
                Program.PrintLine(_id + "Move to back side" + _location + BackTile);
                return true;
            }
            else if (Warehouse.ValueAt(FrontTile) == 0)
            {
                _path.Push(_location);
                _path.Push(FrontTile);
                Program.PrintLine(_id + "Move to front side" + _location + FrontTile);
                return true;
            }
            Program.PrintLine(_id + "can not move");
            return false;
        }

        protected bool Rotate()
        {
            Point new_location = _path.Peek();

            Direction new_direction = GetMovingDirection(new_location);
            if (new_direction == Direction.Error)
            {
                Program.Print("Wrong tile\n");
                return false;
            }

            return Rotate(new_direction);
        }

        protected bool Rotate(Direction new_direction)
        {
            int rotate = new_direction - _direction;
            switch (rotate)
            {
                case 0: //new direction equals current direction
                    return false;

                case -3://rotate clock wise
                case 1:
                    _actionString += " r";
                    _direction = new_direction;
                    return true;

                case 3: //rotate counterclockwise
                case -1:
                    _actionString += " l";
                    _direction = new_direction;
                    return true;

                case 2: //opposite direction, rotate clock wise
                case -2:
                    _actionString += " r";
                    _direction++;
                    if ((int)_direction > 4)
                        _direction -= 4;
                    return true;
                default:
                    return false;
            }
        }

        protected virtual Direction GetMovingDirection(Point destination)
        {
            int offset_x = destination.X - _location.X;
            int offset_y = destination.Y - _location.Y;

            if (offset_x == 0 && offset_y == -1) // move up
            {
                return Direction.Up;
            }
            else if (offset_x == -1 && offset_y == 0) // move left
            {
                return Direction.Left;
            }
            else if (offset_x == 0 && offset_y == 1) // move down
            {
                return Direction.Down;
            }
            else if (offset_x == 1 && offset_y == 0) // move right
            {
                return Direction.Right;
            }
            else
            {
                Program.Print("Error: wrong move");
                return Direction.Error;
            }
        }

        protected virtual void SetLocation(Point new_location)
        {
            // Update Map Tile
            Warehouse.Map[_location.Y, _location.X] = 0;
            Warehouse.Map[new_location.Y, new_location.X] = _id;
            Warehouse.Map[_chargingPoint.Y, _chargingPoint.X] = 1;

            // update location
            _location.X = new_location.X;
            _location.Y = new_location.Y;
        }

        public virtual void GenerateAction(int sec)
        {
            Program.Print("Virtual method");
        }

        public virtual void Avoid()
        {
            Program.Print("Virtual method");
        }

        public virtual void PrepareToPick(Point pickup_point, Rack rack, string product_id, int quantity)
        {
            _path = AStarPathfinding.FindPath(_location, pickup_point, out _noPath, true);
            _state = robot_state.pick;

            _order._rack = rack;
            _order._productID = product_id;
            _order._quantity = quantity;
            _destination_point = pickup_point;
        }

        public virtual void PrepareToSlot(Point pickup_point, Rack rack, int quantity)
        {
            _path = AStarPathfinding.FindPath(_location, pickup_point, out _noPath, true);
            _state = robot_state.slot;

            _order._rack = rack;
            _order._quantity = quantity;
            _destination_point = pickup_point;
        }

        public virtual void PrepareToReturn()
        {
            Program.Print("Virtual method");
        }

        public virtual void ForceReturnChargingPoint()
        {
            Program.Print("Virtual method");
        }

        public virtual void ResumeActivityLastDay()
        {
            if (_backUpState != robot_state.free)
            {
                _state = _backUpState;
                _backUpState = robot_state.free;
                _path = AStarPathfinding.FindPath(_location, _destination_point, out _noPath);
                Program.Print("Resume: " + _id + " " + _backUpState + " " + _destination_point + "\n");
            }
        }

        public virtual bool Reroute()
        {
            Program.Print("Virtual method");
            return false;
        }

        public bool IsCollideWith(Robot another_robot)
        {
            if (another_robot._path.Count == 0 || _path.Count == 0)
                return false;

            if (_location.Equals(another_robot._path.Peek()) && _path.Peek().Equals(another_robot._location))
            {
                return true;
            }
            return false;
        }
    }
}
