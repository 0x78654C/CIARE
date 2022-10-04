using CIARE.Utils.FilesOpenOS;
using System;
using System.Windows.Forms;

namespace CIARE
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AutoStartFile autoStartFile = new AutoStartFile("", "", "");
            if (autoStartFile.CheckFlag() == "0")
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }
    }
}
