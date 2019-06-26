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

            if (_state == robot_state.free)
            {
                _actionString += " n";
            }
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
