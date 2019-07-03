using System;
using System.Collections.Generic;
using System.Text;

namespace Daiwa
{
    public class Product
    {
        public int _shipperID;
        public string _productID;
        public string _productType;
        public string _storageType;

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
                _productType = data[4];
                _storageType = data[5];
            }
            catch (Exception e)
            {
                //Program.Print(e.Message);
            }
        }
    }
}
