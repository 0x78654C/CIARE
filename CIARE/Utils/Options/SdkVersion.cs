using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                        outList.Add(line.Split(' ')[0]);
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
        public static bool CheckSdk(string selectedVersion)
        {
            selectedVersion = (selectedVersion ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(selectedVersion))
                return false;

            return Versions().Any(f =>
                f.StartsWith(selectedVersion + ".", System.StringComparison.OrdinalIgnoreCase) ||
                f.StartsWith(selectedVersion, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
