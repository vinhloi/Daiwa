using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Daiwa
{
    public class PickingRobot : Robot
    {
        private int _count;

        public PickingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _count = 10;
        }

        public void PrepareToPick(Point pickup_point, string product_id, int quantity)
        {
            _path = AStarPathfinding.FindPath(_location, pickup_point);
            _state = robot_state.pick;
            _orderedProduct = product_id;
            _orderedQuantity = quantity;
        }

        public override void GenerateAction(int sec)
        {
            if (sec == 0)
            {
                _actionString = _id.ToString(); // add id at sec 0
            }

            if (_state == robot_state.free || _state == robot_state.waiting) // no action
            {
                _actionString += " n";
                return;
            }

            if (_path.Count > 0) // moving
            {
                Byte robot_id = Warehouse.ValueAt(_path.Peek());
                if (robot_id == 0) // No robot standing at this tile, road is clear
                {
                    MoveToNextTile();
                }
                else // new location is obstructed
                {
                    _actionString += " n"; //vinh: should check if 2 robot facing each other
                }
            }
            else // we arrive at the destination
            {
                if (_state == robot_state.returning)
                    _state = robot_state.free;
                else if (_state == robot_state.pick)
                {
                    Pick();
                }
            }

            if (sec == 59) // Last seconds, add "\n" to end a line
            {
                _actionString += "\n";
            }
        }

        
        private void Pick()
        {
            if(_count == 10) // start picking
            {
                
            }
            else if (_count == 0) // finish picking
            {
                _count = 10;
            }

            _count--;
        }

        private Byte GetAdjacentTransporter()
        {
            int x = _location.X;
            int y = _location.Y;

            if(Warehouse.Map[x - 1, y] >= 10)
            {
            }
            else if (Warehouse.Map[x + 1, y] >= 10)
            {

            }
            else if (Warehouse.Map[x, y - 1] >= 10)
            {

            }
            else if (Warehouse.Map[x, y + 1] >= 10)
            {

            }

        }
    }
}
