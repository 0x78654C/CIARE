using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Utils.FilesOpenOS
{
    public class AutoStartFile
    {
        public string UserRunRegistryPath { get; set; }
        public string UserAppdataFile { get; set; }
        public string OpenedFilePath { get; set; }

        public AutoStartFile(string userRunRegistryPath, string userAppdataFile, string openedFilePath)
        {
            UserRunRegistryPath = userRunRegistryPath;
            UserAppdataFile = userAppdataFile;
            OpenedFilePath = openedFilePath;
        }

        /// <summary>
        /// Store marked file for startup in user CIARE directory.
        /// </summary>
        public void SetFilePath(CheckBox checkBox)
        {
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
        /// Check if marked file equals to opened file path
        /// </summary>
        /// <returns></returns>
        public void CheckFilePath()
        {
            if (!File.Exists(UserAppdataFile))
                return;

            var readFile = File.ReadAllText(UserAppdataFile);
            Form1.Instance.markStartFileChk.Checked = readFile.Contains(OpenedFilePath);
        }


        /// <summary>
        /// Check if appdata file is empty or not.
        /// </summary>
        /// <param name="userAppdataFile"></param>
        /// <returns></returns>
        private bool CheckFileContent(string userAppdataFile)
        {
            return (File.ReadAllText(userAppdataFile).Length > 0);
        }
    }
}
