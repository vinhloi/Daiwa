using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Daiwa
{
    public class TransportRobot : Robot
    {
        public const int _maxItem = 5;
        public int _loadedItem;

        public TransportRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _loadedItem = 0;
        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = _id.ToString(); // add id at sec 0
            }

            // The last point is the pickup point, we need to stop adjacent to it
            if (_path.Count == 1 && _state != robot_state.returning)
            {
                _state = robot_state.waiting;
            }

            if (_state == robot_state.free || _state == robot_state.waiting) // no action
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
                    _state = robot_state.free;
            }

            if (sec == 59) // Last seconds, add "\n" to end a line
            {
                _actionString += "\n";
            }
        }

        public void PrepareToPick(Point pickup_point, string product_id)
        {
            _path = AStarPathfinding.FindPath(_location, pickup_point);
            _state = robot_state.pick;
            _order._productID = product_id;
        }

        public void PrepareToShip()
        {
            Product product = new Product(Warehouse._DicItems[_order._productID]);
            ShippingRobot shipper = (ShippingRobot)Warehouse._Shippers[product._shipperID];
            Point ship_point = shipper.GetShipPoint();
            _path = AStarPathfinding.FindPath(_location, ship_point);
            _state = robot_state.ship;
        }

        public void PrepareToReturn()
        {
            _path = AStarPathfinding.FindPath(_location, _chargingPoint);
            _state = robot_state.returning;
        }

        public bool IsFull()
        {
            return (_loadedItem >= _maxItem) ? true : false;
        }
    }
}
