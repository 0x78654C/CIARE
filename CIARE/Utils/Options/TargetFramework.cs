﻿using System;
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

            if (framework.Text == ".NET 6")
            {
                if (!SdkVersion.CheckSdk(framework.Text[^1..]))
                {
                    MessageBox.Show("The targeted framework (.NET 6) is not installed!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GetFramework(CIARE.Options.Instance.frameWorkCMB, GlobalVariables.registryPath);
                    return;
                }

                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net6.0-windows");
                GlobalVariables.Framework = "net6.0-windows";
                return;
            }

            if (framework.Text == ".NET 7")
            {

                if (!SdkVersion.CheckSdk(framework.Text[^1..]))
                {
                    MessageBox.Show("The targeted framework (.NET 7) is not installed!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GetFramework(CIARE.Options.Instance.frameWorkCMB, GlobalVariables.registryPath);
                    return;
                }
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net7.0-windows");
                GlobalVariables.Framework = "net7.0-windows";
                return;
            }

            if (!SdkVersion.CheckSdk(framework.Text[^1..]))
            {
                MessageBox.Show("The targeted framework (.NET 8) is not installed!", "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
                GetFramework(CIARE.Options.Instance.frameWorkCMB, GlobalVariables.registryPath);
                return;
            }

            RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, regKeyName, "net8.0-windows");
            GlobalVariables.Framework = "net8.0-windows";
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
                if(regFramework.StartsWith("net6"))
                    framework.SelectedIndex = 0;
                else if(regFramework.StartsWith("net7"))
                    framework.SelectedIndex = 1;
                else
                    framework.SelectedIndex = 2;
                return;
            }
            framework.SelectedIndex = 0;
        }
    }
}
