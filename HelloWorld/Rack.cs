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
        Down = 2,
        Left = 3,
        Right = 4
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

        public void SetMaxStorage(MaxStorage max_storage)
        {
            if(_storageType.Equals("fold"))
                _max_storage = max_storage._maxFoldStorage;
            else
                _max_storage = max_storage._maxHangerStorage;
        }

        public string GetRackPosition()
        {
            string XX = _location.X.ToString("X2"); 
            string YY = _location.Y.ToString("X2");
            return XX + YY + _direction.ToString("D") + _height;
        }
    }

    public class GeneralPurposeRack : Rack
    {
        public GeneralPurposeRack(int row, int column, int height, Direction direction)
        {
            _location = new Point(column, row);
            _height = height;
            _direction = direction;
            _storageType = "fold";

            _productType = null;
            _itemList = new List<RackItem>();
            _shipperID = -1;
            _num_items = 0;
            _max_storage = 0;
        }
    }

    public class HangerRack : Rack
    {
        public HangerRack(int row, int column)
        {
            _location = new Point(column, row);
            _height = 0;
            _direction = 0;
            _storageType = "hanger";

            _productType = "mixed";
            _itemList = new List<RackItem>();
            _shipperID = -1;
            _num_items = 0;
            _max_storage = 0;
        }
    }
}
