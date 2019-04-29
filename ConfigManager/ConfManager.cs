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
            config.UseSSL = bool.Parse(ConfigurationManager.AppSettings["usessl"]);
            config.SmtpUsername = ConfigurationManager.AppSettings["SmtpUsername"];
            config.SmtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            config.ServicesToMonitor = ConfigurationManager.AppSettings["ServiceList"].Split(',');
            config.KeywordsForLogMonitor = ConfigurationManager.AppSettings["Keywords"].Split(',');
            config.SourcesForLogMonitor = ConfigurationManager.AppSettings["Sources"].Split(',');
            config.CheckInterval = ParseNumber(ConfigurationManager.AppSettings["Timeout"], 10) * 1000;
            config.SmtpAddress = ConfigurationManager.AppSettings["SMTP"];
            config.SmtpPort = ParseNumber(ConfigurationManager.AppSettings["SMTPport"], 25);
            config.FromMailAddress = ConfigurationManager.AppSettings["FromMail"];
            config.ToMailAddress = ConfigurationManager.AppSettings["ToMail"].Split(',');
            config.ResetDataInterval = ParseNumber(ConfigurationManager.AppSettings["ResetDataInterval"], 5) * 60000;
            config.SslPort = ParseNumber(ConfigurationManager.AppSettings["SslPort"], 443);


            return config;


        }


        private int ParseNumber(string data, int defaltValue)
        {
            int value;
            bool success = int.TryParse(data, out value);
            if (success)
            {
                return value;
            }
            else
            {
                logger.Debug($"could not parse {data} to a string ! using defalt value of {defaltValue}");
            }
                return defaltValue;
        }
    }
}
