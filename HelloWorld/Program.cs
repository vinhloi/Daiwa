using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Daiwa
{
    class Program
    {
        static Process simproc;
        static Warehouse warehouse;

        static void Main(string[] args)
        {
            using (StreamWriter writetext = new StreamWriter("error.txt"))
            {
                writetext.WriteLine("漢字");
            }

            warehouse = new Warehouse();
            warehouse.LoadItemsFile("data\\items.csv");
            warehouse.LoadItemCategoriesFile("data\\item_categories.csv");
            warehouse.LoadMap("data\\map.csv");
            StartSimulator();
            //simproc.WaitForExit();
            while (true)
            {
                string input = simproc.StandardOutput.ReadLine();
                HandleInput(input);
            }

        }

        static void StartSimulator()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false; //required to redirect standart input/output

            // redirects on your choice
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardErrorEncoding = Encoding.UTF8;
            startInfo.FileName = "docker.exe";
            startInfo.Arguments = "run -a stdout -a stdin -a stderr -i simulator";

            simproc = new Process();
            simproc.StartInfo = startInfo;
            simproc.ErrorDataReceived += delegate (object sender, System.Diagnostics.DataReceivedEventArgs errorLine)
            {
                if (errorLine.Data != null)
                {
                    using (StreamWriter writetext = new StreamWriter("error.txt"))
                    {
                        writetext.WriteLine(errorLine.Data);
                    }
                }
            };

            //simproc.OutputDataReceived += new DataReceivedEventHandler(SimulatorOutputDataHandler);

            simproc.Start();
            simproc.BeginErrorReadLine();
            //simproc.BeginOutputReadLine();
        }

        private static void SimulatorOutputDataHandler(object sendingProcess,
           DataReceivedEventArgs outLine)
        {
            HandleInput(outLine.Data);
        }

        private static void HandleInput(string input)
        {
            if (!String.IsNullOrEmpty(input))
            {
                List<string> values = input.Split(' ').ToList<string>();
                switch (values[0])
                {
                    case "init":
                        warehouse.SpecifyProductInitialPosition(values);

                        List<string> robotPositions = warehouse.SpecifyRobotInitialPosition();
                        foreach (string pos in robotPositions)
                            WriteOutput(pos);
                        break;
                    case "store":
                        Debug.WriteLine(input);
                        break;
                    default:
                        Debug.WriteLine(input);
                        break;
                }
            }
        }

        public static void WriteOutput(string output)
        {
            Print(output);
            simproc.StandardInput.Write(output);
        }

        public static void Print(string text)
        {
            Console.WriteLine(text);
        }
    }
}
