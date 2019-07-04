//#define DOCKER

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;

namespace Daiwa
{
    public class Order
    {
        public string _productID;
        public int _quantity;
        public Rack _rack;

        public Order()
        {
            _productID = "";
            _quantity = 0;
            _rack = null;
        }

        public Order(string product, int quantity)
        {
            _productID = product;
            _quantity = quantity;
            _rack = null;
        }

        public Order(string product, int quantity, Rack rack)
        {
            _productID = product;
            _quantity = quantity;
            _rack = rack;
        }
    }

    public class Warehouse
    {
        public static int _day;
        public static int _time;
        public static Byte[,] Map;
        public int _numRows;
        public int _numCols;
        public static Dictionary<string, Product> _DicItems;   // Product information
        public static Dictionary<string, MaxStorage> _DicMaxStorage; // Max storage of products

        // List of racks
        public static List<Rack> _generalRackList; // racks contain product
        public static List<Rack> _hangerRackList;  // racks contain product
        public static List<Rack> _generalEmptyRacksList; // empty rack
        public static List<Rack> _hangerEmptyRackList;   // empty rack

        // List of robots
        public static Dictionary<int, Robot> _Pickers;
        public static Dictionary<int, Robot> _Hangers;
        public static Dictionary<int, Robot> _Transporters;
        public static Dictionary<int, Robot> _TransporterForPick;
        public static Dictionary<int, Robot> _TransporterForSlot;
        public static Dictionary<int, Robot> _AllMovingRobots;
        public static ReceivingRobot _Receiver;
        public static Dictionary<int, Robot> _Shippers;

        public static List<Order> _PickOrders;
        public static List<Order> _SlotOrders;

        public static Random rnd;

