using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Daiwa
{
    public class StorageArea
    {
        public Point _location;
        public string _storageType;
        public int _shipperID;
        public int _num_items;
        public int _max_items;
    }

    public class GeneralPurposeArea : StorageArea
    {
        public int _height;  //Shelf number (1 to 5) 
        public int _direction;  //Directions (1: Upward, 2: Downward, 3: Leftward, 4: Rightward).
        public string _productType;
    }

    public class HangerArea : StorageArea
    {

    }
}
