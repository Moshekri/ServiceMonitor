using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ServicesMonitorController
{
    public class ServicesChecker
    {
        ServiceController serviceController;
        public ServicesChecker()
        {
            serviceController = new ServiceController();
        }
        public ServiceControllerStatus CheckServiceStatus(string serviceName)
        {
            serviceController.ServiceName = serviceName;
            try
            {
                serviceController.Refresh();
                var serviceStatus = serviceController.Status;
                return serviceStatus;
            }
            catch (Exception)
            {

                return ServiceControllerStatus.Stopped;

            }

        }
        public bool StartService(string serviceName)
        {

            serviceController.ServiceName = serviceName;
            serviceController.Refresh();
            if (serviceController.Status == ServiceControllerStatus.StopPending)
            {
                serviceController.Stop();
            }
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 30));
            serviceController.Refresh();
            if (serviceController.Status == ServiceControllerStatus.Stopped && serviceController.StartType != ServiceStartMode.Disabled)
            {
                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));
                serviceController.Refresh();
            }
          
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                return true;
            }
            else
            {
                return false;
            }

        }


    }
}
