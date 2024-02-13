using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Mutex mutex = new Mutex(false, "97740883-d1df-43e6-9bea-f4639907687c");
            try
            {
                // Run only one instance of the application.
                if (mutex.WaitOne(0, false))
                {
                    // Run the application
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());
                }
            }
            finally
            {
                if (mutex != null)
                    mutex.Close();
            }
        }
    }
}
