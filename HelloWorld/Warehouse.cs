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
        public int _day;
        public int _time;
        public static Byte[,] Map;
        public int _numRows;
        public int _numCols;
        public static Dictionary<string, string> _DicItems;   // Product information
        public static Dictionary<string, string> _DicMaxStorage; // Max storage of products

        // List of racks
        public static List<Rack> _generalRackList; // racks contain product
        public static List<Rack> _hangerRackList;  // racks contain product
        public static Queue<Rack> _generalEmptyRacksQueue; // empty rack
        public static Queue<Rack> _hangerEmptyRackQueue;   // empty rack

        // List of robots
        public static Dictionary<int, Robot> _Pickers;
        public static Dictionary<int, Robot> _Hangers;
        public static Dictionary<int, Robot> _Transporters;
        public static Dictionary<int, Robot> _AllMovingRobots;
        public static ReceivingRobot _Receiver;
        public static Dictionary<int, Robot> _Shippers;

        public List<Order> _PickOrders;
        public List<Order> _SlotOrders;

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
            _AllMovingRobots = new Dictionary<int, Robot>();
            _Receiver = null;
            _Shippers = new Dictionary<int, Robot>();

            _PickOrders = new List<Order>();
            _SlotOrders = new List<Order>();

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
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Up));
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Down));
                    }
                    Map[row, column] = 1;
                    break;

                case 11: //General-purpose rack (leftward/rightward directions)
                    for (int height = 1; height <= 5; height++)
                    {
                        _generalEmptyRacksQueue.Enqueue(new GeneralPurposeRack(column, row, height, Direction.Left));
                        if (wall.Equals("1") == false) //walk around for racks at (146, 3)
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
                Product product = new Product(_DicItems[product_id]);
                if (product == null)
                {
                    Program.Print("Can not find product info");
                    continue;
                }

                MaxStorage max_storage_info = new MaxStorage(_DicMaxStorage[product._productType]);
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
                        quantity -= (rack._max_item - rack._num_items);
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
                        quantity -= (rack._max_item - rack._num_items);
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

            //_Transporters.Add(id, new TransportRobot(81, 13, id++));
            //_Pickers.Add(id, new PickingRobot(82, 13, id++));
            //_Hangers.Add(id, new HangingRobot(83, 13, id++));
            
            // Init 30 transporters
            for (int i = 0; i < 10; i++)
            {
                _Transporters.Add(id, new TransportRobot(77, 2 + i, id++));
                _Transporters.Add(id, new TransportRobot(79, 2 + i, id++));
                _Transporters.Add(id, new TransportRobot(81, 2 + i, id++));
            }

            // Init 33 Pickers
            _Pickers.Add(id, new PickingRobot(18, 26, id++));
            _Pickers.Add(id, new PickingRobot(18, 30, id++));
            _Pickers.Add(id, new PickingRobot(18, 36, id++));
            _Pickers.Add(id, new PickingRobot(18, 40, id++));
            _Pickers.Add(id, new PickingRobot(18, 44, id++));
            _Pickers.Add(id, new PickingRobot(47, 26, id++));
            _Pickers.Add(id, new PickingRobot(47, 30, id++));
            _Pickers.Add(id, new PickingRobot(47, 36, id++));
            _Pickers.Add(id, new PickingRobot(47, 40, id++));
            _Pickers.Add(id, new PickingRobot(47, 44, id++));

            for (int i = 0; i < 16; i++)
            {
                _Pickers.Add(id, new PickingRobot((84 + i * 4), 13, id++));
            }

            for (int i = 0; i < 7; i++)
            {
                _Pickers.Add(id, new PickingRobot(155, (50 + i * 8), id++));
            }

            //for (int i = 0; i < 9; i++)
            //{
            //    _Pickers.Add(id, new PickingRobot(133, (74 + i * 4), id++));
            //}


            // Init 23 Hangers
            for (int i = 0; i < 4; i++)
            {
                _Hangers.Add(id, new HangingRobot(64 + i * 4, 32, id++));
            }

            for (int i = 0; i < 3; i++)
            {
                _Hangers.Add(id, new HangingRobot(86 + i * 4, 32, id++));
            }

            for (int i = 0; i < 5; i++)
            {
                _Hangers.Add(id, new HangingRobot(100 + i * 4, 32, id++));
            }

            for (int i = 0; i < 3; i++)
            {
                _Hangers.Add(id, new HangingRobot(122 + i * 4, 32, id++));
            }

            for (int i = 0; i < 8; i++)
            {
                _Hangers.Add(id, new HangingRobot(136 + i * 4, 32, id++));
            }

            //Program.Print(_Transporters.Count.ToString() + " ");

            foreach (KeyValuePair<int, Robot> entry in _Transporters)
            {
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
                _PickOrders.Insert(0, new Order(input[i], int.Parse(input[i + 1])));
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
                // Start solving order from the list, including the old orders
                for (int i = _PickOrders.Count - 1; i >= 0; i--)
                {
                    if (HandlePickOrder(_PickOrders[i]) == false)
                        return;
                    else
                        _PickOrders.RemoveAt(i);
                }
            }
        }
        
        private bool HandlePickOrder(Order order)
        {
            Product product_info = new Product((string)_DicItems[order._productID]);
            if (product_info == null)
            {
                Program.Print("Can not find product info");
                return true;
            }

            // Find racks to get enought quantity of product
            List<Rack> rack_to_pick = FindRackToPick(product_info, order._quantity);
            if (rack_to_pick.Count == 0)
            {
                Program.Print("Can not find rack");
                return true;
            }

            foreach (Rack rack in rack_to_pick)
            {
                // Get the pickup point of rack
                Point pickup_point = rack.GetPickUpPoint();

                // Find picking robot which is free and near rack
                Robot picker = FindPickerToPick(rack);
                if (picker == null) // All pickers are busy
                {
                    //Program.Print("All pickers are busy");
                    return false;
                }

                TransportRobot transporter = FindTransporterToPick(rack._location);
                if (transporter == null) // All transporters are busy
                {
                    //Program.Print("All transporters are busy");
                    return false;
                }

                picker.PrepareToPick(pickup_point, rack, order._productID, rack._expectedPickQuantity);
                transporter.PrepareToPick(pickup_point, rack, order._productID, rack._expectedPickQuantity);
                Program.Print("Handle pick " + order._productID + " " + order._quantity);
            }

            return true;
        }

        public static Rack FindRackToSlot(String product_id, int quantity)
        {
            Product product_info = new Product(_DicItems[product_id]);
            MaxStorage max_storage_info = new MaxStorage(_DicMaxStorage[product_info._productType]);

            Rack result = null;

            if (product_info._storageType.Equals("fold"))
            {
                // Find empty spot in the racks which contain product
                foreach (GeneralPurposeRack rack in _generalRackList)
                {
                    // Find rack with the same product type and the same shipper. 
                    if (rack._productType.Equals(product_info._productType) &&
                        (rack._shipperID == product_info._shipperID) &&
                        rack.IsEnoughSpaceToSlot(quantity))
                    {
                        result = rack;
                        break;
                    }
                }

                // If can't find empty spot from rack with product, get a new empty rack
                if (result ==  null && _generalEmptyRacksQueue.Count > 0)
                {
                    result = _generalEmptyRacksQueue.Dequeue();
                }
            }
            else
            {
                foreach (HangerRack rack in _hangerRackList)
                {
                    // Find rack with the same shipper. 
                    if (rack._shipperID == product_info._shipperID &&
                        rack.IsEnoughSpaceToSlot(quantity))
                    {
                        result = rack;
                    }
                }

                if (result == null && _hangerEmptyRackQueue.Count > 0)
                {
                    result = _hangerEmptyRackQueue.Dequeue();
                }
            }

            if (result != null)
            {
                result._expectedSlotQuantity += quantity;

                if(result.isEmpty())
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

            return result;
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
                        rack._expectedPickQuantity = (quantity > item._quantity) ? item._quantity : quantity;
                        result.Add(rack);
                        quantity -= item._quantity;
                        if (quantity <= 0) // Get enought quantity, return the list
                            return result;
                    }
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
            foreach (TransportRobot robot in _Transporters.Values)
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
                _SlotOrders.Add(new Order(input[i], int.Parse(input[i + 1])));
            }

            // Need to remove here
            if (_time == 0 && _day != 0) // resume the activity of previous day
            {
                foreach (Robot robot in _AllMovingRobots.Values)
                {
                    robot.ResumeActivityLastDay();
                }
            }

            if (_time < 714)
            {
                while (_SlotOrders.Count > 0)
                {
                    TransportRobot transporter = FindTransporterToPick(_Receiver._location);
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
            Product current_product = new Product(_DicItems[product_id]);
            if (current_product._storageType.Equals("fold"))
            {
                foreach (Order order in _SlotOrders)
                {
                    Product another_product = new Product(_DicItems[order._productID]);

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
                    Product another_product = new Product(_DicItems[order._productID]);
                    if (another_product._storageType.Equals(current_product._storageType) &&
                        another_product._shipperID == current_product._shipperID)
                        return order;
                }
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
                //Program.Print(robot._actionString + "\n");
            }

            foreach (Robot robot in _AllMovingRobots.Values)
            {
                Program.WriteOutput(robot._actionString + "\n");
                string debug = robot._actionString + " " + robot._state + " " + robot._location + "\n";
                Program.Print(debug);
            }
        }

        public static Byte ValueAt(Point location)
        {
            return Warehouse.Map[location.Y, location.X];
        }
    }
}
