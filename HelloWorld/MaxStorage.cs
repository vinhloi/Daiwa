using System;
using System.Collections.Generic;
using System.Text;

namespace Daiwa
{
    public class MaxStorage
    {
        public string _productType;
        public int _maxFoldStorage;
        public int _maxHangerStorage;

        public MaxStorage(string csvRow)
        {
            if (string.IsNullOrEmpty(csvRow))
            {
                return;
            }

            string[] data = csvRow.Split(',');

            try
            {
                _productType = data[0];
                _maxFoldStorage = int.Parse(data[1]);
                _maxHangerStorage = int.Parse(data[2]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
