using CIARE.Utils;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    static class Program
    {
        static string s_arg = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Length > 1 && arguments[1] == "--apply-update")
            {
                AutoUpdater.UpdaterApplication.Run(arguments.Skip(2).ToArray());
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SingleInstanceApplication.Run(NewInstanceHandler);
        }

        public static void NewInstanceHandler(object sender, StartupNextInstanceEventArgs e)
        {
            e.BringToForeground = true;
            if (e.CommandLine.Count < 2 || MainForm.Instance == null)
                return;

            s_arg = $"cli|{e.CommandLine[1]}";
            GlobalVariables.processArg = s_arg;
            FileManage.OpenFileFromArgs(s_arg, MainForm.Instance.EditorTabControl);
        }

        public class SingleInstanceApplication : WindowsFormsApplicationBase
        {
            private SingleInstanceApplication()
            {
                base.IsSingleInstance = true;
            }

            protected override void OnCreateMainForm()
            {
                MainForm = new CIARE.MainForm();
                ((CIARE.MainForm)MainForm).InitializeUpdates();
            }

            public static void Run(StartupNextInstanceEventHandler startupNextInstanceEventHandler)
            {
                SingleInstanceApplication singleInstance = new SingleInstanceApplication();
                singleInstance.StartupNextInstance += startupNextInstanceEventHandler;
                singleInstance.Run(Environment.GetCommandLineArgs());
            }
        }
    }
}
