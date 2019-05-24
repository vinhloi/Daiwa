using System;
using System.Collections.Generic;
using System.Text;

namespace Daiwa
{
    public class Warehouse
    {
        int _numRows;
        int _numCols;
        int[,] _data;
      
        public int NumRows { get => _numRows; set => _numRows = value; }
        public int NumCols { get => _numCols; set => _numCols = value; }
        public int[,] Data { get => _data; set => _data = value; }

        public Warehouse(string filename)
        {
            // Get the file's text.
            string whole_file = System.IO.File.ReadAllText(filename);

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
                }
            }
        }
    }
}
