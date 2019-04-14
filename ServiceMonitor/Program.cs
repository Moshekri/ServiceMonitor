using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace ServiceMonitor
{
    static class Program
    {
        static Logger logger;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            logger = LogManager.GetCurrentClassLogger();
#if DEBUG
            logger.Info("Running Service In Debug Mode...");
            ServicesMonitor sv1 = new ServicesMonitor();
            sv1.DebugStart();
            Thread.Sleep(Timeout.Infinite);


#else
            logger.Info("Running Service In Release Mode...");
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
