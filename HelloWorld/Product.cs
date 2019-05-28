using System;
using System.Collections.Generic;
using System.Text;

namespace Daiwa
{
    public class Product
    {
        public int _shipperID;
        public string _productID;
        public string _brandID;
        public string _productName;
        public string _productType;
        public string _storageType;
        public int _earlySpring;
        public int _lateSpring;
        public int _summer;
        public int _earlyFall;
        public int _lateFall;
        public int _winter;
        public string _color;
        public string _size;

        public Product(string csvRow)
        {
            if (string.IsNullOrEmpty(csvRow))
            {
                return;
            }

            string[] data = csvRow.Split(',');

            try
            {
                _shipperID = int.Parse(data[0]);
                _productID = data[1];
                _brandID = data[2];
                _productName = data[3];
                _productType = data[4];
                _storageType = data[5];
                _earlySpring = int.Parse(data[6]);
                _lateSpring = int.Parse(data[7]);
                _summer = int.Parse(data[8]);
                _earlyFall = int.Parse(data[9]);
                _lateFall = int.Parse(data[10]);
                _winter = int.Parse(data[11]);
                _color = data[12];
                _size = data[13];
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception when initialize product " + _productID);
            }
        }
    }
}
