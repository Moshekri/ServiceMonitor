using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Common;
using ConfigManager;
using MessageSender;
using NLog;

namespace ServicesMonitorController
{
    public class ServiceMonitorController
    {
        ServiceMonitorConfiguration configuration;
        ServiceMonitorConfiguration originalConfig;
        Timer timer;
        Timer MasterTimer;
        ServicesChecker checker;
        Logger logger;

        public ServiceMonitorController(ServiceMonitorConfiguration config)
        {
            checker = new ServicesChecker();
            configuration = config;
            originalConfig = CreateCopy(config);
            timer = new Timer(config.CheckInterval);

            MasterTimer = new Timer(5 * 60 * 1000);
            MasterTimer.Elapsed += MasterTimer_Elapsed;
            timer.Elapsed += Timer_Elapsed;
            logger = NLog.LogManager.GetCurrentClassLogger();
        }

        private ServiceMonitorConfiguration CreateCopy(ServiceMonitorConfiguration config)
        {
            ServiceMonitorConfiguration newConfig = new ServiceMonitorConfiguration();
            newConfig.CheckInterval = config.CheckInterval;
            newConfig.FromMailAddress = config.FromMailAddress;
            Array.Copy(config.KeywordsForLogMonitor, newConfig.KeywordsForLogMonitor, config.KeywordsForLogMonitor.Length);
            Array.Copy(config.ServicesToMonitor, newConfig.ServicesToMonitor, config.ServicesToMonitor.Length);
            newConfig.SmtpAddress = config.SmtpAddress;
            newConfig.SmtpPort = config.SmtpPort;
            Array.Copy(config.SourcesForLogMonitor, newConfig.SourcesForLogMonitor, config.SourcesForLogMonitor.Length);
            Array.Copy(config.ToMailAddress, newConfig.ToMailAddress, config.ToMailAddress.Length);
            return newConfig;
        }

        /// <summary>
        /// This will reset all changes done when services were stopped effectevilly creatint a reapeating 
        /// e-mail message every x minutes (
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MasterTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ConfManager cnfman = new ConfManager();
            configuration = cnfman.GetConfiguration();

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var serviceList = ServiceController.GetServices();

            foreach (var service in configuration.ServicesToMonitor)
            {
                var s = serviceList.FirstOrDefault(ser => ser.ServiceName == service);
                if (s != null)
                {
                    var status = checker.CheckServiceStatus(service);
                    switch (status)
                    {

                        case ServiceControllerStatus.Stopped:
                        case ServiceControllerStatus.StopPending:

                            logger.Info($"Service {service} has stopped");
                            var IsserviceStarted = checker.StartService(service);
                            if (!IsserviceStarted)
                            {
                                SendMail(service);
                                RemoveServiceFromList(service);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void RemoveServiceFromList(string service)
        {
            for (int i = 0; i < configuration.ServicesToMonitor.Length; i++)
            {
                if (configuration.ServicesToMonitor[i] == service)
                {
                    configuration.ServicesToMonitor[i] = string.Empty;
                }
            }
        }

        private void SendMail(string service)
        {
            string machineName = System.Net.Dns.GetHostName();
            string msg = $"The Service {service} on this machine ({machineName}) has stopped" +
                $" and an attepmt to restart it has failed." +
                $"" +
                $"Action is needed !!!";
            string subject = $"Monitored Service \"{service}\" has stopped ";
            MailMessageSender.SendMessage(msg, subject, configuration);
        }

        public void Start()
        {
            timer.Enabled = true;
            MasterTimer.Enabled = true;
        }
        public void Stop()
        {
            MasterTimer.Enabled = true;
            timer.Enabled = false;
        }



    }
}
