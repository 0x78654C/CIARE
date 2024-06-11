using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class SdkVersion
    {
        /// <summary>
        ///  Get sdk version.
        /// </summary>
        /// <returns></returns>
        private static List<string> Versions()
        {
            List<string> outList = new List<string>();
            var processRun = new ProcessRun("dotnet", "--list-sdks", Application.UserAppDataPath);
            var sdkResult = processRun.Run();
            using (var reader = new StringReader(sdkResult))
            {
                var line = string.Empty;
                while (null != (line = reader.ReadLine()))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        int len = line.Length - 1;
                        outList.Add(line[..^len]);
                    }
                }
            }
            return outList;
        }

        /// <summary>
        /// Check if current sdk exist.
        /// </summary>
        /// <param name="comboBox"></param>
        /// <param name="sdkVersionStart"></param>
        public static bool CheckSdk(string selectedVersion) => Versions().Contains(selectedVersion);
    }
}
