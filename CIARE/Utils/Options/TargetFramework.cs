using System;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.Options
{
    [SupportedOSPlatform("windows")]
    public class TargetFramework
    {
        /// <summary>
        /// Check stored status for target framework in registry.
        /// </summary>
        /// <param name="regKeyName"></param>
        public static void CheckFramework(string regKeyName)
        {
            string regFramework = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.OFramework);
            if (regFramework.Length > 0)
                GlobalVariables.Framework = regFramework;
        }

        /// <summary>
        /// Store in registry target framework status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void SetFramework(ComboBox framework, string regKeyName)
        {
            GlobalVariables.selectedIndex = framework.SelectedIndex;

            if (framework.Text.StartsWith(".NET 6"))
            {
                if (!SdkVersion.CheckSdk(framework.Text.Substring(0, 6)))
                {
                    MessageBox.Show("The targeted framework (.NET 6) is not installed!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GetFramework(CIARE.Options.Instance.frameWorkCMB, GlobalVariables.registryPath);
                    return;
                }

                if (framework.Text.Contains("Windows"))
                {
                    RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net6.0-windows");
                    GlobalVariables.Framework = "net6.0-windows";
                    GlobalVariables.winForms = true;
                }
                else
                {
                    RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net6.0");
                    GlobalVariables.Framework = "net6.0";
                    GlobalVariables.winForms = false;
                }
                return;
            }

            if (framework.Text.StartsWith(".NET 7"))
            {

                if (!SdkVersion.CheckSdk(framework.Text.Substring(5, 1)))
                {
                    MessageBox.Show("The targeted framework (.NET 7) is not installed!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GetFramework(CIARE.Options.Instance.frameWorkCMB, GlobalVariables.registryPath);
                    return;
                }
                if (framework.Text.Contains("Windows"))
                {
                    RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net7.0-windows");
                    GlobalVariables.Framework = "net7.0-windows";
                    GlobalVariables.winForms = true;
                }
                else
                {
                    RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net7.0");
                    GlobalVariables.Framework = "net7.0";
                    GlobalVariables.winForms = false;
                }
                return;
            }

            if (!SdkVersion.CheckSdk(framework.Text.Substring(5, 1)))
            {
                MessageBox.Show("The targeted framework (.NET 8) is not installed!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                GetFramework(CIARE.Options.Instance.frameWorkCMB, GlobalVariables.registryPath);
                return;
            }

            if (framework.Text.Contains("Windows"))
            {
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net8.0-windows");
                GlobalVariables.Framework = "net8.0-windows";
                GlobalVariables.winForms = true;
            }
            else
            {
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net8.0");
                GlobalVariables.Framework = "net8.0";
                GlobalVariables.winForms = false;
            }
        }

        /// <summary>
        /// Get framework and set combobox text to specified target framework status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="regKeyName"></param>
        public static void GetFramework(ComboBox framework, string regKeyName)
        {
            string regFramework = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{regKeyName}", GlobalVariables.OFramework);
            if (regFramework.Length > 0)
            {
                //framework.SelectedIndex = regFramework.StartsWith("net6") ? 0 : 1; 
                if (regFramework =="net6.0")
                    framework.SelectedIndex = 0;
                else if (regFramework == "net6-windows")
                    framework.SelectedIndex = 1;
                else if (regFramework == "net7.0")
                    framework.SelectedIndex = 2;
                else if (regFramework == "net7-windows")
                    framework.SelectedIndex = 3;
                else if (regFramework == "net8.0")
                    framework.SelectedIndex = 4;
                else if (regFramework == "net8.0-windows")
                    framework.SelectedIndex = 5;
                else
                    framework.SelectedIndex = 0;
                return;
            }
        }
    }
}
