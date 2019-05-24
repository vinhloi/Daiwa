using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public enum Direction
    {
        North = 0,
        East = 1,
        West = 2,
        South = 3,
    }

    public class Robot
    {
        public int _id;

        public Point _location;
        public Point _chargingPoint;

        public Direction _direction;
       
        public string _actionList;  // List of action in 60 seconds

        public Robot()
        {

        }

        public virtual void DoAction()
        {

        }

    }

    public class TransportRobot : Robot
    {
        public const int _maxItem = 5;
        public int _loadedItem;
    }

    public class PickingRobot : Robot
    {
    }

    public class HangerRobot : Robot
    {
    }

    public class ReceivingRobot : Robot
    {
    }

    public class ShippingRobot : Robot
    {
    }
}
