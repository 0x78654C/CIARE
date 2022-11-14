using System;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Utils.FilesOpenOS
{
    [SupportedOSPlatform("windows")]
    public class AutoStartFile
    {

        public string UserRunRegistryPath { get; set; }
        public string UserAppdataFile { get; set; }
        public string UserAppdataFileTemp { get; set; }
        public string OpenedFilePath { get; set; }
        private string _ciarePath = GlobalVariables.ciarePath;
        private string _runCiareReg = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{GlobalVariables.regUserRunPath}", "CIARE");
        private string _regWinLogin = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", GlobalVariables.OWinLogin);
        public AutoStartFile(string userRunRegistryPath, string userAppdataFile, string userAppdataFileTemp, string openedFilePath)
        {
            UserRunRegistryPath = userRunRegistryPath;
            UserAppdataFile = userAppdataFile;
            OpenedFilePath = openedFilePath;
            UserAppdataFileTemp = userAppdataFileTemp;
        }

        /// <summary>
        /// Store marked file for startup in user CIARE directory.
        /// </summary>
        public void SetFilePath(CheckBox checkBox)
        {
            if (string.IsNullOrEmpty(OpenedFilePath))
            {
                checkBox.Checked = false;
                return;
            }

            if (!File.Exists(UserAppdataFile))
                File.WriteAllText(UserAppdataFile, String.Empty);

            var fileContent = File.ReadAllText(UserAppdataFile);
            if (checkBox.Checked)
            {
                if (!string.IsNullOrEmpty(OpenedFilePath) && !fileContent.Contains(OpenedFilePath))
                    File.AppendAllText(UserAppdataFile, OpenedFilePath + Environment.NewLine);
            }
            else
            {
                if (OpenedFilePath.Length > 0)
                    File.WriteAllText(UserAppdataFile, RemoveLine());
            }
        }

        /// <summary>
        /// Remove line from marked files.
        /// </summary>
        /// <returns></returns>
        private string RemoveLine()
        {
            string data = string.Empty;
            var fileLines = File.ReadAllLines(UserAppdataFile);
            foreach (var line in fileLines)
            {
                if (!line.Contains(OpenedFilePath))
                    data += line + Environment.NewLine;
            }
            return data;
        }

        /// <summary>
        /// Remove files that does not exist anymore on main mark file.
        /// </summary>
        private void RemoveLineUnexistingPath()
        {
            string data = string.Empty;
            var fileLines = File.ReadAllLines(UserAppdataFile);
            foreach (var line in fileLines)
            {
                if (File.Exists(line))
                    data += line + Environment.NewLine;
            }
            File.WriteAllText(UserAppdataFile, data);
        }


        /// <summary>
        /// Check if marked file equals to opened file path
        /// </summary>
        /// <returns></returns>
        public void CheckFilePath()
        {
            if (!File.Exists(UserAppdataFile))
                return;

            if (string.IsNullOrEmpty(OpenedFilePath))
                return;

            var readFile = File.ReadAllText(UserAppdataFile);
            Form1.Instance.markStartFileChk.Checked = readFile.Contains(OpenedFilePath);
        }


        /// <summary>
        /// Check if appdata file is empty or not.
        /// </summary>
        /// <param name="userAppdataFile"></param>
        /// <returns></returns>
        public bool CheckFileContent(string userAppdataFile)
        {
            return (File.ReadAllText(userAppdataFile).Length > 0);
        }

        /// <summary>
        /// Set CIARE to start on windows login if there are marked files for open.
        /// </summary>
        public void SetRegistryRunApp(CheckBox checkBox)
        {
            if (checkBox.Checked)
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.regUserRunPath, "CIARE", _ciarePath);
            else
            {
                if (_runCiareReg.Length > 0)
                    RegistryManagement.RegKey_Delete(GlobalVariables.regUserRunPath, "CIARE");
            }
            GlobalVariables.OWinLoginState = checkBox.Checked;
        }

        /// <summary>
        /// Set CIARE to start on windows login if there are marked files for open.
        /// </summary>
        public void SetRegistryRunApp()
        {
            bool state;
            if (CheckFileContent(UserAppdataFile))
            {
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.regUserRunPath, "CIARE", _ciarePath);
                state = true;
            }
            else
            {
                if (_runCiareReg.Length > 0)
                    RegistryManagement.RegKey_Delete(GlobalVariables.regUserRunPath, "CIARE");
                state = false;
            }
            GlobalVariables.OWinLoginState = state;
            if (_regWinLogin.Length > 0)
                RegistryManagement.RegKey_WriteSubkey(GlobalVariables.registryPath, GlobalVariables.OWinLogin, state.ToString());
        }


        /// <summary>
        /// Open marked files on windows log in.
        /// </summary>
        public void OpenFilesOnLongOn(string argParam)
        {
            if (!CheckFlag())
                return;

            if (File.Exists(UserAppdataFileTemp))
                return;

            File.WriteAllText(UserAppdataFileTemp, string.Empty);

            if (!CheckFileContent(UserAppdataFile))
                return;

            RemoveLineUnexistingPath();
            ProcessRun processRun;
            var fileLines = File.ReadAllLines(UserAppdataFile);

            foreach (var line in fileLines)
            {
                if (line.Length > 0)
                {
                    string dataReadTemp = File.ReadAllText(UserAppdataFileTemp);
                    if (!dataReadTemp.Contains(line))
                    {

                        if (!string.IsNullOrEmpty(argParam))
                        {
                            processRun = new ProcessRun(_ciarePath, argParam, Application.StartupPath);
                            processRun.RunVisible();
                            break;
                        }
                        if (!CheckListFiles())
                        {
                            processRun = new ProcessRun(_ciarePath, line, Application.StartupPath);
                            processRun.RunVisible();
                            File.AppendAllText(UserAppdataFileTemp, line + Environment.NewLine);
                        }
                    }
                }
            }

            //TODO: This is nasty. I know. I will remake it.
            try
            {
                if (string.IsNullOrEmpty(Form1.Instance.textEditorControl1.Text))
                    Environment.Exit(0);
            }
            catch
            {
                Environment.Exit(0);
            }

            if (File.Exists(UserAppdataFileTemp) && CheckListFiles())
                File.Delete(UserAppdataFileTemp);
        }


        /// <summary>
        /// Clear registry flag for CIARE startup check.
        /// </summary>
        public void DelTempFile()
        {
            if (File.Exists(UserAppdataFileTemp))
                File.Delete(UserAppdataFileTemp);
        }

        /// <summary>
        /// Check if flag is set for startup.
        /// </summary>
        /// <returns></returns>
        public bool CheckFlag()
        {
            var regStartUpFlag = RegistryManagement.RegKey_Read($"HKEY_CURRENT_USER\\{GlobalVariables.registryPath}", "OStartUp");
            return regStartUpFlag == "True" ? true : false;
        }

        /// <summary>
        /// Check if temp list equals with mark list. 
        /// </summary>
        /// <returns></returns>
        public bool CheckListFiles()
        {
            if (!File.Exists(UserAppdataFileTemp) || !File.Exists(UserAppdataFile))
                return false;

            string dataMarkTemp = File.ReadAllText(UserAppdataFileTemp);
            string dataMarkFile = File.ReadAllText(UserAppdataFile);

            return dataMarkFile == dataMarkTemp;
        }


        /// <summary>
        /// Check if form was closed properly and if not delete temp mark file.
        /// </summary>
        public void CheckSetAtiveFormState()
        {
            CrashCheck crashCheck = new CrashCheck(GlobalVariables.registryPath, GlobalVariables.activeForm);
            bool status = crashCheck.CheckCrashStatus();
            if (!status)
            {
                if (File.Exists(UserAppdataFileTemp))
                    File.Delete(UserAppdataFileTemp);
                crashCheck.SetActiveFormState();
                return;
            }
            crashCheck.SetActiveFormState();
        }
    }
}
