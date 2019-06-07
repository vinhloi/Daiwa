using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public enum Direction
    {
        Fix = 0,
        Up = 1,
        Right = 2,
        Down = 3,
        Left = 4
    }

    public struct RackItem
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
        public int _max_storage;

        public Rack(int x, int y)
        {
            _location = new Point(x, y);
            _itemList = new List<RackItem>();
            _shipperID = -1;
            _num_items = 0;
            _max_storage = int.MaxValue;
        }

        public virtual void SetMaxStorage(MaxStorage max_storage)
        {
        }

        public string GetRackPosition()
        {
            string XX = _location.X.ToString("X2");
            string YY = _location.Y.ToString("X2");
            return XX + YY + _direction.ToString("d") + _height;
        }

        public bool IsFull()
        {
            return (_num_items >= _max_storage) ? true : false;
        }

        public bool isEmpty()
        {
            return (_num_items == 0) ? true : false;
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
            _max_storage = max_storage._maxFoldStorage;
        }
    }

    public class HangerRack : Rack
    {
        public HangerRack(int x, int y) : base(x, y)
        {
            _height = 0;
            _direction = 0;
            _storageType = "hanger";
            _productType = "mixed";
        }

        public override void SetMaxStorage(MaxStorage max_storage)
        {
            _max_storage = max_storage._maxHangerStorage;
        }
    }
}
