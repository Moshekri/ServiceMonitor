using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ServiceMonitorConfiguration
    {
        public ServiceMonitorConfiguration()
        {
            ServicesToMonitor= KeywordsForLogMonitor=SourcesForLogMonitor =ToMailAddress = new string[20];
        }
        public int CheckInterval { get; set; }
        public string[] ServicesToMonitor { get; set; }
        public string[] KeywordsForLogMonitor { get; set; }
        public string[] SourcesForLogMonitor { get; set; }
        public string SmtpAddress { get; set; }
        public int SmtpPort { get; set; }
        public string FromMailAddress { get; set; }
        public string[] ToMailAddress { get; set; }

        
    }
}
