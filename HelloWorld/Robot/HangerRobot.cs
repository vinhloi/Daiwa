using System;
using System.Collections.Generic;
using System.Text;

namespace Daiwa
{
    public class HangerRobot : Robot
    {
        public HangerRobot(int x, int y, Byte id) : base(x, y, id)
        {
        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = _id.ToString();
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
    }
}
