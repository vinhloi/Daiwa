using System;
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

        public Robot(int x, int y, Byte id)
        {
            _id = id;
            _location.X = x;
            _location.Y = y;
            _chargingPoint.X = x;
            _chargingPoint.Y = y;
            _direction = Direction.Up;
            _actionString = "";
            _backUpState = robot_state.free;
            _state = robot_state.free;
            _path = new Stack<Point>();
            Warehouse.Map[y, x] = id;

            _order = new Order();
        }

        public string GetHexaPosition()
        {
            string XX = _location.X.ToString("X2");
            string YY = _location.Y.ToString("X2");
            return XX + YY;
        }

        protected virtual void MoveToNextTile()
        {
            Point new_location = _path.Peek();

            Direction movingDirection = GetMovingDirection(new_location);
            if (movingDirection == Direction.Error)
            {
                return; // The new tile is not adjecent to current tile, return
            }

            if (Rotate(movingDirection) == false)
            {
                SetLocation(new_location);
                _actionString += " f";
                _path.Pop();
            }
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
                    _direction = (Direction)(((int)_direction + 1) % 4);
                    return true;
                default:
                    return true;
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
            _path = AStarPathfinding.FindPath(_location, pickup_point);
            _state = robot_state.pick;

            _order._rack = rack;
            _order._productID = product_id;
            _order._quantity = quantity;
            _destination_point = pickup_point;
        }

        public virtual void PrepareToSlot(Point pickup_point, Rack rack, int quantity)
        {
            _path = AStarPathfinding.FindPath(_location, pickup_point);
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
            _state = _backUpState;
            _backUpState = robot_state.free;
            _path = AStarPathfinding.FindPath(_location, _destination_point);
            Program.Print("Resume: " + _id + " " + _backUpState + " " + _destination_point + "\n");
        }

        public virtual bool Reroute()
        {
            Program.Print("Virtual method");
            return false;
        }

        public bool IsFacing(Robot another_robot)
        {
            if((_direction == Direction.Up && another_robot._direction== Direction.Down) ||
                (_direction == Direction.Down && another_robot._direction == Direction.Up) ||
                (_direction == Direction.Left && another_robot._direction == Direction.Left) ||
                (_direction == Direction.Right && another_robot._direction == Direction.Right))
            {
                return true;
            }
            return false;
        }
    }
}
