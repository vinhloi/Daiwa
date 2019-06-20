using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Daiwa
{
    public class HangingRobot : PickingRobot
    {
        public HangingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            type = "hanging";
        }
    }
}
