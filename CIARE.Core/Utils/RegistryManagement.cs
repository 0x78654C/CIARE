using Microsoft.Win32;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace CIARE.Core.Utils
{
    [SupportedOSPlatform("windows")]
    public static class RegistryManagement
    {


        /// <summary>
        /// Registry key check
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public static bool RegKey_Check(string keyName, string subKeyName)
        {
            try
            {
                if (Registry.GetValue(keyName, subKeyName, null) == null)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Registry key wirte
        /// </summary>
        /// <param name="keyPath"></param>
        /// <param name="keyName"></param>
        /// <param name="keyValue"></param>
        public static void RegKey_WriteSubkey(string keyName, string subKeyName, string subKeyValue)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
            (keyName, true);
            rk.SetValue(subKeyName, subKeyValue);
        }

        /// <summary>
        /// Registry key reader.
        /// </summary>
        /// <param name="keyPath"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public static string RegKey_Read(string keyName, string subKeyName)
        {
            string key = string.Empty;

            string InstallPath = (string)Registry.GetValue(keyName, subKeyName, null);
            if (InstallPath != null)
            {
                key = InstallPath;
            }
            return key;
        }

        /// <summary>
        ///  Registry key create
        /// </summary>
        /// <param name="keyName"></param>
        /// <param name="subKeyName"></param>
        /// <param name="subKeyValue"></param>

        public static void RegKey_CreateKey(string keyName, string subKeyName, string subKeyValue)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey
            (keyName);

            key.SetValue(subKeyName, subKeyValue);
            key.Close();
        }

        /// <summary>
        /// Delete a subkey by name.
        /// </summary>
        /// <param name="keyName">Main key name.</param>
        /// <param name="subKeyName">Sub key name.</param>
        public static void RegKey_Delete(string keyName, string subKeyName)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(keyName, true))
            {
                key.DeleteValue(subKeyName);
            }
        }

        /// <summary>
        /// Check if share point path is avaible on registry.
        /// </summary>
        /// <param name="regKeyList"></param>
        /// <param name="keyName"></param>
        /// <param name="subKeyValue"></param>
        public static void CheckRegKeysStart(List<string> regKeyList, string keyName, string subKeyValue, bool zero)
        {
            foreach (var key in regKeyList)
            {
                if (string.IsNullOrEmpty(RegKey_Read(keyName, key)))
                    RegKey_CreateKey(keyName, key, subKeyValue);
            }
        }
    }
}
