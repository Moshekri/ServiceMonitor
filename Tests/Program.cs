using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceController c = new ServiceController("MUSE Email");
            c.Refresh();
            Console.WriteLine($"status of service {c.ServiceName} is {c.Status.ToString()}");
            if (c.Status == ServiceControllerStatus.Running)
            {
                c.Stop();
                do
                {
                    c.Refresh();
                    Console.WriteLine($"status of service {c.ServiceName} is {c.Status.ToString()}");
                } while (c.Status != ServiceControllerStatus.Stopped);

            }
            else if (c.Status == ServiceControllerStatus.Stopped)
            {
                System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0 /f");
                //c.Start();
                //do
                //{
                //    c.Refresh();
                //    Console.WriteLine($"status of service {c.ServiceName} is {c.Status.ToString()}");
                //} while (c.Status != ServiceControllerStatus.Running);

            }
            c.Refresh();
            Console.WriteLine(c.Status);
            Console.ReadLine();



        }
    }
}