        public Warehouse()
        {
            _numRows = 0;
            _numCols = 0;
            Map = null;
            _DicItems = new Dictionary<string, Product>();
            _DicMaxStorage = new Dictionary<string, MaxStorage>();
            _generalRackList = new List<Rack>();
            _hangerRackList = new List<Rack>();
            _generalEmptyRacksList = new List<Rack>();
            _hangerEmptyRackList = new List<Rack>();

            _Pickers = new Dictionary<int, Robot>();
            _Hangers = new Dictionary<int, Robot>();
            _Transporters = new Dictionary<int, Robot>();
            _TransporterForPick = new Dictionary<int, Robot>();
            _TransporterForSlot = new Dictionary<int, Robot>();
            _AllMovingRobots = new Dictionary<int, Robot>();
            _Receiver = null;
            _Shippers = new Dictionary<int, Robot>();

            _PickOrders = new List<Order>();
            _SlotOrders = new List<Order>();

            rnd = new Random();
#if (DOCKER)
            LoadItemsFile("app/data/items.csv");
            LoadItemCategoriesFile("app/data/item_categories.csv");
            LoadMap("app/data/map.csv");
#else
            LoadItemsFile("data\\items.csv");
            LoadItemCategoriesFile("data\\item_categories.csv");
            LoadMap("data\\map.csv");
#endif

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
                    if (c < _num_cols - 1 && c != 0 && r != 0 && r < _num_rows - 1)
                        CreateObject(r, c, line_r[c + 1]);
                }
            }
        }

        private void CreateObject(int row, int column, string wall)
        {
            switch (Map[row, column])
            {
                case 10: //General-purpose rack (upward/downward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalEmptyRacksList.Add(new GeneralPurposeRack(column, row, height, Direction.Up));
                        _generalEmptyRacksList.Add(new GeneralPurposeRack(column, row, height, Direction.Down));
                    }
                    Map[row, column] = 1;
                    break;

                case 11: //General-purpose rack (leftward/rightward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalEmptyRacksList.Add(new GeneralPurposeRack(column, row, height, Direction.Left));
                        if (wall.Equals("1") == false) //walk around for racks at (146, 3)
                            _generalEmptyRacksList.Add(new GeneralPurposeRack(column, row, height, Direction.Right));
                    }
                    Map[row, column] = 1;
                    break;

                case 12: //Hanger rack (leftward/rightward directions)
                    _hangerEmptyRackList.Add(new HangerRack(column, row));
                    Map[row, column] = 1;
                    break;

                case 20: //Receiving point
                    _Receiver = new ReceivingRobot(column, row, 5); // Trick: set id = {5} to avoid 0
                    Map[row, column] = 1;
                    break;

                case 21: // Shipping point (corresponding to Shipper ID 1 to 4)
                case 22:
                case 23:
                case 24:
                    // Trick: set id = {6 7 8 9} to avoid 1 
                    _Shippers.Add(Map[row, column] - 20, new ShippingRobot(column, row, (Byte)(Map[row, column] - 15)));
                    Map[row, column] = 1;
                    break;
                default:
                    break;
            }
        }

        public void LoadItemsFile(string items_file)
        {
            using (var reader = new StreamReader(items_file))
            {
                reader.ReadLine(); // remove the first line
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    Product product = new Product(line);
                    if (product._productID != null)
                        _DicItems.Add(product._productID, product);
                }
            }
        }

        public void LoadItemCategoriesFile(string item_categories_file)
        {
            using (var reader = new StreamReader(item_categories_file))
            {
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    MaxStorage storage = new MaxStorage(line);
                    if (storage._productType != null)
                        _DicMaxStorage.Add(storage._productType, storage);
                }
            }
        }


        public void Store(List<string> input)
        {
            int count = 0;
            Program.WriteOutput("store");

            for (int i = 1; i < input.Count; i += 2)
            {
                string product_id = input[i];
                int input_quantity = int.Parse(input[i + 1]);
                count++;
                Product product = _DicItems[product_id];
                if (product == null)
                {
                    Program.Print("Can not find product info");
                    continue;
                }

                MaxStorage max_storage_info = _DicMaxStorage[product._productType];
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
                    if ((rack._num_items + input_quantity) <= rack._max_item)
                        stored_quantity = input_quantity;
                    else
                        stored_quantity = rack._max_item - rack._num_items;

                    rack._itemList.Add(new RackItem(product_id, stored_quantity));
                    rack._num_items += stored_quantity;
                    input_quantity -= stored_quantity;

                    string store_product_command = String.Concat(Enumerable.Repeat(" " + product_id + " " + rack.GetXXYYDH(), stored_quantity));
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
                while (quantity >= 10)
                {
                    if (_generalEmptyRacksList.Count > 0)
                    {
                        result.Add(_generalEmptyRacksList[0]);
                        _generalEmptyRacksList.RemoveAt(0);
                        quantity -= max_storage;
                    }
                    else
                        break;
                }

                // Find empty spot in the racks which contain product
                int start = rnd.Next(_generalRackList.Count);
                for (int i = 0; i < _generalRackList.Count; i++)
                {
                    int pos = (i + start) % _generalRackList.Count;
                    Rack rack = _generalRackList[pos];

                    if (quantity <= 0) //Get enough rack to store product.
                        break;

                    // Find rack with the same product type and the same shipper. 
                    if (rack.IsFull() == false &&
                        rack._productType.Equals(product_info._productType) &&
                        (rack._shipperID == product_info._shipperID))
                    {
                        result.Add(rack);
                        quantity -= (rack._max_item - rack._num_items);
                    }
                }

                if (quantity > 0) //If still not enough empty spot, get one from the empty racks
                {
                    if (_generalEmptyRacksList.Count > 0)
                    {
                        result.Add(_generalEmptyRacksList[0]);
                        _generalEmptyRacksList.RemoveAt(0);
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
                while (quantity >= 30)
                {
                    if (_hangerEmptyRackList.Count > 0)
                    {
                        result.Add(_hangerEmptyRackList[0]);
                        _hangerEmptyRackList.RemoveAt(0);
                        quantity -= max_storage;
                    }
                    else
                        break;
                }

                int start = rnd.Next(_hangerRackList.Count);
                // Find empty spot in the racks which contain product
                for (int i = 0; i < _hangerRackList.Count; i++)
                {
                    int pos = (i + start) % _hangerRackList.Count;
                    Rack rack = _hangerRackList[pos];

                    if (quantity <= 0) //Get enough rack to store product.
                        break;

                    // Find rack with the same shipper. 
                    if (rack.IsFull() == false &&
                        (rack._shipperID == product_info._shipperID))
                    {
                        result.Add(rack);
                        quantity -= (rack._max_item - rack._num_items);
                    }
                }

                if (quantity > 0) //If still not enough rack, get one from the empty racks
                {
                    if (_hangerEmptyRackList.Count > 0)
                    {
                        result.Add(_hangerEmptyRackList[0]);
                        _hangerEmptyRackList.RemoveAt(0);
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

            // Init 12 transporters
            _TransporterForPick.Add(id, new TransportRobot(46, 33, id++));
            _TransporterForPick.Add(id, new TransportRobot(62, 31, id++));
            //_TransporterForPick.Add(id, new TransportRobot(83, 13, id++));
            //_TransporterForPick.Add(id, new TransportRobot(83, 22, id++));
            //_TransporterForPick.Add(id, new TransportRobot(83, 31, id++));
            //_TransporterForPick.Add(id, new TransportRobot(83, 33, id++));

            for (int i = 0; i < 3; i++)
            {
                _TransporterForSlot.Add(id, new TransportRobot(77, 2 + i, id++));
            }

            _Pickers.Add(id, new PickingRobot(47, 1, id++));
            _Pickers.Add(id, new PickingRobot(18, 1, id++));
            for (int i = 0; i < 6; i++)
            {
                _Pickers.Add(id, new PickingRobot(90 + i * 10, 14, id++));
            }

            // Init 8 Hangers
            for (int i = 0; i < 8; i++)
            {
                _Hangers.Add(id, new HangingRobot(65 + i * 10, 32, id++));
            }

            foreach (KeyValuePair<int, Robot> entry in _TransporterForPick)
            {
                _Transporters.Add(entry.Key, entry.Value);
                _AllMovingRobots.Add(entry.Key, entry.Value);
            }

            foreach (KeyValuePair<int, Robot> entry in _TransporterForSlot)
            {
                _Transporters.Add(entry.Key, entry.Value);
                _AllMovingRobots.Add(entry.Key, entry.Value);
            }

            foreach (KeyValuePair<int, Robot> entry in _Pickers)
            {
                _AllMovingRobots.Add(entry.Key, entry.Value);
            }

            foreach (KeyValuePair<int, Robot> entry in _Hangers)
            {
                _AllMovingRobots.Add(entry.Key, entry.Value);
            }

            Program.WriteOutput("conveyor");
            foreach (Robot robot in _Transporters.Values)
                Program.WriteOutput(" " + robot.GetHexaPosition());
            Program.WriteOutput("\n");

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
            for (int i = 1; i < input.Count; i += 2) // Add new order into the list
            {
                Order new_order = new Order(input[i], int.Parse(input[i + 1]));
                Order existing_order = _PickOrders.FirstOrDefault(x => x._productID.Equals(new_order._productID));
                if (existing_order != null)
                {
                    existing_order._quantity += new_order._quantity;
                }
                else
                {
                    _PickOrders.Add(new_order);
                }
            }

            if (_time == 0 && _day != 0) // resume the activity of previous day
            {
                foreach (Robot robot in _AllMovingRobots.Values)
                {
                    robot.ResumeActivityLastDay();
                }
            }

            if (_time < 714)
            {
                while (_PickOrders.Count > 0)
                {
                    Order order = FindOrderToHandle();
                    int NumItemInRack = 0;

                    // Find rack contains order product
                    Rack rack = FindRackToPick(order, out NumItemInRack);
                    if (rack == null)
                    {
                        Program.PrintLine("Can not find rack contain the product");
                        _PickOrders.Remove(order);
                        continue;
                    }

                    // Get the pickup point of rack
                    Point pickup_point = rack.GetPickUpPoint();

                    // Find picking robot which is free and near rack
                    Robot picker = FindPickerToPick(rack);
                    if (picker == null) // All pickers are busy
                    {
                        //Program.Print("All pickers are busy");
                        return;
                    }

                    TransportRobot transporter = FindTransporterToPick(rack._location);
                    if (transporter == null) // All transporters are busy
                    {
                        //Program.Print("All transporters are busy");
                        return;
                    }

                    rack._expectedPickQuantity = (order._quantity > NumItemInRack) ? NumItemInRack : order._quantity;
                    order._quantity -= rack._expectedPickQuantity;
                    if (order._quantity <= 0) // Get enought quantity, remove order from the list of orders
                        _PickOrders.Remove(order);

                    picker.PrepareToPick(pickup_point, rack, order._productID, rack._expectedPickQuantity);
                    transporter.PrepareToPick(pickup_point, rack, order._productID, rack._expectedPickQuantity);
                    Program.Print("Handle pick " + order._productID + " " + rack._expectedPickQuantity);
                }
            }
            //else
            //{
            //    foreach (Robot robot in _AllMovingRobots.Values)
            //    {
            //        robot.ForceReturnChargingPoint();
            //    }
            //}
        }

        public Order FindOrderToHandle()
        {
            Order result = _PickOrders[0];
            foreach(Order order in _PickOrders)
            {
                if (order._quantity > result._quantity)
                    result = order;
            }

            return result;
        }

        public static bool AvoidTrafficJam(Rack rack)
        {
            Point pick_point = rack.GetPickUpPoint();

            foreach (Robot robot in _AllMovingRobots.Values)
            {
                if (robot._state != robot_state.free)
                {
                    if (rack._direction == Direction.Left ||
                   rack._direction == Direction.Right ||
                   rack._direction == Direction.Fix)
                    {
                        if (pick_point.X == robot._destination_point.X || pick_point.X == robot._location.X)
                            return false;
                    }
                    else
                    {
                        if (pick_point.Y == robot._destination_point.Y || pick_point.Y == robot._location.Y)
                            return false;
                    }
                }
            }

            //Program.Print("\n" + rack.GetPickUpPoint() + " avoid ");
            //foreach (Robot robot in _AllMovingRobots.Values)
            //    if (robot._state != robot_state.free)
            //        Program.Print(" " + robot._location + robot._destination_point);
            return true;
        }

        public static Rack FindRackToSlot(String product_id, int quantity)
        {
            Product product_info = _DicItems[product_id];
            MaxStorage max_storage_info = _DicMaxStorage[product_info._productType];

            Rack result = null;

            if (product_info._storageType.Equals("fold"))
            {
                int start = rnd.Next(_generalRackList.Count);
                // Find empty spot in the racks which contain product
                for (int i = 0; i < _generalRackList.Count; i++)
                {
                    int pos = (i + start) % _generalRackList.Count;
                    Rack rack = _generalRackList[pos];

                    // Find rack with the same product type and the same shipper. 
                    if (rack._productType.Equals(product_info._productType) &&
                        (rack._shipperID == product_info._shipperID) &&
                        rack.IsEnoughSpaceToSlot(quantity) && AvoidTrafficJam(rack))
                    {
                        result = rack;
                        break;
                    }
                }

                // If can't find empty spot from rack with product, get a new empty rack
                if (result == null && _generalEmptyRacksList.Count > 0)
                {
                    foreach (Rack emptyrack in _generalEmptyRacksList)
                    {
                        if (AvoidTrafficJam(emptyrack))
                        {
                            result = emptyrack;
                            _generalEmptyRacksList.Remove(result);
                            break;
                        }
                    }
                }
            }
            else
            {
                int start = rnd.Next(_hangerRackList.Count);
                // Find empty spot in the racks which contain product
                for (int i = 0; i < _hangerRackList.Count; i++)
                {
                    int pos = (i + start) % _hangerRackList.Count;
                    Rack rack = _hangerRackList[pos];

                    // Find rack with the same shipper. 
                    if (rack._shipperID == product_info._shipperID &&
                        rack.IsEnoughSpaceToSlot(quantity) && AvoidTrafficJam(rack))
                    {
                        result = rack;
                    }
                }

                if (result == null && _hangerEmptyRackList.Count > 0)
                {
                    foreach (Rack emptyrack in _hangerEmptyRackList)
                    {
                        if (AvoidTrafficJam(emptyrack))
                        {
                            result = emptyrack;
                            _hangerEmptyRackList.Remove(result);
                            break;
                        }
                    }
                }
            }

            if (result != null)
            {
                result._expectedSlotQuantity += quantity;

                if (result.isEmpty())
                {
                    result._shipperID = product_info._shipperID;
                    result.SetMaxStorage(max_storage_info);
                    if (result._storageType.Equals("fold"))
                    {
                        _generalRackList.Add(result);
                        result._productType = product_info._productType;
                    }
                    else
                        _hangerRackList.Add(result);
                }
            }

            Program.Print("\nFind rack to slot: " + result._storageType + " " + result.GetPickUpPoint() + " " + result._num_items + "\n");
            return result;
        }

        public static Rack FindRackToPick(Order order, out int NumItemInRack)
        {
            Rack result = null;
            NumItemInRack = 0;

            if (_DicItems.ContainsKey(order._productID) == false)
            {
                Program.Print("Can not find product info");
                return result;
            }

            Product product = _DicItems[order._productID];
            List<Rack> searchList = product._storageType.Equals("fold") ? _generalRackList : _hangerRackList;

            int start = rnd.Next(searchList.Count);
            // Find empty spot in the racks which contain product
            for (int i = 0; i < searchList.Count; i++)
            {
                int pos = (i + start) % searchList.Count;
                Rack rack = searchList[pos];

                NumItemInRack = rack.GetNumberOfItem(order._productID);

                // Find the correct product 
                if (NumItemInRack > 0 && AvoidTrafficJam(rack))
                {
                    result = rack;
                    Program.Print("\nRack to pick: " + result._storageType + " " + result.GetXXYYDH() + " " + order._productID + ":" + NumItemInRack + "\n");
                    break;
                }
            }


            return result;
        }

        public static Robot FindPickerToPick(Rack rack)
        {
            Robot select_robot = null;
            int current_distance = 0;

            Dictionary<int, Robot> searchList = rack._storageType.Equals("fold") ? _Pickers : _Hangers;

            foreach (Robot robot in searchList.Values)
            {
                int new_distance = AStarPathfinding.ComputeHScore(robot._location.X, robot._location.Y, rack._location.X, rack._location.Y);
                if (robot._state == robot_state.free || robot._state == robot_state.returning)
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

        private TransportRobot FindTransporterToPick(Point location)
        {
            TransportRobot select_robot = null;
            int current_distance = 0;
            foreach (TransportRobot robot in _TransporterForPick.Values)
            {
                if (robot._state == robot_state.free || robot._state == robot_state.returning)
                {
                    int new_distance = AStarPathfinding.ComputeHScore(robot._location.X, robot._location.Y, location.X, location.Y);

                    if (select_robot == null || new_distance < current_distance)
                    {
                        select_robot = robot;
                        current_distance = new_distance;
                    }
                }
            }

            return select_robot;
        }

        private TransportRobot FindTransporterToSlot(Point location)
        {
            TransportRobot select_robot = null;
            int current_distance = 0;
            foreach (TransportRobot robot in _TransporterForSlot.Values)
            {
                if (robot._state == robot_state.free || robot._state == robot_state.returning)
                {
                    int new_distance = AStarPathfinding.ComputeHScore(robot._location.X, robot._location.Y, location.X, location.Y);

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
            for (int i = 1; i < input.Count; i += 2) // Add new order into the list
            {
                Order new_order = new Order(input[i], int.Parse(input[i + 1]));
                Order existing_order = _SlotOrders.FirstOrDefault(x => x._productID.Equals(new_order._productID));
                if (existing_order != null)
                {
                    existing_order._quantity += new_order._quantity;
                }
                else
                {
                    _SlotOrders.Add(new_order);
                }
            }

            //// Need to remove here
            //if (_time == 0 && _day != 0) // resume the activity of previous day
            //{
            //    foreach (Robot robot in _AllMovingRobots.Values)
            //    {
            //        robot.ResumeActivityLastDay();
            //    }
            //}

            if (_time < 714)
            {
                while (_SlotOrders.Count > 0)
                {
                    TransportRobot transporter = FindTransporterToSlot(_Receiver._location);
                    if (transporter == null) // All transporters are busy
                    {
                        return;
                    }

                    while (transporter._expectedReceiveItems.Count < TransportRobot._maxItem)
                    {
                        Order order = FindNextSlotOrder(transporter);
                        if (order == null)
                            break;
                        transporter.UpdateExpectedReceiveItem(order);
                        if (order._quantity == 0)
                        {
                            _SlotOrders.Remove(order);
                        }
                    }

                    transporter.PrepareToReceive();
                }
            }
            else
            {
                foreach (Robot robot in _AllMovingRobots.Values)
                {
                    robot.ForceReturnChargingPoint();
                }
            }
        }

        public Order FindNextSlotOrder(TransportRobot robot)
        {
            if (_SlotOrders.Count == 0)
                return null;

            if (robot._expectedReceiveItems.Count == 0)
            {
                return _SlotOrders[0];
            }

            string product_id = robot._expectedReceiveItems.Peek();
            Product current_product = _DicItems[product_id];
            if (current_product._storageType.Equals("fold"))
            {
                foreach (Order order in _SlotOrders)
                {
                    Product another_product = _DicItems[order._productID];

                    if (another_product._storageType.Equals(current_product._storageType) &&
                        another_product._productType.Equals(current_product._productType) &&
                        another_product._shipperID == current_product._shipperID)
                        return order;
                }
            }
            else
            {
                foreach (Order order in _SlotOrders)
                {
                    Product another_product = _DicItems[order._productID];
                    if (another_product._storageType.Equals(current_product._storageType) &&
                        another_product._shipperID == current_product._shipperID)
                        return order;
                }
            }
            return null;
        }

        public static Order FindNextPickOrder(TransportRobot robot)
        {
            if (_PickOrders.Count == 0)
                return null;

            string product_id = robot._loadedItems.Peek();
            Product current_product = _DicItems[product_id];

            foreach (Order order in _PickOrders)
            {
                Product another_product = _DicItems[order._productID];
                if (another_product._shipperID == current_product._shipperID)
                    return order;
            }
            return null;
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

                foreach (Robot robot in _AllMovingRobots.Values)
                {
                    robot.GenerateAction(i);
                }
            }

            Program.WriteOutput(_Receiver._actionString + "\n");
            Program.Print(_Receiver._actionString + "\n");

            foreach (Robot robot in _Shippers.Values)
            {
                Program.WriteOutput(robot._actionString + "\n");
                Program.Print(robot._actionString + "\n");
            }

            foreach (Robot robot in _AllMovingRobots.Values)
            {
                Program.WriteOutput(robot._actionString + "\n");
                if (robot._state != robot_state.free)
                {
                    string debug = robot._actionString + " " + robot.type + " " + robot._state + " " + robot._location + robot._direction + " to " + robot._destination_point + " " + robot._path.Count + "\n";
                    Program.Print(debug);
                }
            }
        }

        public static Byte ValueAt(Point location)
        {
            return Warehouse.Map[location.Y, location.X];
        }
    }
}
