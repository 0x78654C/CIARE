using CIARE.Utils;
using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json.Bson;
using System;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    static class Program
    {
        static BackgroundWorker worker;
        static string s_arg = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            SingleInstanceApplication.Run(new MainForm(), NewInstanceHandler);

            //Mutex mutex = new Mutex(false, "97740883-d1df-43e6-9bea-f4639907687c");
            //try
            //{
            //    // Run only one instance of the application.
            //    if (mutex.WaitOne(0, false))
            //    {
            //        // Run the application
            //        Application.EnableVisualStyles();
            //        Application.SetCompatibleTextRenderingDefault(false);
            //        Application.Run(new MainForm());
            //    }
            //}
            //finally
            //{
            //    if (mutex != null)
            //        mutex.Close();
            //}
        }

        public static void NewInstanceHandler(object sender, StartupNextInstanceEventArgs e)
        {
            s_arg = $"cli|{e.CommandLine[1]}";
            e.BringToForeground = true;
            GlobalVariables.processArg = s_arg;
            FileManage.OpenFileFromArgs(s_arg);
        //    worker = new BackgroundWorker();
        //    worker.DoWork += Worker;
        //    worker.RunWorkerAsync();
        }

        private static void Worker(object sender, DoWorkEventArgs e) =>
            FileManage.OpenFileFromArgs(s_arg);


        public class SingleInstanceApplication : WindowsFormsApplicationBase
        {
            private SingleInstanceApplication()
            {
                base.IsSingleInstance = true;
            }

            public static void Run(Form form, StartupNextInstanceEventHandler startupNextInstanceEventHandler)
            {
                SingleInstanceApplication singleInstance = new SingleInstanceApplication();
                singleInstance.MainForm = form;
                singleInstance.StartupNextInstance += startupNextInstanceEventHandler;
                singleInstance.Run(Environment.GetCommandLineArgs());
            }
        }
    }
}
