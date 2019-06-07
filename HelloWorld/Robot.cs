using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public enum robot_state
    {
        waiting = 0,
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
            _actionList = "";
            state = robot_state.waiting;
            path = null;
        }

        public string GetHexaPosition()
        {
            string XX = _location.X.ToString("X2");
            string YY = _location.Y.ToString("X2");
            return XX + YY;
        }

        public string Move(Byte[,] map, Point destination)
        {

            // No obstacle, move to new location
            _location.X = destination.X;
            _location.Y = destination.Y;
            map[_location.Y, _location.X] = _id;
            return "";
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
