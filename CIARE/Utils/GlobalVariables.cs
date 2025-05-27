using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using System.Runtime.Versioning;
using Microsoft.Extensions.AI;
using System.Diagnostics;
using System.Security;

namespace CIARE.Utils
{
    [SupportedOSPlatform("windows")]
    public class GlobalVariables
    {
        public static string paramData = string.Empty;
        private static string s_rootPath = Path.GetPathRoot(Environment.SystemDirectory);
        private static readonly string s_accountName = Environment.UserName;
        public static readonly string userProfileDirectory = $"{s_rootPath}Users\\{s_accountName}\\AppData\\Local\\CIARE\\";
        public static readonly string markFile = $"{userProfileDirectory}markedFiles.cDat";
        public static readonly string markFileTemp = $"{userProfileDirectory}markedFiles_tmp.cDat";
        public static readonly string tabsFilePath = $"{userProfileDirectory}tabsFilePath.cDat";
        public static readonly string tabsFilePathAll = $"{userProfileDirectory}tabsFilePathAll.cDat";
        public static string processArg = string.Empty;
        private static readonly string getGetVersionName = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static readonly string versionName = getGetVersionName.Substring(0, getGetVersionName.Length); // set -2  for next release
        //public static readonly string versionName = getGetVersionName; // patch only till new feature is out
        public static bool isStoringTabs = false;
        public static string openedFilePath { get; set; } = string.Empty;
        public static string openedFileName = string.Empty;
        public static string openedFileMD5 { get; set; } = "";
        public static int openedFileLen = 0;
        public static string commandLineArguments = string.Empty;
        public static string findData = string.Empty;
        public static string findWhat = string.Empty;
        public static string repalceWith = string.Empty;
        public static bool savedFile { get; set; } = false;
        public static bool savedFileNoMD5Check = false;
        public static bool noFileSelected = false;
        public static bool splitWindowPosition = false;
        public static TextAreaControl textAreaFirst { get; set; }
        public static TextAreaControl textAreaSecond { get; set; }
        public static bool exeName = false;
        public static bool checkFormOpen = false;
        public static bool outPutDisplay = false;
        public static bool findTabOpen = false;
        public static bool noPath = false;
        public static bool darkColor = false;
        public static string configParam = "/p:configuration=Debug";
        public static string platformParam = "/p:Platform=\"Any CPU\"";
        public static string publishAot = string.Empty;
        public static OutputKind OutputKind;
        public static bool OCodeCompletion = true;
        public static bool OLineNumber = true;
        public static bool OFoldingCode = true;
        public static string apiUrl = string.Empty;
        public static bool OStartUp = false;
        public static bool OWinLoginState = false;
        public static bool OUnsafeCode = false;
        public static bool OPublishNative = false;
        public static bool codeWriter = false;
        public static bool isRed = false;

        public static string Framework { get; set; } = "net6.0-windows";
        public static bool noClear { get; set; } = false;
        public static string binaryName = string.Empty;
        public static string livePassword = string.Empty;
        public static string remoteLivePassword = string.Empty;
        public static string sessionId = string.Empty;
        public static string sessionIdMain = string.Empty;
        public static string remoteSessionId = string.Empty;
        public static string remoteConnectionId = string.Empty;
        public static string binaryNameStore = string.Empty;
        public static string binarytype = ".exe";
        public static string binarytypeTemplate = "Exe";
        public static bool binaryPublish = false;
        public static bool winForms = false;
        public static bool connected = false;
        public static bool liveDisconnected = false;
        public static bool apiConnected = false;
        public static bool apiRemoteConnected = false;
        public static bool typeConnection = false;
        public static bool isConnected = false;
        public static int reconnectionCount { get; set; } = 6;
        public static int liveTabIndex = 0;
        public static string ciarePath = $"{Application.StartupPath}CIARE.exe";
        public static string registryPath = "SOFTWARE\\CIARE";
        public static string regUserRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        public static string codeCompletionKey = "OCodeCompletion";
        public static string foldingCodeKey = "OFoldingCode";
        public static string lineNumberKey = "OLineNumber";
        public static string liveShare = "OLiveShare";
        public static string startUp = "OStartUp";
        public static string unsafeCode = "OUnsafeCode";
        public static string publish = "OPublishNative";
        public static string OWinLogin = "OWinLogin";
        public static string activeForm = "activeForm";
        public static string OConfigParam = "OConfigParam";
        public static string OPlatformParam = "OPlatformParam";
        public static string OFramework = "OFramework";
        public static bool isCLIOpen = false;
        public static readonly string OlastTabPosition = "lastTabPosition";
        public static int selectedIndex = 0;
        //output richtextbox zoomfactor varlue store;
        public static float zoomFactor = 1f;
        //-----------
        // --AI vars--
        public static readonly string openAIKey = "openAIKey";
        public static readonly string openModel = "Model";
        public static readonly string ollamModel = "OllamaModel";
        public static readonly string openAIMaxTokens = "openAIMaxTokens";
        public static string aiMaxTokens = string.Empty;
        public static SecureString aiKey;
        public static string model = string.Empty;
        public static string modelOllamaVar = string.Empty;
        public static string aiType = "OpenAI";
        public static string aiTypeVar = "";
        public static string ollamaUri = "http://localhost:11434/";
        public static List<ChatMessage> chatHistory = new();
        public static string errorAiResponse = string.Empty;

        // --Reference----
        public static List<string> customRefAsm { get; set; } = new List<string>(); // Used to store custom asspably path file.
        public static List<string> customRefList { get; set; } = new List<string>();
        public static List<string> blackRefList { get; set; } = new List<string>();
        //- NuGet-
        public static readonly string downloadNugetPath = $"{userProfileDirectory}nuget\\";
        public static List<string> nugetPackage = new List<string>();
        public static List<string> filteredCustomRef { get; set; } = new List<string>();
        public static List<string> nugetNames { get; set; } = new List<string>();
        public static List<string> downloadPackages = new List<string>();
        public static List<string> depNugetFiles = new List<string>();
        public const string nugetApi = "https://api.nuget.org/v3/index.json";
        public static bool isFrameworkFound = false;
        public static bool isVStheme = false;
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
