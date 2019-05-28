using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public class Robot
    {
        public int _id;
        public Point _location;
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
        public Point _chargingPoint;
        public Direction _direction;
        public const int _maxItem = 5;
        public int _loadedItem;
    }

    public class PickingRobot : Robot
    {
        public Point _chargingPoint;
        public Direction _direction;

    }

    public class HangerRobot : Robot
    {
        public Point _chargingPoint;
        public Direction _direction;
    }

    public class ReceivingRobot : Robot
    {
        public ReceivingRobot(int row, int column)
        {
            _id = 0;
            _location = new Point(column, row);
            _actionList = "";
        }
    }

    public class ShippingRobot : Robot
    {
        public ShippingRobot(int row, int column, int celldata)
        {
            _id = celldata - 20; // celldata 21 to 24 = Shipping robot ID 1 to 4
            _location = new Point(column, row);
            _actionList = "";
        }
    }
}
