using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public enum robot_state
    {
        free = 0,
        store = 1,
        pick = 2,
        ship = 3,
        receive = 4,
        waiting = 5,
        returning = 6
    }

    public class Robot
    {
        public Byte _id;
        public Point _location;
        public Point _chargingPoint;
        public Direction _direction;
        public string _actionString;  // List of action in 60 seconds
        public robot_state _state;
        public Stack<Point> _path;

        public Order _order;

        private bool _avoid;

        public Robot(int x, int y, Byte id)
        {
            _id = id;
            _location.X = x;
            _location.Y = y;
            _chargingPoint.X = x;
            _chargingPoint.Y = y;
            _direction = Direction.Up;
            _actionString = "";
            _state = robot_state.free;
            _path = new Stack<Point>();
            _avoid = false;
            Warehouse.Map[y, x] = id;

            _order._productID = "";
            _order._quantity = 0;
            _order._rackID = "";
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

            int rotate = movingDirection - _direction;
            switch (rotate)
            {
                case 0: //Robot has the correct direction, move forward
                    SetLocation(new_location);
                    _actionString += " f";
                    _path.Pop();
                    break;

                case -3://rotate clock wise
                case 1:
                    _actionString += " r";
                    _direction = movingDirection;
                    break;

                case 3: //rotate counterclockwise
                case -1:
                    _actionString += " l";
                    _direction = movingDirection;
                    break;

                case 2: //opposite direction, rotate clock wise
                case -2:
                    _actionString += " r";
                    _direction = (Direction)(((int)_direction + 1) % 4);
                    break;
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

        public virtual void PrepareToPick(Point pickup_point, string rack_id, string product_id, int quantity)
        {
            Program.Print("Virtual method");
        }
    }
}
