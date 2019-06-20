using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Daiwa
{
    public class PickingRobot : Robot
    {
        protected int _pickingTime;
        protected TransportRobot transporter;
        protected string type;

        public PickingRobot(int x, int y, Byte id) : base(x, y, id)
        {
            _pickingTime = 0;
            transporter = null;
            type = "picking";
        }

        public override void PrepareToPick(Point pickup_point, string rack_id, string product_id, int quantity)
        {
            _path = AStarPathfinding.FindPath(_location, pickup_point);
            _state = robot_state.pick;
            _order._rackID = rack_id;
            _order._productID = product_id;
            _order._quantity = quantity;
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
            }
            else if (_path.Count > 0) // moving
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
                else if (_state == robot_state.pick && _order._quantity > 0)
                {
                    Pick();
                }
            }

            if (sec == 59) // Last seconds, add "\n" to end a line
            {
                _actionString += "\n";
            }
        }

        
        protected void Pick()
        {
            if(_pickingTime <  9)
            {
                if (_pickingTime == 0) // start picking
                {
                    transporter = GetAdjacentTransporter();
                    if (transporter == null)
                    {
                        _actionString += " n";
                        return;
                    }
                    else
                    {
                        _actionString = _actionString + " p " + transporter._id + " " + _order._rackID + " " + _order._productID;
                    }
                }
                _pickingTime++;
            }
            else // finish picking
            {
                transporter._loadedItem++;
                _order._quantity--;
                if(transporter.IsFull())
                {
                    transporter.PrepareToShip();
                }

                if(_order._quantity == 0)
                {
                    transporter.PrepareToShip();
                    // need to revise this
                    _state = robot_state.free;
                }
                _pickingTime = 0;
            }
        }

        protected TransportRobot GetAdjacentTransporter()
        {
            int x = _location.X;
            int y = _location.Y;

            var proposedLocations = new List<Point>()
            {
                new Point ( x - 1, y ),
                new Point ( x + 1, y ),
                new Point ( x, y - 1 ),
                new Point ( x, y + 1 ),
            };

            foreach(Point location in proposedLocations)
            {
                Byte id = Warehouse.ValueAt(location);
                if(id> 9 && Warehouse._Transporters.ContainsKey(id))
                {
                    TransportRobot robot = (TransportRobot)Warehouse._Transporters[id];
                    if(robot._order._productID.Equals(this._order._productID) 
                        && robot._state == robot_state.waiting
                        && robot.IsFull() == false)
                        return robot;
                }
            }

            return null;
        }
    }
}
