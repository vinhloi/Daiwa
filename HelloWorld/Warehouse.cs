using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Odbc;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Drawing;

namespace Daiwa
{
    public class Warehouse
    {
        public int _numRows;
        public int _numCols;
        public Byte[,] _map;
        public List<Rack> _generalRackList; // racks contain product
        public List<Rack> _hangerRackList;  // racks contain product
        public Queue<Rack> _generalEmptyRacksQueue; // empty rack
        public Queue<Rack> _hangerEmptyRackQueue;   // empty rack
        public List<Robot> _PickerList;
        public List<Robot> _HangerList;
        public List<Robot> _TransporterList;
        public List<Robot> _ReceiverList;
        public List<Robot> _ShipperList;
        Hashtable _htItems;
        Hashtable _htMaxStorage;

        public int _day;
        public int _time;

        public AStarPathfinding pathfinder;

        public Warehouse()
        {
            _numRows = 0;
            _numCols = 0;
            _map = null;
            _htItems = new Hashtable();
            _htMaxStorage = new Hashtable();
            _generalRackList = new List<Rack>();
            _hangerRackList = new List<Rack>();
            _generalEmptyRacksQueue = new Queue<Rack>();
            _hangerEmptyRackQueue = new Queue<Rack>();

            _PickerList = new List<Robot>();
            _HangerList = new List<Robot>();
            _TransporterList = new List<Robot>();
            _ReceiverList = new List<Robot>();
            _ShipperList = new List<Robot>();

            LoadItemsFile("data\\items.csv");
            LoadItemCategoriesFile("data\\item_categories.csv");
            LoadMap("data\\map.csv");

        }

        public void LoadMap(string map_file)
        {
            Program.Print("LoadMap\n");

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
            _map = new Byte[_num_rows, _num_cols];

            // Load the array.
            for (int r = 0; r < _num_rows; r++)
            {
                string[] line_r = lines[r].Split(',');
                for (int c = 0; c < _num_cols; c++)
                {
                    _map[r, c] = Byte.Parse(line_r[c]);
                    CreateObject(r, c);
                }
            }
        }

        private void CreateObject(int row, int column)
        {
            switch (_map[row, column])
            {
                case 10: //General-purpose rack (upward/downward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Up));
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Down));
                    }
                    break;

