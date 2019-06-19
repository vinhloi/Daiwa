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
    public struct Order
    {
        public string _productID;
        public int _quantity;
        public string _rackID;

        public Order(string product, int quantity)
        {
            _productID = product;
            _quantity = quantity;
            _rackID = "";
        }

        public Order(string product, int quantity, string rack)
        {
            _productID = product;
            _quantity = quantity;
            _rackID = rack;
        }
    }

    public class Warehouse
    {
        public int _day;
        public int _time;
        public static Byte[,] Map;
        public int _numRows;
        public int _numCols;
        public static Dictionary<string, string> _DicItems;   // Product information
        Dictionary<string, string> _DicMaxStorage; // Max storage of products

        // List of racks
        public List<Rack> _generalRackList; // racks contain product
        public List<Rack> _hangerRackList;  // racks contain product
        public Queue<Rack> _generalEmptyRacksQueue; // empty rack
        public Queue<Rack> _hangerEmptyRackQueue;   // empty rack

        // List of robots
        public static Dictionary<int, Robot> _Pickers;
        public static Dictionary<int, Robot> _Hangers;
        public static Dictionary<int, Robot> _Transporters;
        public static ReceivingRobot _Receiver;
        public static Dictionary<int, Robot> _Shippers;

        public Queue<Order> _PickOrders;
        public Queue<Order> _SlotOrders;

        public Warehouse()
        {
            _numRows = 0;
            _numCols = 0;
            Map = null;
            _DicItems = new Dictionary<string, string>();
            _DicMaxStorage = new Dictionary<string, string>();
            _generalRackList = new List<Rack>();
            _hangerRackList = new List<Rack>();
            _generalEmptyRacksQueue = new Queue<Rack>();
            _hangerEmptyRackQueue = new Queue<Rack>();

            _Pickers = new Dictionary<int, Robot>();
            _Hangers = new Dictionary<int, Robot>();
            _Transporters = new Dictionary<int, Robot>();
            _Receiver = null;
            _Shippers = new Dictionary<int, Robot>();

            _PickOrders = new Queue<Order>();
            _SlotOrders = new Queue<Order>();

            LoadItemsFile("data\\items.csv");
            LoadItemCategoriesFile("data\\item_categories.csv");
            LoadMap("data\\map.csv");
        }

        public void LoadMap(string map_file)
        {
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
            Map = new Byte[_num_rows, _num_cols];

            // Load the array.
            for (int r = 0; r < _num_rows; r++)
            {
                string[] line_r = lines[r].Split(',');
                for (int c = 0; c < _num_cols; c++)
                {
                    Map[r, c] = Byte.Parse(line_r[c]);
                    CreateObject(r, c);
                }
            }
        }

        private void CreateObject(int row, int column)
        {
            switch (Map[row, column])
            {
                case 10: //General-purpose rack (upward/downward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Up));
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Down));
                    }
                    Map[row, column] = 1;
                    break;

                case 11: //General-purpose rack (leftward/rightward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Left));
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Right));
                    }
                    Map[row, column] = 1;
                    break;

                case 12: //Hanger rack (leftward/rightward directions)
                    _hangerEmptyRackQueue.Enqueue(new HangerRack(column, row));
                    Map[row, column] = 1;
                    break;

                case 20: //Receiving point
                    _Receiver = new ReceivingRobot(column, row, 5); // Trick: set id = {5} to avoid 0
                    break;

                case 21: // Shipping point (corresponding to Shipper ID 1 to 4)
                case 22:
                case 23:
                case 24:
                    // Trick: set id = {6 7 8 9} to avoid 1 
                    _Shippers.Add(Map[row, column] - 20, new ShippingRobot(column, row, (Byte)(Map[row, column] - 15)));
                    break;
                default:
                    break;
            }
        }

        public void LoadItemsFile(string items_file)
        {
            using (var reader = new StreamReader(items_file))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    _DicItems.Add(values[1], line);
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

                    _DicMaxStorage.Add(values[0], line);
                }
            }
        }


        public void SpecifyProductInitialPosition(List<string> input)
        {
            int count = 0;
            Program.WriteOutput("store");

            for (int i = 1; i < input.Count; i += 2)
            {
                string product_id = input[i];
                int input_quantity = int.Parse(input[i + 1]);
                count++;
                Product product = new Product((string)_DicItems[product_id]);
                if (product == null)
                {
                    Program.Print("Can not find product info");
                    continue;
                }

                MaxStorage max_storage_info = new MaxStorage((string)_DicMaxStorage[product._productType]);
                if (max_storage_info == null)
                {
                    Program.Print("Can not find max storage");
                    continue;
                }

                List<Rack> suitable_racks = FindRackToStore(product, input_quantity, max_storage_info);

                foreach (Rack rack in suitable_racks)
                {
                    if (rack.isEmpty())
                    {
                        rack._shipperID = product._shipperID;
                        rack.SetMaxStorage(max_storage_info);
                        if (rack._storageType.Equals("fold"))
                        {
                            _generalRackList.Add(rack);
                            rack._productType = product._productType;
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

                    string store_product_command = " " + product_id + " " + rack.GetXXYYDH();
                    for (int j = 0; j < stored_quantity; j++)
                        Program.WriteOutput(store_product_command);
                }
            }

            Program.WriteOutput("\n");
            Program.Print("stock total: " + count.ToString());
        }

        private List<Rack> FindRackToStore(Product product_info, int quantity, MaxStorage max_storage_info)
        {
            List<Rack> result = new List<Rack>();

            if (product_info._storageType.Equals("fold"))
            {
                int max_storage = max_storage_info._maxFoldStorage;
                while (quantity >= max_storage)
                {
                    if (_generalEmptyRacksQueue.Count > 0)
                    {
                        result.Add(_generalEmptyRacksQueue.Dequeue());
                        quantity -= max_storage;
                    }
                }

                // Find empty spot in the racks which contain product
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

                if (quantity > 0) //If still not enough empty spot, get one from the empty racks
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

            Byte id = 10;

            _Transporters.Add(id, new TransportRobot(82, 1, id++));
            _Pickers.Add(id, new PickingRobot(82, 2, id++));
            _Hangers.Add(id, new HangingRobot(64, 25, id++));

            //// Init transporter
            //for (int i = 0; i < 10; i++)
            //{
            //    _DicTransporter.Add(id, new TransportRobot((50 + i), 11, id++));
            //}

            //for (int i = 0; i < 10; i++)
            //{
            //    _DicTransporter.Add(id, new TransportRobot((62 + i), 11, id++));
            //}

            //for (int i = 0; i < 10; i++)
            //{
            //    _DicTransporter.Add(id, new TransportRobot(79, 2 + i, id++));
            //    _DicTransporter.Add(id, new TransportRobot(80, 2 + i, id++));
            //}

            //// Init Picker robots
            //_DicPicker.Add(id, new PickingRobot(18, 26, id++));
            //_DicPicker.Add(id, new PickingRobot(18, 30, id++));
            //_DicPicker.Add(id, new PickingRobot(18, 36, id++));
            //_DicPicker.Add(id, new PickingRobot(18, 40, id++));
            //_DicPicker.Add(id, new PickingRobot(18, 44, id++));

            //_DicPicker.Add(id, new PickingRobot(47, 26, id++));
            //_DicPicker.Add(id, new PickingRobot(47, 30, id++));
            //_DicPicker.Add(id, new PickingRobot(47, 36, id++));
            //_DicPicker.Add(id, new PickingRobot(47, 40, id++));
            //_DicPicker.Add(id, new PickingRobot(47, 44, id++));

            //for (int i = 0; i < 16; i++)
            //{
            //    _DicPicker.Add(id, new PickingRobot((84 + i * 4), 13, id++));
            //}

            //for (int i = 0; i < 14; i++)
            //{
            //    _DicPicker.Add(id, new PickingRobot(155, (50 + i * 4), id++));
            //}

            //for (int i = 0; i < 9; i++)
            //{
            //    _DicPicker.Add(id, new PickingRobot(133, (74 + i * 4), id++));
            //}


            //// Init Hanger robots
            //for (int i = 0; i < 5; i++)
            //{
            //    _DicHanger.Add(id, new HangerRobot(64 + i * 4, 32, id++));
            //}

            //for (int i = 0; i < 3; i++)
            //{
            //    _DicHanger.Add(id, new HangerRobot(86 + i * 4, 32, id++));
            //}

            //for (int i = 0; i < 5; i++)
            //{
            //    _DicHanger.Add(id, new HangerRobot(100 + i * 4, 32, id++));
            //}

            //for (int i = 0; i < 3; i++)
            //{
            //    _DicHanger.Add(id, new HangerRobot(122 + i * 4, 32, id++));
            //}

            //for (int i = 0; i < 9; i++)
            //{
            //    _DicHanger.Add(id, new HangerRobot(136 + i * 4, 32, id++));
            //}

            //Program.Print(_DicTransporter.Count.ToString() + " ");
            Program.WriteOutput("conveyor");
            foreach (Robot robot in _Transporters.Values)
                Program.WriteOutput(" " + robot.GetHexaPosition());
            Program.WriteOutput("\n");

            //Program.Print(_DicPicker.Count.ToString() + " ");
            Program.WriteOutput("picker");
            foreach (Robot robot in _Pickers.Values)
                Program.WriteOutput(" " + robot.GetHexaPosition());
            Program.WriteOutput("\n");

            Program.WriteOutput("hanger");
            foreach (Robot robot in _Hangers.Values)
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
            for (int i = 1; i < input.Count; i += 2)
            {
                _PickOrders.Enqueue(new Order(input[i], int.Parse(input[i + 1])));
            }

            while (_PickOrders.Count() > 0)
            {
                Order order = _PickOrders.Peek();

                Product product_info = new Product((string)_DicItems[order._productID]);
                if (product_info == null)
                {
                    Program.Print("Can not find product info");
                    continue;
                }

                // Find racks to get enought quanity of product
                List<Rack> rack_to_pick = FindRackToPick(product_info, order._quantity);
                if (rack_to_pick.Count == 0)
                    return;

                foreach (Rack rack in rack_to_pick)
                {
                    // Get the pickup point of rack
                    Point pickup_point = rack.GetPickUpPoint();

                    // Find picking robot which is free and near rack
                    Robot picker = FindRobotToPick(rack);
                    if (picker == null) // All pickers are busy
                    {
                        // vinh: need to consider here.
                        return;
                    }
                    picker.PrepareToPick(pickup_point, rack.GetXXYYDH(), order._productID, rack._orderedQuantity);

                    while (rack._orderedQuantity > 0)
                    {
                        TransportRobot transporter = FindRobotToTransport(rack);
                        if (transporter == null) // All transporters are busy
                        {
                            return;
                        }

                        transporter.PrepareToPick(pickup_point, order._productID);
                        rack._orderedQuantity -= TransportRobot._maxItem;
                    }
                }

                // Find robot to execute order. remove from queue.
                _PickOrders.Dequeue();
            }
        }

        private List<Rack> FindRackToPick(Product product, int quantity)
        {
            List<Rack> result = new List<Rack>();
            List<Rack> searchList = product._storageType.Equals("fold") ? _generalRackList : _hangerRackList;

            // Loop through the rack list
            foreach (Rack rack in searchList)
            {
                // Loop through the item list of the rack
                foreach (RackItem item in rack._itemList)
                {
                    // Find the correct product 
                    if (item._productID.Equals(product._productID) && item._quantity > 0)
                    {
                        rack._orderedQuantity = (quantity > item._quantity) ? item._quantity : quantity; 
                        result.Add(rack);
                        quantity -= item._quantity;
                        if (quantity <= 0) // Get enought quantity, return the list
                            return result;
                    }
                }
            }

            return result;
        }

        private Robot FindRobotToPick(Rack rack)
        {
            Robot select_robot = null;
            int current_distance = 0;

            Dictionary<int, Robot> searchList = rack._storageType.Equals("fold") ? _Pickers : _Hangers;

            foreach (Robot robot in searchList.Values)
            {
                int new_distance = AStarPathfinding.ComputeHScore(robot._location.X, robot._location.Y, rack._location.X, rack._location.Y);
                if (robot._state == robot_state.free)
                {
                    if (select_robot == null || new_distance < current_distance)
                    {
                        select_robot = robot;
                        current_distance = new_distance;
                    }
                }
            }

            return select_robot;
        }

        private TransportRobot FindRobotToTransport(Rack rack)
        {
            TransportRobot select_robot = null;
            int current_distance = 0;
            foreach (TransportRobot robot in _Transporters.Values)
            {
                int new_distance = AStarPathfinding.ComputeHScore(robot._location.X, robot._location.Y, rack._location.X, rack._location.Y);
                if (robot._state == robot_state.free)
                {
                    if (select_robot == null || new_distance < current_distance)
                    {
                        select_robot = robot;
                        current_distance = new_distance;
                    }
                }
            }

            return select_robot;
        }

        public void Slot(List<string> input)
        {
        }

        public void GenerateAction()
        {
            for (int i = 0; i < 60; i++)
            {
                _Receiver.GenerateAction(i);

                foreach (Robot robot in _Shippers.Values)
                {
                    robot.GenerateAction(i);
                }

                foreach (Robot robot in _Transporters.Values)
                {
                    robot.GenerateAction(i);
                }

                foreach (Robot robot in _Pickers.Values)
                {
                    robot.GenerateAction(i);
                }

                foreach (Robot robot in _Hangers.Values)
                {
                    robot.GenerateAction(i);
                }
            }

            Program.WriteOutput(_Receiver._actionString);

            foreach (Robot robot in _Shippers.Values)
            {
                Program.WriteOutput(robot._actionString);
            }

            foreach (Robot robot in _Transporters.Values)
            {
                Program.WriteOutput(robot._actionString);
            }

            foreach (Robot robot in _Pickers.Values)
            {
                Program.WriteOutput(robot._actionString);
            }

            foreach (Robot robot in _Hangers.Values)
            {
                Program.WriteOutput(robot._actionString);
            }
        }

        public static Byte ValueAt(Point location)
        {
            return Warehouse.Map[location.Y, location.X];
        }
    }
}
