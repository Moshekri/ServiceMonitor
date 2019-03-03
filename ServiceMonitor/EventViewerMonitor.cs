using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace ServiceMonitor
{
    public class EventViewerMonitor
    {

        public EventLogEntryCollection GetEntries(string logName)
        {
            try
            {
                EventLog eventLog = new EventLog(logName);
                var entries = eventLog.Entries;
                return entries;
            }
            catch (Exception)
            {
                throw;
            }

        }


    }
}
