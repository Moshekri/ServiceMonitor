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
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);



        System.Timers.Timer timer;
        int timeout = 180;
        string[] services;
        int[] timeouts;
        bool[] sentMail;
        Logger logger;
        string smtpHost;
        int smtpPort;



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
            sentMail = new bool[services.Length];

            for (int i = 0; i < timeouts.Length; i++)
            {
                timeouts[i] = 0;
            }
            if (int.TryParse(ConfigurationManager.AppSettings["Timeout"], out timeout))
            {

            }
            logger.Info($"Timeout set to {timeout} seconds");
            timer = new System.Timers.Timer(10000);
            timer.Elapsed += Timer_Elapsed;
            InitializeComponent();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckEventLogForErrors();
            CheckServicesStatus();
        }

        private void CheckServicesStatus()
        {
            // run on all the services from the list
            for (int i = 0; i <= timeouts.Length; i++)
            {
                logger.Trace($"Checking if service {services[i]} is running");
                if (timeouts[i] > timeout)
                {
                    logger.Info($"The service {services[i]} has been shutdown for more then {timeout} seconds , Restarting the server");
                    // RestartServer();
                }
                else
                {
                    ServiceController serviceController = new ServiceController(services[i]);
                    serviceController.Refresh();
                    if (serviceController.Status == ServiceControllerStatus.Stopped)
                    {
                        if (sentMail[i] == false)
                        {
                            try
                            {
                                SendMail($"Service {services[i]} has stopped");
                                sentMail[i] = true;
                            }
                            catch (Exception ex)
                            {
                                logger.Debug(ex.Message);
                            }
                            timeouts[i]++;
                            if (timeouts[i] % 10 == 0)
                            {
                                logger.Info($"The service {services[i]} has been shutdown for  {timeouts[i]} seconds ...");
                            }
                            // try to restart the service
                            if (serviceController.Status != ServiceControllerStatus.StartPending && serviceController.Status != ServiceControllerStatus.Running)
                            {
                                logger.Debug($"Trying to start service \"{serviceController.DisplayName}\"");
                                serviceController.Start();
                                serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 60));
                            }
                            if (serviceController.Status == ServiceControllerStatus.Running)
                            {
                                logger.Debug($"Service \"{serviceController.DisplayName}\"  is now in the running state");
                                try
                                {
                                    SendMail($"Service \"{serviceController.DisplayName}\"  is now in the running state");
                                }
                                catch (Exception)
                                {

                                    throw;
                                }
                                sentMail[i] = false;
                                timeouts[i] = 0;
                            }
                            else
                            {
                                logger.Debug($"Error trying to start service {serviceController.DisplayName}");
                            }
                        }
                    }

                }
            }
        }
        private void CheckEventLogForErrors()
        {
            string[] sources = ConfigurationManager.AppSettings["Sources"].Split(',');
            string[] keywords = ConfigurationManager.AppSettings["Keywords"].Split(',');
            string[] monitoredServices = ConfigurationManager.AppSettings["ServiceList"].Split(',');
            bool restartServices = false;
            EventLog eventLog = new EventLog("System");
            EventLogEntryCollection entries = eventLog.Entries;

            //search eventlog for
            foreach (EventLogEntry entry in entries)
            {
                foreach (string keyword in keywords)
                {
                    var dif = DateTime.Now - entry.TimeWritten;
                    if (entry.Message.Contains(keyword) && (dif.Days == 0 && dif.Hours == 0 && dif.Minutes == 0 && dif.Seconds < 10))
                    {
                        logger.Debug($"{DateTime.Now} , found  keyword\" {keyword}\" in eventlog , it happaned {DateTime.Now.Second - entry.TimeWritten.Second} ago");
                        logger.Debug($"{DateTime.Now} , restarting services ... ");
                        restartServices = true;
                        break;
                    }
                }
            }

            if (restartServices)
            {
                logger.Debug("Found an entery in the error log that is related to muse ");
                StringBuilder sb = new StringBuilder();
                foreach (string service in monitoredServices)
                {
                    ServiceController sc = new ServiceController(service);
                    sc.Refresh();
                    if (sc.StartType != ServiceStartMode.Disabled)
                    {
                        logger.Debug($"Restarting service \"{sc.DisplayName}\"");
                        sb.AppendLine($"restarting service \"{sc.DisplayName}\"");
                        logger.Debug($"{DateTime.Now} ,restarting service \"{sc.DisplayName}\"");
                        sc.Start();
                        Thread.Sleep(10000);
                    }
                }
                SendMail(sb.ToString());
            }
        }

        private void SendMail(string zippedLogPath, string msg = "Muse server has restarted due to service error")
        {
            string[] to = ConfigurationManager.AppSettings["ToMail"].Split(',');
            SmtpClient mailClient = new SmtpClient(smtpHost, smtpPort);
            string from = ConfigurationManager.AppSettings["FromMail"];
            foreach (var destination in to)
            {
                logger.Debug("Sending Email");
                MailMessage message = new MailMessage(from, destination, msg, "");
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
        }
        private void SendMail(string msg)
        {
            string[] to = ConfigurationManager.AppSettings["ToMail"].Split(',');
            SmtpClient mailClient = new SmtpClient(smtpHost, smtpPort);
            string from = ConfigurationManager.AppSettings["FromMail"];
            foreach (var destination in to)
            {
                logger.Debug("Sending Email");
                MailMessage message = new MailMessage(from, destination, msg, "");

                try
                {
                    mailClient.Send(message);
                }
                catch (Exception ex)
                {

                    logger.Debug(ex.Message);
                }

            }
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


        //private void RestartServer()
        //{
        //    timer.Enabled = false;
        //    timer.Stop();

        //    string zippedLogPath = ConfigurationManager.AppSettings["LogPath"];
        //    if (File.Exists(zippedLogPath + "\\temp\\logs.zip"))
        //    {
        //        File.Delete(zippedLogPath + "\\temp\\logs.zip");
        //    }
        //    try
        //    {

        //        LogManager.DisableLogging();
        //        Thread.Sleep(500);
        //        ZipFile.CreateFromDirectory(zippedLogPath, zippedLogPath + "\\temp\\logs.zip");
        //        LogManager.EnableLogging();
        //    }
        //    catch(Exception ex)
        //    {
        //        string d = "ff";
        //    }
        //    SendMail(zippedLogPath);



        //    for (int i = 10; i > 0 ; i--)
        //    {
        //        logger.Info($"Server restart in {i} Seconds...");
        //        Thread.Sleep(1000);
        //    }
        //    logger.Info("Server restarting now !! ");
        //    Restrt r = new Restrt();
        //    r.RestartComputer();
        //   // System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0 /f");
        //}
    }
}
