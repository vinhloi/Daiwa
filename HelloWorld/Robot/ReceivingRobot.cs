using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Daiwa
{
    public class ReceivingRobot : Robot
    {
        private int _random;
        private List<Point> _receivePoints;
        private int _receivingTime;
        private TransportRobot transporter;

        public ReceivingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _random = 0;
            _receivingTime = 0;
            type = "receiver";

            _receivePoints = new List<Point>()
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
                _actionString = "0";
            }

            if (_receivingTime < 4)
            {
                if (_receivingTime == 0)
                {
                    if (sec + 5 > 59)
                    {
                        _actionString += " n";
                        return;
                    }

                    transporter = GetAdjacentTransporter(); // Find a transporter
                    if (transporter == null)
                    {
                        _actionString += " n"; // Can't find transporter, return
                        return;
                    }

                    _actionString = _actionString + " p " + transporter._id + " " + transporter._expectedReceiveItems.Peek();
                    transporter._isLoading = true;
                }
                _receivingTime++;
            }
            else
            {
                transporter.FinishReceiving();
                _receivingTime = 0;
            }
        }

        private TransportRobot GetAdjacentTransporter()
        {
            foreach (Point location in _receivePoints)
            {
                Byte id = Warehouse.ValueAt(location);
                if (id > 9 && Warehouse._Transporters.ContainsKey(id))
                {
                    TransportRobot robot = (TransportRobot)Warehouse._Transporters[id];
                    if (robot._state == robot_state.receive
                        && robot._expectedReceiveItems.Count > 0
                        && robot._path.Count == 0)
                        return robot;
                }
            }

            return null;
        }

        public Point GetReceivePoint()
        {
            _random = (_random + 1) % 4;
            return _receivePoints[_random];
        }

        public override void PrepareToReturn()
        {
            Program.Print("This robot can't move");
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
