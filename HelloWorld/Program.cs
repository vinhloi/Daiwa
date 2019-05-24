using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Daiwa
{
    class Program
    {
        static void Main(string[] args)
        {
            Warehouse warehouse = new Warehouse("map.csv");

            //ProcessStartInfo startInfo = new ProcessStartInfo();
            //startInfo.UseShellExecute = false; //required to redirect standart input/output

            //// redirects on your choice
            //startInfo.RedirectStandardInput = true;
            //startInfo.RedirectStandardOutput = true;
            //startInfo.RedirectStandardError = true;

            //startInfo.FileName = "docker.exe";
            //startInfo.Arguments = "run -a stdout -a stdin -a stderr -i simulator";

            //Process simproc = new Process();
            //simproc.StartInfo = startInfo;
            //simproc.Start();

            //    string input = simproc.StandardOutput.ReadLine();
            //    if(String.IsNullOrEmpty(input))
            //    {
            //        //break;
            //    }

            //    List<string> data = input.Split(' ').ToList<string>();

            //    switch (data[0])
            //    {
            //        case "init":
            //            Console.WriteLine(data[1]);
            //            break;
            //        case "store":
            //            break;
            //        default:
            //            break;
            //    }
            //    //simproc.StandardInput.WriteLine("temp");

        }
    }
}
