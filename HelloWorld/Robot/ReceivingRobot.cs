using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Daiwa
{
    public class ReceivingRobot : Robot
    {
        public int _shipperID;
        public ReceivingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _shipperID = id;
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
