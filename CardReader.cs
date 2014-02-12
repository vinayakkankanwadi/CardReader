/// <FILE>
/// Card Reader: This program would read USB Card Reader using LibUSBDotNet
/// and uses .Net Windows service
/// Vinayak Kankanwadi
/// </FILE>
/// 

using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.Text;
using System.Collections.ObjectModel;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace CardReaderService
{
    class CardReader : ServiceBase
    {
        /// <summary>
        /// Public Constructor for WindowsService.
        /// - Put all of your Initialization code here.
        /// </summary>
        /// 

        public static string logFile = "c:\\temp\\log.txt";
        public static string trackFile = "c:\\temp\\trackData.txt";

        public CardReader()
        {
            this.ServiceName = "CardReader Service";
            this.EventLog.Source = "CardReader Service";
            this.EventLog.Log = "Application";
            
            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;

            if (!EventLog.SourceExists("CardReader Service"))
                EventLog.CreateEventSource("CardReader Service", "Application");
        }
        
        public static void GetIDs()
        {
            // Write the string to a file.
            System.IO.StreamWriter fileReader = new System.IO.StreamWriter(logFile, true);

            DateTime theDate = DateTime.Now;
            string lines = theDate.ToString("yyyy/MM/dd hh:mm:ss");

            fileReader.WriteLine(lines);

             
            UsbRegDeviceList allDevices = UsbDevice.AllDevices;
            foreach (UsbRegistry usbRegistry in allDevices)
            {
                if (usbRegistry.Open(out AllUsbDevice))
                {
                    if (AllUsbDevice.Info.ProductString.ToString().CompareTo("USB Swipe Reader") == 0 &&
                        AllUsbDevice.Info.ManufacturerString.ToString().CompareTo("Mag-Tek") == 0)
                    {
                        fileReader.WriteLine(AllUsbDevice.Info.ManufacturerString.ToString());
                        fileReader.WriteLine(AllUsbDevice.Info.ProductString.ToString());

                        MyUsbFinder.Pid = AllUsbDevice.Info.Descriptor.ProductID;
                        MyUsbFinder.Vid = AllUsbDevice.Info.Descriptor.VendorID;
                        if (MyUsbFinder.Vid == 0x0801)
                        {
                            fileReader.WriteLine("Product ID: {0:x}", MyUsbFinder.Pid);
                            fileReader.WriteLine("Vendor  ID: {0:x}", MyUsbFinder.Vid);
                            break;
                        }
						else
						{
							MyUsbFinder.Pid = 0;
							MyUsbFinder.Vid = 0;
							fileReader.WriteLine("Error :D");
						}
                    }
                }
        }
            // Free usb resources.
            // This is necessary for libusb-1.0 and Linux compatibility.
            fileReader.Close();
            UsbDevice.Exit();
        }

        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        static void Main()
        {
            ServiceBase.Run(new CardReader());
        }

        /// <summary> 
        /// The method performs the main function of the service. It runs on  
        /// a thread pool worker thread. 
        /// </summary> 
        /// <param name="state"></param> 
        private void ServiceWorkerThread(object state)
        {
            GetIDs();
            ErrorCode ec = ErrorCode.None;
            try
            {
                // Find and open the usb device.
                MyUsbDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);
                // If the device is open and ready
                if (MyUsbDevice == null) throw new Exception("Device Not Found.");

                IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                if (!ReferenceEquals(wholeUsbDevice, null))
                {
                    wholeUsbDevice.SetConfiguration(1);
                    wholeUsbDevice.ClaimInterface(0);
                }
                // open read endpoint 1.
                UsbEndpointReader reader = MyUsbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

                byte[] readBuffer = new byte[1024];
                int bytesRead = 0;

                // Periodically check if the service is stopping. 
                while (!this.stopping)
                {
                    //Thread.Sleep(2000);  // Simulate some lengthy operations. 
                    ec = reader.Read(readBuffer, 30000, out bytesRead);
                    if (bytesRead > 0)
                    {
                        string trackString = Encoding.Default.GetString(readBuffer, 0, bytesRead);
                        string[] words = trackString.Split('?');
                        System.IO.StreamWriter fileReader2 = new System.IO.StreamWriter(trackFile, true);

                        if (words.Length > 0)
                        {
                            //Console.WriteLine();
                            string[] track1 = words[0].Split('%');
                            if (track1.Length > 1 && track1[1].Length > 0)
                            {
                                fileReader2.WriteLine(track1[1]);
                                //Console.WriteLine(track1[1]);
                            }
                        }
                        if (words.Length > 1)
                        {
                            string[] track2 = words[1].Split(';');
                            if (track2.Length > 1 && track2[1].Length > 0)
                            {
                                fileReader2.WriteLine(track2[1]);
                                //Console.WriteLine(track2[1]);
                            }
                        }
                        if (words.Length > 2)
                        {
                            string[] track3 = words[2].Split(';');
                            if (track3.Length > 1 && track3[1].Length > 0)
                            {
                                fileReader2.WriteLine(track3[1]);
                                //Console.WriteLine(track3[1]);
                            }
                        }
                        fileReader2.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter(logFile, true);
                file.WriteLine();
                file.WriteLine((ec != ErrorCode.None ? ec + ":" : String.Empty) + ex.Message);
                file.Close();
            }
            finally
            {
                if (MyUsbDevice != null)
                {
                    if (MyUsbDevice.IsOpen)
                    {
                        IUsbDevice wholeUsbDevice = MyUsbDevice as IUsbDevice;
                        if (!ReferenceEquals(wholeUsbDevice, null))
                        {
                            // Release interface #0.
                            wholeUsbDevice.ReleaseInterface(0);
                        }
                        MyUsbDevice.Close();
                    }
                    MyUsbDevice = null;
                    UsbDevice.Exit();
                }
            }           
            // Signal the stopped event. 
            this.stoppedEvent.Set();
        } 
        /// <summary>
        /// Dispose of objects that need it here.
        /// </summary>
        /// <param name="disposing">Whether or not disposing is going on.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// OnStart: Put startup code here
        ///  - Start threads, get inital data, etc.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
//#if DEBUG
//            System.Diagnostics.Debugger.Launch();
//#endif
        
            //base.OnStart(args);
            // Log a service start message to the Application log. 
            //this.eventLog1.WriteEntry("CSWindowsService in OnStart.");

            // Queue the main service function for execution in a worker thread. 
            ThreadPool.QueueUserWorkItem(new WaitCallback(ServiceWorkerThread)); 
        }

        /// <summary>
        /// OnStop: Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
            
            //base.OnStop();
            // Log a service stop message to the Application log. 
            //this.eventLog1.WriteEntry("CSWindowsService in OnStop.");

            // Indicate that the service is stopping and wait for the finish  
            // of the main service function (ServiceWorkerThread). 
            this.stopping = true;
            this.stoppedEvent.WaitOne(); 
        }

        /// <summary>
        /// OnPause: Put your pause code here
        /// - Pause working threads, etc.
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
        }

        /// <summary>
        /// OnContinue: Put your continue code here
        /// - Un-pause working threads, etc.
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();
        }

        /// <summary>
        /// OnShutdown(): Called when the System is shutting down
        /// - Put code here when you need special handling
        ///   of code that deals with a system shutdown, such
        ///   as saving special data before shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();
        }

        /// <summary>
        /// OnCustomCommand(): If you need to send a command to your
        ///   service without the need for Remoting or Sockets, use
        ///   this method to do custom methods.
        /// </summary>
        /// <param name="command">Arbitrary Integer between 128 & 256</param>
        protected override void OnCustomCommand(int command)
        {
            //  A custom command can be sent to a service by using this method:
            //#  int command = 128; //Some Arbitrary number between 128 & 256
            //#  ServiceController sc = new ServiceController("NameOfService");
            //#  sc.ExecuteCommand(command);

            base.OnCustomCommand(command);
        }

        /// <summary>
        /// OnPowerEvent(): Useful for detecting power status changes,
        ///   such as going into Suspend mode or Low Battery for laptops.
        /// </summary>
        /// <param name="powerStatus">The Power Broadcase Status (BatteryLow, Suspend, etc.)</param>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        /// <summary>
        /// OnSessionChange(): To handle a change event from a Terminal Server session.
        ///   Useful if you need to determine when a user logs in remotely or logs off,
        ///   or when someone logs into the console.
        /// </summary>
        /// <param name="changeDescription"></param>
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }

        public static UsbDevice MyUsbDevice;
        public static UsbDevice AllUsbDevice;
        public static UsbDeviceFinder MyUsbFinder = new UsbDeviceFinder(0x0801, 0x0002);

        private bool stopping = false;
        private ManualResetEvent stoppedEvent = new ManualResetEvent(false);
    }
}
