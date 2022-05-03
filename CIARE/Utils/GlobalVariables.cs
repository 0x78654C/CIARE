namespace CIARE.Utils
{
    public class GlobalVariables
    {
        public static string paramData = string.Empty;
        public static string openedFilePath = string.Empty;
        public static bool savedFile = false;
        public static readonly string registryPath = "SOFTWARE\\CIARE";
        public static readonly string roslynTemplate = @"/*
 * Simple C# code sample for run with Roslyn runtime code compiler and execution.
 */

using System;
using System.Windows.Forms;
// You can add more dependencies.

namespace Test_Code
{
  class Test
  {	
     static void Main(string[] arg)
     {
        //Do work here!
     }
  }
}
";

    }
}
