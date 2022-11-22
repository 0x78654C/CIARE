﻿using System;
using System.IO;
using System.Windows.Forms;

namespace CIARE.Utils
{
    public class GlobalVariables
    {
        public static string paramData = string.Empty;
        private static string s_rootPath = Path.GetPathRoot(Environment.SystemDirectory);
        private static readonly string s_accountName = Environment.UserName;
        public static readonly string userProfileDirectory = $"{s_rootPath}Users\\{s_accountName}\\AppData\\Local\\CIARE\\";
        public static readonly string markFile = $"{userProfileDirectory}\\markedFiles.cDat";
        public static readonly string markFileTemp = $"{userProfileDirectory}\\markedFiles_tmp.cDat";
        public static string openedFilePath = string.Empty;
        public static string openedFileName = string.Empty;
        public static string commandLineArguments = string.Empty;
        public static string findData = string.Empty;
        public static string findWhat = string.Empty;
        public static string repalceWith = string.Empty;
        public static bool savedFile = false;
        public static bool splitWindowPosition = false;
        public static bool exeName = false;
        public static bool checkFormOpen = false;
        public static bool outPutDisplay = false;
        public static bool findTabOpen = false;
        public static bool noPath = false;
        public static bool darkColor = false;
        public static string configParam = "/p:configuration=Debug";
        public static string platformParam = "/p:Platform=\"Any CPU\"";
        public static bool OCodeCompletion = true;
        public static bool OLineNumber = true;
        public static bool OFoldingCode = true;
        public static string apiUrl = string.Empty;
        public static bool OWarnings = false;
        public static bool OStartUp = false;
        public static bool OWinLoginState = false;
        public static bool compileTime = false;
        public static bool codeWriter = false;
        public static string Framework = "net6.0-windows";
        public static bool noClear = false;
        public static string binaryName = string.Empty;
        public static string livePassword = string.Empty;
        public static string remoteLivePassword = string.Empty;
        public static string sessionId = string.Empty;
        public static string sessionIdMain = string.Empty;
        public static string remoteSessionId = string.Empty;
        public static string remoteConnectionId = string.Empty;
        public static string binaryNameStore = string.Empty;
        public static bool connected = false;
        public static bool liveDisconnected = false;
        public static bool apiConnected = false;
        public static bool apiRemoteConnected = false;
        public static bool typeConnection = false;
        public static bool isConnected = false;
        public static int reconnectionCount = 6;
        public static string ciarePath = $"{Application.StartupPath}CIARE.exe";
        public static readonly string registryPath = "SOFTWARE\\CIARE";
        public static readonly string regUserRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        public static readonly string codeCompletionKey = "OCodeCompletion";
        public static readonly string foldingCodeKey = "OFoldingCode";
        public static readonly string lineNumberKey = "OLineNumber";
        public static readonly string warnings = "OWarnings";
        public static readonly string liveShare = "OLiveShare";
        public static readonly string startUp = "OStartUp";
        public static readonly string OWinLogin = "OWinLogin";
        public static readonly string activeForm = "activeForm";
        public static readonly string OConfigParam = "OConfigParam";
        public static readonly string OPlatformParam = "OPlatformParam";
        public static readonly string OFramework = "OFramework";
        public static readonly string roslynTemplate = @"/*
 * Simple C# code sample for run with Roslyn runtime code compiler and execution.
 * Top-level statements can be used as well.
 */

using System;
// You can add more dependencies.

namespace Test_Code
{
  class Test
  {	
     static void Main(string[] args)
     {
        //Do work here!
     }
  }
}
";

    }
}
