﻿
namespace CIARE.Utils
{
    public class GlobalVariables
    {
        public static string paramData = string.Empty;
        public static string openedFilePath = string.Empty;
        public static string commandLineArguments = string.Empty;
        public static bool savedFile = false;
        public static bool exeName = false;
        public static bool checkFormOpen = false;
        public static bool outPutDisplay = false;
        public static bool findTabOpen = false;
        public static bool darkColor = false;
        public static string binaryName = string.Empty; 
        public static string binaryNameStore = string.Empty; 
        public static readonly string registryPath = "SOFTWARE\\CIARE";
        public static readonly string roslynTemplate = @"/*
 * Simple C# code sample for run with Roslyn runtime code compiler and execution.
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
