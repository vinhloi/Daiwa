//#define DOCKER
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
        static bool running = true;
        static void Main(string[] args)
        {
#if (DOCKER)
            warehouse = new Warehouse();
            while (running)
            {
                string input = Console.In.ReadLine();
                HandleInput(input);
            }
#else
            //StreamWriter writetext = new StreamWriter("error.txt", false);
            //StreamWriter writetext1 = new StreamWriter("debug.txt", false);
            StartSimulator();
            var watch = Stopwatch.StartNew();
            warehouse = new Warehouse();
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            PrintLine("execution time: " + elapsedMs);
            while (true)
            {
                string input = simproc.StandardOutput.ReadLine();
                HandleInput(input);
            }
#endif
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
            startInfo.StandardOutputEncoding = Encoding.UTF8;

            startInfo.FileName = "docker.exe";
            startInfo.Arguments = "run -a stdout -a stdin -a stderr -i simulator";

            simproc = new Process();
            simproc.StartInfo = startInfo;
            simproc.ErrorDataReceived += delegate (object sender, System.Diagnostics.DataReceivedEventArgs errorLine)
            {
                if (errorLine.Data != null)
                {
                    using (StreamWriter writetext = new StreamWriter("error.txt", true))
                    {
                        writetext.WriteLine(DateTime.Now.ToString("h:mm:ss ") + errorLine.Data);
                    }
                    running = false;
                }
            };

            simproc.Start();
            simproc.BeginErrorReadLine();
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
                List<string> values = input.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                switch (values[0])
                {
                    case "init":
                        var watch = Stopwatch.StartNew();
                        warehouse.Store(values);
                        warehouse.SpecifyRobotInitialPosition();
                        watch.Stop();
                        var elapsedMs = watch.ElapsedMilliseconds;
                        PrintLine("init time: " + elapsedMs);
                        break;
                    case "pick":
                        Print(input + "\n");
                        warehouse.Pick(values);
                        break;
                    case "slot":
                        Print(input + "\n");
                        warehouse.Slot(values);
                        warehouse.GenerateAction();
                        break;
                    case "0":
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                        Print("\n" + input + "\n");
                        warehouse.UpdateTime(values);
                        break;
                    default:
                        Print(input + "\n");
#if (DOCKER)
                        //using (StreamWriter writetext = new StreamWriter("app/output.txt", false))
#else
                        using (StreamWriter writetext = new StreamWriter("output.txt", true))
                        {
                            writetext.WriteLine(input);
                        }
#endif
                        running = false;
                        break;
                }
            }
        }

        public static void WriteOutput(string output)
        {
#if (DOCKER)
            Console.Out.Write(output);
#else
            simproc.StandardInput.Write(output);
#endif
        }

        public static void Print(string text)
        {
#if (DOCKER)
            //using (StreamWriter writetext = new StreamWriter("app/debug.txt", true))
            //{
            //    writetext.Write(text);
            //}
#else
            using (StreamWriter writetext = new StreamWriter("debug.txt", true))
            {
                writetext.Write(text);
            }
            Console.Write(text);
#endif
        }

        public static void PrintLine(string text)
        {
#if (DOCKER)
            //using (StreamWriter writetext = new StreamWriter("app/debug.txt", true))
            //{
            //    writetext.WriteLine(text);
            //}
#else
            using (StreamWriter writetext = new StreamWriter("debug.txt", true))
            {
                writetext.WriteLine(text);
            }
            Console.WriteLine(text);
#endif
        }
    }
}
