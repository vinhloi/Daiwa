using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public enum robot_state
    {
        sleeping = 0,
        running = 1
    }

    public class Robot
    {
        public Byte _id;
        public Point _location;
        public Point _chargingPoint;
        public Direction _direction;
        public string _actionList;  // List of action in 60 seconds
        public robot_state state;
        public Stack<Point> path;

        public Robot(int x, int y, Byte id)
        {
            _id = id;
            _location.X = x;
            _location.Y = y;
            _chargingPoint.X = x;
            _chargingPoint.Y = y;
            _direction = Direction.Up;
            _actionList = _id.ToString();
            state = robot_state.sleeping;
            path = null;
            Warehouse.Map[y, x] = id;
        }

        public string GetHexaPosition()
        {
            string XX = _location.X.ToString("X2");
            string YY = _location.Y.ToString("X2");
            return XX + YY;
        }

        public int MoveTo(Point destination)
        {
            Direction movingDirection = GetDirection(destination);
            if (movingDirection == Direction.Fix)
                return 0;

            int rotate = movingDirection - _direction;
            switch(rotate)
            {
                case 0: //Robot has the correct direction, move forward
                    SetNewLocation(destination);
                    break;

                case -3://rotate clock wise
                case 1: 
                    _actionList += " r";
                    _direction = movingDirection;
                    break;

                case 3: //rotate counterclockwise
                case -1:
                    _actionList += " l";
                    _direction = movingDirection;
                    break;

                case 2: //opposite direction, rotate clock wise
                case -2:
                    _actionList += " r";
                    _direction = (Direction)(((int)_direction + 1) % 4);
                    break;
            }
            return rotate;
        }

        protected Direction GetDirection(Point destination)
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
                return Direction.Fix;
            }
        }

        protected void SetNewLocation(Point new_location)
        {
            if (Warehouse.Map[new_location.Y, new_location.X] == 0) // new location is clear
            {
                // Clear the old tile
                Warehouse.Map[_location.Y, _location.X] = 0;

                _location.X = new_location.X;
                _location.Y = new_location.Y;
                Warehouse.Map[new_location.Y, new_location.X] = _id;

                _actionList += " f";
            }
            else // new location is obstructed
            {
                _actionList += " n";//vinh: should check if 2 robot facing each other
            }
        }

        public virtual void DoAction()
        {

        }

    }

    public class TransportRobot : Robot
    {
        public const int _maxItem = 5;
        public int _loadedItem;

        public TransportRobot(int x, int y, Byte id) : base(x ,y, id)
        {
            _loadedItem = 0;
        }

        public override void DoAction()
        {
            if(path.Count > 0)
            {
                int result = MoveTo(path.Peek());
                if (result == 0)
                    path.Pop();
            }
            else
            {
                //vinh: do something

            }
        }
    }

    public class PickingRobot : Robot
    {
        public PickingRobot(int x, int y, Byte id) : base(x, y, id)
        {
        }
    }

    public class HangerRobot : Robot
    {
        public HangerRobot(int x, int y, Byte id) : base(x, y, id)
        {
        }
    }

    public class ReceivingRobot : Robot
    {
        public int _shipperID;
        public ReceivingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _shipperID = id;
        }
    }

    public class ShippingRobot : Robot
    {
        public ShippingRobot(int x, int y, Byte id) : base(x, y, id)
        {
        }
    }
}
