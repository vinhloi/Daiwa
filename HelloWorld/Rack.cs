﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;

namespace Daiwa
{
    public class RackItem
    {
        public string _productID;
        public int _quantity;

        public RackItem(string product_id, int quantity)
        {
            _productID = product_id;
            _quantity = quantity;
        }
    }

    public class Rack
    {
        public Point _location;
        public int _height;  //Shelf number
        public Direction _direction;
        public string _storageType;
        public string _productType;
        public List<RackItem> _itemList;
        public int _shipperID;
        public int _num_items;
        public int _max_item;

        public int _expectedPickQuantity;
        public int _expectedSlotQuantity;

        public Rack(int x, int y)
        {
            _location = new Point(x, y);
            _itemList = new List<RackItem>();
            _shipperID = -1;
            _num_items = 0;
            _max_item = int.MaxValue;
            _expectedPickQuantity = 0;
            _expectedSlotQuantity = 0;
        }

        public virtual void SetMaxStorage(MaxStorage max_storage)
        {
        }

        public Point GetPickUpPoint()
        {
            Point pickup_point;
            switch(_direction)
            {
                case Direction.Up:
                    pickup_point = new Point(_location.X, _location.Y - 1) ;
                    break;
                case Direction.Right:
                    pickup_point = new Point(_location.X + 1, _location.Y);
                    break;
                case Direction.Down:
                    pickup_point = new Point(_location.X, _location.Y + 1);
                    break;
                case Direction.Left:
                    pickup_point = new Point(_location.X - 1, _location.Y);
                    break;
                case Direction.Fix:
                default:
                    pickup_point = new Point(_location.X - 1, _location.Y);
                    break;
            }
            return pickup_point;
        }

        public string GetXXYYDH()
        {
            string XX = _location.X.ToString("X2");
            string YY = _location.Y.ToString("X2");
            string D = "";
            switch(_direction)
            {
                case Direction.Up:
                    D = "1";
                break;
                case Direction.Down:
                    D = "2";
                    break;
                case Direction.Left:
                    D = "3";
                    break;
                case Direction.Right:
                    D = "4";
                    break;
                case Direction.Fix:
                    D = "0";
                    break;
                default:
                    D = "0";
                    break;
            }
            return XX + YY + D + _height;
        }

        public bool IsFull()
        {
            return (_num_items + _expectedSlotQuantity >= _max_item) ? true : false;
        }

        public bool IsEnoughSpaceToSlot(int quantity)
        {
            if(_num_items + _expectedSlotQuantity + quantity <= _max_item)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool isEmpty()
        {
            return (_num_items == 0) ? true : false;
        }

        public void RemoveItem(string product_id)
        {
            RackItem obj = _itemList.FirstOrDefault(x => x._productID.Equals(product_id));
            if (obj != null)
            {
                obj._quantity--;
                _num_items--;
                _expectedPickQuantity--;
                if (obj._quantity == 0)
                    _itemList.Remove(obj);
            }
        }

        public int GetNumberOfItem(string product_id)
        {
            if (_expectedPickQuantity > 0) // If rack is on picking order, avoid it to not cause traffic jam
                return 0;

            foreach (RackItem item in _itemList)
            {
                // Find the correct product 
                if (item._productID.Equals(product_id))
                {
                    return item._quantity;
                }
            }

            return 0;
        }

        public void AddItem(string product_id)
        {
            _num_items++;
            _expectedSlotQuantity--;

            RackItem obj = _itemList.FirstOrDefault(x => x._productID.Equals(product_id));
            if (obj != null)
            {
                obj._quantity++;
            }
            else
            {
                _itemList.Add(new RackItem(product_id, 1));
            }
        }
    }

    public class GeneralPurposeRack : Rack
    {
        public GeneralPurposeRack(int x, int y, int height, Direction direction) : base(x, y)
        {
            _height = height;
            _direction = direction;
            _storageType = "fold";
            _productType = null;
        }

        public override void SetMaxStorage(MaxStorage max_storage)
        {
            _max_item = max_storage._maxFoldStorage;
        }
    }

    public class HangerRack : Rack
    {
        public HangerRack(int x, int y) : base(x, y)
        {
            _height = 0;
            _direction = Direction.Fix;
            _storageType = "hanger";
            _productType = "mixed";
        }

        public override void SetMaxStorage(MaxStorage max_storage)
        {
            _max_item = max_storage._maxHangerStorage;
        }
    }
}
