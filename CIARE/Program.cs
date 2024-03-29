﻿using CIARE.Utils;
using Microsoft.VisualBasic.ApplicationServices;
using System;
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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SingleInstanceApplication.Run(new MainForm(), NewInstanceHandler);
        }

        public static void NewInstanceHandler(object sender, StartupNextInstanceEventArgs e)
        {
            s_arg = $"cli|{e.CommandLine[1]}";
            e.BringToForeground = true;
            GlobalVariables.processArg = s_arg;
            FileManage.OpenFileFromArgs(s_arg, MainForm.Instance.EditorTabControl);
        }

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
