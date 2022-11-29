using System;
using System.Runtime.InteropServices;

namespace CIARE.Utils
{
    public class SplitArguments
    {
   
        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW(
    [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetCommandLine();

        /// <summary>
        /// Get command line arguments without split by space.
        /// </summary>
        /// <returns></returns>
        public static string GetCommandLineArgs()
        {
            IntPtr ptr = GetCommandLine();

            return Marshal.PtrToStringAuto(ptr);
        }

        /// <summary>
        /// Convert string to Command lines arguments.
        /// </summary>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }
    }
}
