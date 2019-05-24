using System;
using System.Collections.Generic;
using System.Text;

namespace Daiwa
{
    class MaxItemStorage
    {
        string _productType;
        int _maxStorageGeneralPurposeRack;
        int _maxStorageHangerRack;

        public string ProductType { get => _productType; set => _productType = value; }
        public int MaxStorageGeneralPurposeRack { get => _maxStorageGeneralPurposeRack; set => _maxStorageGeneralPurposeRack = value; }
        public int MaxStorageHangerRack { get => _maxStorageHangerRack; set => _maxStorageHangerRack = value; }
    }
}
