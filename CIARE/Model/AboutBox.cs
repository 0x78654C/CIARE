using System;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using CIARE.Utils;
using CIARE.GUI;
using System.Runtime.Versioning;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    partial class AboutBox : Form
    {
        [SupportedOSPlatform("windows")]
        public AboutBox()
        {
            InitializeComponent();
            string version = GetDisplayVersion(AssemblyVersion);

            this.Text = string.Format("About {0}", AssemblyTitle);
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = string.Format("VERSION   : {0}", version);
            this.labelCopyright.Text = string.Format("COPYRIGHT : {0}", AssemblyCopyright);
            this.labelCompanyName.Text = string.Format("PUBLISHER : {0}", GetPublisherName());
            this.labelStatus.Text = "MODE      : WINFORMS / ROSLYN / PROJECT + SINGLE FILE";
            this.textBoxDescription.Text = BuildAboutDescription(version);
            this.textBoxDescription.SelectionStart = 0;
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            ApplyRetroTheme();
        }

        /// <summary>
        /// Overwrite the key press.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Escape:
                    this.Close();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private static string GetDisplayVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return "unknown";

            Version parsedVersion;
            if (!Version.TryParse(version, out parsedVersion))
                return version;

            return parsedVersion.Build >= 0
                ? string.Format("{0}.{1}.{2}", parsedVersion.Major, parsedVersion.Minor, parsedVersion.Build)
                : string.Format("{0}.{1}", parsedVersion.Major, parsedVersion.Minor);
        }

        private string GetPublisherName()
        {
            return string.IsNullOrWhiteSpace(AssemblyCompany) ? "x_coding" : AssemblyCompany;
        }

        private string BuildAboutDescription(string version)
        {
            return string.Join(Environment.NewLine, new[]
            {
                "BOOT MESSAGE",
                "",
                "CIARE is a lightweight Windows editor for writing, inspecting, compiling, and running C# code with Roslyn.",
                "It works for quick single-file experiments and for full .sln / .csproj workflows.",
                "",
                "CORE SYSTEMS",
                "- Syntax highlighting, code folding, completion, and real-time diagnostics.",
                "- Built-in file explorer with project and solution awareness.",
                "- Smart build target detection near the active file.",
                "- Compile/run, binary build, publish, command-line arguments, and output capture.",
                "- NuGet and reference management for external assemblies and packages.",
                "- Go to Definition, Find Usages, split editors, Live Share, and AI integrations.",
                "",
                "PROJECT CONSOLE",
                "- Ctrl+Shift+N creates a new C# project or solution.",
                "- Ctrl+Shift+O opens an existing .sln or .csproj.",
                "",
                string.Format("VERSION: {0}", version),
                "PROJECT: https://github.com/0x78654C/CIARE",
                "CONTACT: xcoding.dev@gmail.com"
            });
        }

        private void ApplyRetroTheme()
        {
            Color terminalBack = Color.FromArgb(8, 13, 10);
            Color panelBack = Color.FromArgb(14, 24, 17);
            Color headerBack = Color.FromArgb(18, 48, 28);
            Color phosphor = Color.FromArgb(130, 255, 170);
            Color dimPhosphor = Color.FromArgb(92, 196, 126);
            Color amber = Color.FromArgb(255, 196, 92);

            BackColor = terminalBack;
            ForeColor = phosphor;

            tableLayoutPanel.BackColor = terminalBack;
            logoPictureBox.BackColor = panelBack;

            labelHeaderBar.BackColor = headerBack;
            labelHeaderBar.ForeColor = amber;
            labelProductName.BackColor = terminalBack;
            labelProductName.ForeColor = amber;
            labelTagLine.BackColor = terminalBack;
            labelTagLine.ForeColor = phosphor;

            Label[] metadataLabels =
            {
                labelVersion,
                labelCopyright,
                labelCompanyName,
                labelStatus
            };

            foreach (Label label in metadataLabels)
            {
                label.BackColor = terminalBack;
                label.ForeColor = dimPhosphor;
            }

            textBoxDescription.BackColor = Color.FromArgb(3, 8, 5);
            textBoxDescription.ForeColor = phosphor;
            textBoxDescription.BorderStyle = BorderStyle.FixedSingle;

            okButton.BackColor = headerBack;
            okButton.ForeColor = phosphor;
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.FlatAppearance.BorderColor = phosphor;
            okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 68, 40);
            okButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(34, 90, 52);
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion
    }
}
