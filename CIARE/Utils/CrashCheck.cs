using System.Runtime.Versioning;

namespace CIARE.Utils
{
    /*
      Class for storing in registry the status (closed/open) of CIARE form.
     */

    [SupportedOSPlatform("windows")]
    public class CrashCheck
    {
        public string RegCrashCheck { get; set; }
        public string RegCiarePath{ get; set; }

        public CrashCheck(string regCiarePath, string regCreashCheck)
        {
            RegCiarePath = regCiarePath;
            RegCrashCheck = regCreashCheck;
        }

        /// <summary>
        /// Set status on form load.
        /// </summary>
        public void SetActiveFormState() => RegistryManagement.RegKey_WriteSubkey(RegCiarePath, RegCrashCheck, "True");

        /// <summary>
        /// Set status on form close.
        /// </summary>
        public void SetClosedFormState() => RegistryManagement.RegKey_WriteSubkey(RegCiarePath, RegCrashCheck, "False");

        /// <summary>
        /// Check status stored on form close/load event.
        /// </summary>
        /// <returns></returns>
        public bool CheckCrashStatus()
        {
            string crashStat = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{RegCiarePath}", RegCrashCheck);
            return crashStat.Length > 0 ? bool.Parse(crashStat) : false;
        }
    }
}