                case 11: //General-purpose rack (leftward/rightward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Left));
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Right));
                    }
                    break;

                case 12: //Hanger rack (leftward/rightward directions)
                    _hangerEmptyRackQueue.Enqueue(new HangerRack(column, row));
                    break;

                case 20: //Receiving point
                    _ReceiverList.Add(new ReceivingRobot(column, row, 0));
                    break;

                case 21: // Shipping point (corresponding to Shipper ID 1 to 4)
                case 22:
                case 23:
                case 24:
                    _ShipperList.Add(new ShippingRobot(column, row, _map[row, column] - 20));
                    break;
                default:
                    break;
            }
        }

        public void LoadItemsFile(string items_file)
        {
            Program.Print("LoadItemsFile\n");

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
            Program.Print("LoadItemCategoriesFile\n");

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


        public void SpecifyProductInitialPosition(List<string> input)
        {
            int count = 0;
            Program.Print("SpecifyProductInitialPosition\n");
            Program.WriteOutput("store");

            for (int i = 1; i < input.Count; i += 2)
            {
                //Debug.WriteLine(i / 2);

                string product_id = input[i];
                int input_quantity = int.Parse(input[i + 1]);
                count += input_quantity;
                Product product_info = new Product((string)_htItems[product_id]);
                if (product_info == null)
                {
                    Program.Print("Can not find product info");
                    continue;
                }

                MaxStorage max_storage_info = new MaxStorage((string)_htMaxStorage[product_info._productType]);
                if (max_storage_info == null)
                {
                    Program.Print("Can not find max storage");
                    continue;
                }

                List<Rack> suitable_racks = FindSuitableRacks(product_info, input_quantity, max_storage_info);

                foreach(Rack rack in suitable_racks)
                {
                    if(rack.isEmpty())
                    {
                        rack._shipperID = product_info._shipperID;
                        rack.SetMaxStorage(max_storage_info);
                        if (rack._storageType.Equals("fold"))
                        {
                            _generalRackList.Add(rack);
                            rack._productType = product_info._productType;
                        }
                        else
                            _hangerRackList.Add(rack);
                    }

                    int stored_quantity = 0;
                    if ((rack._num_items + input_quantity) <= rack._max_storage)
                        stored_quantity = input_quantity;
                    else
                        stored_quantity = rack._max_storage - rack._num_items;

                    rack._itemList.Add(new RackItem(product_id, stored_quantity));
                    rack._num_items += stored_quantity;
                    input_quantity -= stored_quantity;

                    string store_product_command = " " + product_id + " " + rack.GetRackPosition();
                    for (int j = 0; j < stored_quantity; j++)
                        Program.WriteOutput(store_product_command);
                }
            }

            Program.WriteOutput("\n");
            Program.Print("total" + count.ToString());
        }

        private List<Rack> FindSuitableRacks(Product product_info, int quantity, MaxStorage max_storage_info)
        {
            List<Rack> result = new List<Rack>();

            if (product_info._storageType.Equals("fold"))
            {
                int max_storage = max_storage_info._maxFoldStorage;
                while (quantity >= max_storage)
                {
                    if(_generalEmptyRacksQueue.Count > 0)
                    {
                        result.Add(_generalEmptyRacksQueue.Dequeue());
                        quantity -= max_storage;
                    }
                }

                foreach (GeneralPurposeRack rack in _generalRackList)
                {
                    if (quantity <= 0) //Get enough rack to store product.
                        break;

                    // Find rack with the same product type and the same shipper. 
                    if (rack.IsFull() == false &&
                        rack._productType.Equals(product_info._productType) &&
                        (rack._shipperID == product_info._shipperID))
                    {
                        result.Add(rack);
                        quantity -= (rack._max_storage - rack._num_items);
                    }
                }

                if(quantity > 0) //If still not enough rack, get one from the empty racks
                {
                    if (_generalEmptyRacksQueue.Count > 0)
                    {
                        result.Add(_generalEmptyRacksQueue.Dequeue());
                        quantity -= max_storage;
                    }
                    else
                    {
                        Program.Print("Can't find rack to store item");
                    }
                }
            }
            else
            {
                int max_storage = max_storage_info._maxHangerStorage;
                while (quantity >= max_storage)
                {
                    if (_hangerEmptyRackQueue.Count > 0)
                    {
                        result.Add(_hangerEmptyRackQueue.Dequeue());
                        quantity -= max_storage;
                    }
                }

                foreach (HangerRack rack in _hangerRackList)
                {
                    if (quantity <= 0) //Get enough rack to store product.
                        break;

                    // Find rack with the same shipper. 
                    if (rack.IsFull() == false &&
                        (rack._shipperID == product_info._shipperID))
                    {
                        result.Add(rack);
                        quantity -= (rack._max_storage - rack._num_items);
                    }
                }

                if (quantity > 0) //If still not enough rack, get one from the empty racks
                {
                    if (_hangerEmptyRackQueue.Count > 0)
                    {
                        result.Add(_hangerEmptyRackQueue.Dequeue());
                        quantity -= max_storage;
                    }
                    else
                    {
                        Program.Print("Can't find rack to store item");
                    }
                }
            }

            return result;
        }

        public void SpecifyRobotInitialPosition()
        {
            //Program.Print("SpecifyRobotInitialPosition");

            int id = 10;

            // Init transporter
            for (int i = 0; i < 10; i++)
            {
                _TransporterList.Add(new TransportRobot((50 + i), 11, id++));
            }

            for (int i = 0; i < 10; i++)
            {
                _TransporterList.Add(new TransportRobot((62 + i), 11, id++));
            }

            for (int i = 0; i < 10; i++)
            {
                _TransporterList.Add(new TransportRobot(79, 2 + i, id++));
                _TransporterList.Add(new TransportRobot(80, 2 + i, id++));
            }

            // Init Picker robots
            _PickerList.Add(new PickingRobot(18, 26, id++));
            _PickerList.Add(new PickingRobot(18, 30, id++));
            _PickerList.Add(new PickingRobot(18, 36, id++));
            _PickerList.Add(new PickingRobot(18, 40, id++));
            _PickerList.Add(new PickingRobot(18, 44, id++));

            _PickerList.Add(new PickingRobot(47, 26, id++));
            _PickerList.Add(new PickingRobot(47, 30, id++));
            _PickerList.Add(new PickingRobot(47, 36, id++));
            _PickerList.Add(new PickingRobot(47, 40, id++));
            _PickerList.Add(new PickingRobot(47, 44, id++));

            for(int i = 0; i < 16; i++)
            {
                _PickerList.Add(new PickingRobot((84 + i * 4), 13, id++));
            }

            for (int i = 0; i < 14; i++)
            {
                _PickerList.Add(new PickingRobot(155, (50 + i * 4), id++));
            }

            for (int i = 0; i < 9; i++)
            {
                _PickerList.Add(new PickingRobot(133, (74 + i * 4), id++));
            }


            // Init Hanger robots
            for (int i = 0; i < 5; i++)
            {
                _HangerList.Add(new HangerRobot(64 + i * 4, 32, id++));
            }

            for (int i = 0; i < 3; i++)
            {
                _HangerList.Add(new HangerRobot(86 + i * 4, 32, id++));
            }

            for (int i = 0; i < 5; i++)
            {
                _HangerList.Add(new HangerRobot(100 + i * 4, 32, id++));
            }

            for (int i = 0; i < 3; i++)
            {
                _HangerList.Add(new HangerRobot(122 + i * 4, 32, id++));
            }

            for (int i = 0; i < 9; i++)
            {
                _HangerList.Add(new HangerRobot(136 + i * 4, 32, id++));
            }

            Program.Print(_TransporterList.Count.ToString() + " ");
            Program.WriteOutput("conveyor");
            foreach(Robot robot in _TransporterList)
                Program.WriteOutput(" " + robot.GetHexaPosition());
            Program.WriteOutput("\n");

            Program.Print(_PickerList.Count.ToString() + " ");
            Program.WriteOutput("picker");
            foreach (Robot robot in _PickerList)
                Program.WriteOutput(" " + robot.GetHexaPosition());
            Program.WriteOutput("\n");

            Program.Print(_HangerList.Count.ToString() + " ");
            Program.WriteOutput("hanger");
            foreach (Robot robot in _HangerList)
                Program.WriteOutput(" " + robot.GetHexaPosition());
            Program.WriteOutput("\n");
        }

        public void UpdateTime(List<string> input)
        {
            _day = int.Parse(input[0]);
            _time = int.Parse(input[1]);
        }

        public void Pick(List<string> input)
        {

        }

        public void Slot(List<string> input)
        {

        }
    }
}
