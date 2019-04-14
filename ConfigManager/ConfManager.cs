using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Common;

namespace ConfigManager
{
    public class ConfManager
    {
        Logger logger;
        public ConfManager()
        {
            logger = LogManager.GetCurrentClassLogger();
        }
        public ServiceMonitorConfiguration GetConfiguration()
        {
            ServiceMonitorConfiguration config = new ServiceMonitorConfiguration();

            config.ServicesToMonitor = ConfigurationManager.AppSettings["ServiceList"].Split(',');
            config.KeywordsForLogMonitor= ConfigurationManager.AppSettings["Keywords"].Split(',');
            config.SourcesForLogMonitor = ConfigurationManager.AppSettings["Sources"].Split(',');
            int interval;
            bool success = int.TryParse(ConfigurationManager.AppSettings["Timeout"], out interval);
            if (!success)
            {
                config.CheckInterval = 10 *1000;
                logger.Error("Could not parse Timeout parameter , setting to default (10 seconds)");
            }
            else
            {
                
                config.CheckInterval = interval*1000;
            }
            config.SmtpAddress = ConfigurationManager.AppSettings["SMTP"];
            int port;
            success = int.TryParse(ConfigurationManager.AppSettings["SMTP"],out port);
            if (success)
            {
                config.SmtpPort = port;
            }
            else
            {
                config.SmtpPort = 25; // default smtp port
                logger.Error("Cannot convert SMTPPORT to int ,  setting to default (25)");
            }
            config.FromMailAddress = ConfigurationManager.AppSettings["FromMail"];
            config.ToMailAddress = ConfigurationManager.AppSettings["ToMail"].Split(',');


            return config;


        }
    }
}
