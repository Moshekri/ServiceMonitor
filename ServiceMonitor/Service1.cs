using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Net.Mail;
using System.IO.Compression;
using System.IO;

namespace ServiceMonitor
{
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };

    public partial class Service1 : ServiceBase
    {
        System.Timers.Timer timer;
        int timeout = 180;
        string[] services;
        int[] timeouts;
        Logger logger;
        string smtpHost;
        int smtpPort;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
        public void DebugStart()
        {
            OnStart(null);
        }
        public Service1()
        {
            smtpHost = ConfigurationManager.AppSettings["SMTP"];
            try
            {
                smtpPort = int.Parse(ConfigurationManager.AppSettings["SMTPPORT"]);
            }
            catch (Exception)
            {
                smtpPort = 25;
            }
           
            logger = LogManager.GetCurrentClassLogger();
            logger.Debug("Creating service object.");
            logger.Debug("Reading servic list from configuration ...");
            string serviceList = ConfigurationManager.AppSettings["ServiceList"];
            logger.Debug($"Service list : {serviceList}");
            services = serviceList.Split(',');
            timeouts = new int[services.Length];
            for (int i = 0; i < timeouts.Length; i++)
            {
                timeouts[i] = 0;
            }
            if (int.TryParse(ConfigurationManager.AppSettings["Timeout"], out timeout))
            {

            }
            logger.Info($"Timeout set to {timeout} seconds");
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed;
            InitializeComponent();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // run on all the services from the list
            for (int i = 0; i <= timeouts.Length; i++)
            {
                logger.Trace($"Checking if service {services[i]} is running");
                if (timeouts[i] > timeout)
                {
                    logger.Info($"The service {services[i]} has been shutdown for more then {timeout} seconds , Restarting the server");
                    RestartServer();
                }
                else
                {
                    ServiceController serviceController = new ServiceController(services[i]);
                    serviceController.Refresh();
                    if (serviceController.Status == ServiceControllerStatus.Stopped)
                    {
                        timeouts[i]++;
                        if (timeouts[i] % 10 ==0)
                        {
                            logger.Info($"The service {services[i]} has been shutdown for  {timeouts[i]} seconds ...");// attempting to restart the service ");
                        }
                        // serviceController.Start();
                        // serviceController.Refresh();
                        // logger.Info($"The service {services[i]} is now at the {serviceController.Status} state ");
                        // serviceController.Refresh();
                        //if (serviceController.Status == ServiceControllerStatus.Running)
                        // {
                        //     timeouts[i] = 0;
                        // }
                    }
                }

            }
        }

        private void RestartServer()
        {
            timer.Enabled = false;
            timer.Stop();

            SmtpClient mailClient = new SmtpClient(smtpHost, smtpPort);
            
            string from = ConfigurationManager.AppSettings["FromMail"];
            string[] to = ConfigurationManager.AppSettings["ToMail"].Split(',');
            string zippedLogPath = ConfigurationManager.AppSettings["LogPath"];
            if (File.Exists(zippedLogPath + "\\temp\\logs.zip"))
            {
                File.Delete(zippedLogPath + "\\temp\\logs.zip");
            }
            try
            {
               
                LogManager.DisableLogging();
                Thread.Sleep(500);
                ZipFile.CreateFromDirectory(zippedLogPath, zippedLogPath + "\\temp\\logs.zip");
                LogManager.EnableLogging();
            }
            catch(Exception ex)
            {
                string d = "ff";
            }
            foreach (var destination in to)
            {
                MailMessage message = new MailMessage(from, destination, "Muse server has restarted due to service error", "");
                message.Attachments.Add(new Attachment(zippedLogPath + "\\logs.zip"));
                try
                {
                    mailClient.Send(message);
                }
                catch (Exception ex)
                {

                    logger.Debug(ex.Message);
                }
                
            }


           
            for (int i = 10; i > 0 ; i--)
            {
                logger.Info($"Server restart in {i} Seconds...");
                Thread.Sleep(1000);
            }
            logger.Info("Server restarting now !! ");
            System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0 /f");
        }

        protected override void OnStart(string[] args)
        {
           
            // Update the service state to Start Pending.
            logger.Info("Starting Service..");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            logger.Debug("Starting Timer ...");
            timer.Start();
            logger.Debug("Timer started ...");
            logger.Debug("Enabeling timer events...");
            timer.Enabled = true;

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            logger.Info("Service entered the running state.");
        }
        protected override void OnStop()
        {
            logger.Info("Stopping service ...");
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            logger.Debug("Disabeling timer events .");
            timer.Enabled = false;
            logger.Debug("Stopping timer.");
            timer.Stop();

            this.Stop();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            logger.Info("Service is Stopped.");

        }
    }
}
