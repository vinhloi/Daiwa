using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public class ShippingRobot : Robot
    {
        private int _random;
        private List<Point> _shipPoints;

        private int _shippingTime;
        private TransportRobot transporter;

        public ShippingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _random = 0;
            _shippingTime = 0;

            _shipPoints = new List<Point>()
            {
                new Point ( _location.X - 1, _location.Y ),
                new Point ( _location.X + 1, _location.Y ),
                new Point ( _location.X, _location.Y - 1 ),
                new Point ( _location.X, _location.Y + 1 ),
            };

        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = (_id - 5).ToString(); // Trick: Because we set id = {6 7 8 9} to avoid 0 and 1 
            }
            
            Ship();

            if (sec == 59)
            {
                _actionString += "\n";
            }
        }

        private void Ship()
        {
            if (transporter == null) // shipper is free
            {
                transporter = GetAdjacentTransporter(); // Find a transporter
                if (transporter == null)
                {
                    _actionString += " n"; // Can't find transporter, return
                }
                else
                {
                    _state = robot_state.ship;
                    _actionString = _actionString + " s " + transporter._id + " " + transporter._order._productID;
                    _shippingTime++;
                }
            }
            else
            {
                if (_shippingTime < 4) // shipping take 5 seconds
                {
                    _shippingTime++;
                }
                else // Finish shipping
                {
                    transporter._loadedItem--;
                    if (transporter._loadedItem == 0)
                    {
                        transporter.PrepareToReturn();
                        transporter = null;
                    }
                    _shippingTime = 0;
                    _state = robot_state.free;
                }
            }
        }

        private TransportRobot GetAdjacentTransporter()
        {
            foreach (Point location in _shipPoints)
            {
                Byte id = Warehouse.ValueAt(location);
                if (id > 9 && Warehouse._Transporters.ContainsKey(id))
                {
                    TransportRobot robot = (TransportRobot)Warehouse._Transporters[id];
                    if (robot._state == robot_state.waiting
                        && robot._loadedItem > 0)
                        return robot;
                }
            }

            return null;
        }

        public Point GetShipPoint()
        {
            _random = (_random + 1) % 4;
            return _shipPoints[_random];
        }

        protected override void MoveToNextTile()
        {
            Program.Print("This robot can't move");
        }

        protected override Direction GetMovingDirection(Point destination)
        {
            return Direction.Fix;
        }

        protected override void SetLocation(Point new_location)
        {
            Program.Print("This robot can't move");
        }

        public override void Avoid()
        {
            Program.Print("This robot can't move");
        }
    }
}
