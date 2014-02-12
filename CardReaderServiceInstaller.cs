using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace CardReaderService
{
    [RunInstaller(true)]
    public class CardReaderServiceInstaller : Installer
    {
        /// <summary>
        /// Public Constructor for CardReaderServiceInstaller.
        /// - Put all of your Initialization code here.
        /// </summary>
        public CardReaderServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            //# Service Information
            serviceInstaller.DisplayName = "CardReader Service";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // This must be identical to the WindowsService.ServiceBase name
            // set in the constructor of WindowsService.cs
            serviceInstaller.ServiceName = "CardReader Service";
            serviceInstaller.Description = "Provides ability to start/stop CardReader Service. If this is disabled, services related to CardReader would Fail";

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }
    }
}
