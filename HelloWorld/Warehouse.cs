using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.IO;
using System.Collections;
using System.Diagnostics;

namespace Daiwa
{
    public class Warehouse
    {
        public int _numRows;
        public int _numCols;
        public int[,] _data;
        public List<Rack> _generalRackList;
        public List<Rack> _hangerRackList;
        public List<Robot> _robotList;
        Hashtable _htItems;
        Hashtable _htMaxStorage;

        public Warehouse()
        {
            _numRows = 0;
            _numCols = 0;
            _data = null;
            _htItems = new Hashtable();
            _htMaxStorage = new Hashtable();
            _generalRackList = new List<Rack>();
            _hangerRackList = new List<Rack>();
            _robotList = new List<Robot>();
        }

        public void LoadMap(string map_file)
        {
            Console.WriteLine("LoadMap");

            // Get the file's text.
            string whole_file = File.ReadAllText(map_file);

            // Split into lines.
            whole_file = whole_file.Replace('\n', '\r');
            string[] lines = whole_file.Split(new char[] { '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            // See how many rows and columns there are.
            int _num_rows = lines.Length;
            int _num_cols = lines[0].Split(',').Length;

            // Allocate the data array.
            _data = new int[_num_rows, _num_cols];

            // Load the array.
            for (int r = 0; r < _num_rows; r++)
            {
                string[] line_r = lines[r].Split(',');
                for (int c = 0; c < _num_cols; c++)
                {
                    int i = 0;
                    if (!Int32.TryParse(line_r[c], out i))
                    {
                        i = -1;
                    }
                    _data[r, c] = i;
                    CreateObject(r, c, i);
                }
            }

        }

        public void CreateObject(int row, int column, int celldata)
        {
            switch (celldata)
            {
                case 10: //General-purpose rack (upward/downward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalRackList.Add(new GeneralPurposeRack(row, column, height, Direction.Up));
                        _generalRackList.Add(new GeneralPurposeRack(row, column, height, Direction.Down));
                    }
                    break;

                case 11: //General-purpose rack (leftward/rightward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalRackList.Add(new GeneralPurposeRack(row, column, height, Direction.Left));
                        _generalRackList.Add(new GeneralPurposeRack(row, column, height, Direction.Right));
                    }
                    break;

                case 12: //Hanger rack (leftward/rightward directions)
                    _hangerRackList.Add(new HangerRack(row, column));
                    break;

                case 20: //Receiving point
                    _robotList.Add(new ReceivingRobot(row, column));
                    break;

                case 21: // Shipping point (corresponding to Shipper ID 1 to 4)
                case 22:
                case 23:
                case 24:
                    _robotList.Add(new ShippingRobot(row, column, celldata));
                    break;
                default:
                    break;
            }
        }

        public void LoadItemsFile(string items_file)
        {
            Console.WriteLine("LoadItemsFile");

            using (var reader = new StreamReader(items_file))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    _htItems.Add(values[1], line);
                }
            }
        }

        public void LoadItemCategoriesFile(string item_categories_file)
        {
            Console.WriteLine("LoadItemCategoriesFile");

            using (var reader = new StreamReader(item_categories_file))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    _htMaxStorage.Add(values[0], line);
                }
            }
        }


        public string SpecifyProductInitialPosition(List<string> input)
        {
            Console.WriteLine("SpecifyProductInitialPosition");

            string output = "store";
            for (int i = 1; i < input.Count; i += 2)
            {
                //Debug.WriteLine(i / 2);

                string product_id = input[i];
                int quantity = int.Parse(input[i + 1]);

                Product product_info = new Product((string)_htItems[product_id]);
                if (product_info == null)
                {
                    Console.WriteLine("Can not find product info");
                    continue;
                }

                MaxStorage max_storage = new MaxStorage((string)_htMaxStorage[product_info._productType]);
                if (max_storage == null)
                {
                    Console.WriteLine("Can not find max storage");
                    continue;
                }

                Rack empty_rack = FindEmptyRack(product_info, quantity);

                if (empty_rack != null)
                {
                    empty_rack._productType = product_info._productType;
                    empty_rack._shipperID = product_info._shipperID;
                    empty_rack._num_items = quantity;
                    empty_rack.SetMaxStorage(max_storage);
                    empty_rack._itemList.Add(new RackItem(product_id, quantity));

                    string store_product_command = " " + product_id + " " + empty_rack.GetRackPosition();
                    for (int j = 0; j < quantity; j++)
                        output = output + store_product_command;

                }
                else
                {
                    Console.WriteLine("Can not find empty rack");
                }
            }

            return output;
        }

        private Rack FindEmptyRack(Product product_info, int quantity)
        {
            if (product_info._storageType.Equals("fold"))
            {
                foreach (GeneralPurposeRack rack in _generalRackList)
                {
                    if (rack._num_items == 0)
                        return rack;
                    if (rack.IsFull() == false &&
                        rack._productType.Equals(product_info._productType) &&
                        (rack._shipperID == product_info._shipperID))
                    {
                        empty_rack = _generalRackList.FirstOrDefault(rack => rack._num_items == 0);
                    }
                        

                }
            }
            else
            {
                empty_rack = _hangerRackList.FirstOrDefault(rack => rack._num_items == 0);
            }

            return empty_rack;
        }

        public List<string> SpecifyRobotInitialPosition()
        {
            Console.WriteLine("SpecifyRobotInitialPosition");

            List<string> robots = new List<string>();
            robots.Add("conveyor 0202 0203");
            robots.Add("picker 0302 0303");
            robots.Add("hanger 0402 0403");
            return robots;
        }
    }
}
