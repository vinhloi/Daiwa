using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public class ShippingRobot : Robot
    {
        public ShippingRobot(int x, int y, Byte id) : base(x, y, id)
        {
        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = (_id - 5).ToString(); // Trick: Because we set id = {6 7 8 9} to avoid 0 and 1 
            }

            if (_state == robot_state.free)
            {
                _actionString += " n";
            }

            if (sec == 59)
            {
                _actionString += "\n";
            }
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
