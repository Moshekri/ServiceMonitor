using NLog;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using ConfigManager;
using Common;
using ServicesMonitorController;

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

    public partial class ServicesMonitor : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        Logger logger;
        ConfManager confManager;
        ServiceMonitorConfiguration config;
        ServiceMonitorController serviceMonitorController;

        public void DebugStart()
        {
            OnStart(null);
        }
        public ServicesMonitor()
        {
            logger = LogManager.GetCurrentClassLogger();
            confManager = new ConfManager();
            config = confManager.GetConfiguration();
            serviceMonitorController = new ServiceMonitorController(config);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {

            // Update the service state to Start Pending.
            logger.Info("Starting Service..");
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            serviceMonitorController.Start();



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

            serviceMonitorController.Stop();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            logger.Info("Service is Stopped.");

        }



    }
}
