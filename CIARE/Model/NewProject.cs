using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;

namespace CIARE
{
    [SupportedOSPlatform("windows")]
    public sealed class NewProjectResult
    {
        public string WorkspacePath { get; set; } = string.Empty;
        public string ProjectFilePath { get; set; } = string.Empty;
        public string SolutionFilePath { get; set; } = string.Empty;
        public string StarterFilePath { get; set; } = string.Empty;
    }

    [SupportedOSPlatform("windows")]
    public partial class NewProject : Form
    {
        private const string CSharpProjectTypeGuid = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";

        private readonly ListBox _templateList = new ListBox();
        private readonly TextBox _projectNameText = new TextBox();
        private readonly TextBox _locationText = new TextBox();
        private readonly TextBox _solutionNameText = new TextBox();
        private readonly ComboBox _targetFrameworkCombo = new ComboBox();
        private readonly CheckBox _createSolutionCheckBox = new CheckBox();
        private readonly Button _createButton = new Button();
        private readonly Button _cancelButton = new Button();
        private readonly Button _browseButton = new Button();
        private bool _syncingSolutionName;
        private string _lastProjectName = "CiareApp";

        public NewProjectResult CreatedProject { get; private set; }

        public NewProject()
        {
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            Text = "New Project";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 460);
            MinimumSize = new Size(680, 420);
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ShowInTaskbar = false;
            Font = SystemFonts.MessageBoxFont;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _templateList.Dock = DockStyle.Fill;
            _templateList.IntegralHeight = false;
            _templateList.Items.AddRange(ProjectTemplate.All.Cast<object>().ToArray());
            _templateList.SelectedIndex = 0;
            _templateList.SelectedIndexChanged += (_, _) => UpdateTemplateSelection();

            var templatePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1
            };
            templatePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            templatePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            templatePanel.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Project type"
            }, 0, 0);
            templatePanel.Controls.Add(_templateList, 0, 1);

            var optionsLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 7,
                Padding = new Padding(12, 0, 0, 0)
            };
            optionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            optionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            optionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            _projectNameText.Dock = DockStyle.Fill;
            _projectNameText.Text = "CiareApp";
            _projectNameText.TextChanged += ProjectNameText_TextChanged;

            _locationText.Dock = DockStyle.Fill;
            _locationText.Text = GetDefaultProjectLocation();

            _browseButton.Dock = DockStyle.Fill;
            _browseButton.Text = "Browse";
            _browseButton.Click += BrowseButton_Click;

            _solutionNameText.Dock = DockStyle.Fill;
            _solutionNameText.Text = _projectNameText.Text;

            _targetFrameworkCombo.Dock = DockStyle.Fill;
            _targetFrameworkCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _targetFrameworkCombo.Items.AddRange(new object[]
            {
                "net6.0",
                "net6.0-windows",
                "net7.0",
                "net7.0-windows",
                "net8.0",
                "net8.0-windows",
                "net9.0",
                "net9.0-windows",
                "net10.0",
                "net10.0-windows"
            });
            SelectTargetFramework(GlobalVariables.Framework);
            _targetFrameworkCombo.SelectedIndexChanged += (_, _) => UpdateTemplateSelection();

            _createSolutionCheckBox.AutoSize = true;
            _createSolutionCheckBox.Checked = true;
            _createSolutionCheckBox.Text = "Create solution file (.sln)";
            _createSolutionCheckBox.CheckedChanged += (_, _) =>
                _solutionNameText.Enabled = _createSolutionCheckBox.Checked;

            _createButton.Text = "Create";
            _createButton.Width = 92;
            _createButton.Height = 28;
            _createButton.Click += CreateButton_Click;

            _cancelButton.Text = "Cancel";
            _cancelButton.Width = 92;
            _cancelButton.Height = 28;
            _cancelButton.DialogResult = DialogResult.Cancel;

            AddOptionRow(optionsLayout, 0, "Name:", _projectNameText);
            AddOptionRow(optionsLayout, 1, "Location:", _locationText);
            optionsLayout.Controls.Add(_browseButton, 2, 1);
            AddOptionRow(optionsLayout, 2, "Solution:", _solutionNameText);
            AddOptionRow(optionsLayout, 3, "Framework:", _targetFrameworkCombo);
            optionsLayout.Controls.Add(_createSolutionCheckBox, 1, 4);
            optionsLayout.SetColumnSpan(_createSolutionCheckBox, 2);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0)
            };
            buttonPanel.Controls.Add(_createButton);
            buttonPanel.Controls.Add(_cancelButton);
            optionsLayout.Controls.Add(buttonPanel, 0, 6);
            optionsLayout.SetColumnSpan(buttonPanel, 3);

            mainLayout.Controls.Add(templatePanel, 0, 0);
            mainLayout.Controls.Add(optionsLayout, 1, 0);
            Controls.Add(mainLayout);

            AcceptButton = _createButton;
            CancelButton = _cancelButton;

            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
            UpdateTemplateSelection();
        }

        private static void AddOptionRow(TableLayoutPanel layout, int row, string labelText, Control control)
        {
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Text = labelText,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 7, 8, 0)
            }, 0, row);

            layout.Controls.Add(control, 1, row);
            layout.SetColumnSpan(control, row == 1 ? 1 : 2);
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select project location";
                dialog.ShowNewFolderButton = true;
                if (Directory.Exists(_locationText.Text))
                    dialog.SelectedPath = _locationText.Text;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    _locationText.Text = dialog.SelectedPath;
            }
        }

        private void ProjectNameText_TextChanged(object sender, EventArgs e)
        {
            if (_syncingSolutionName)
                return;

            string projectName = _projectNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(_solutionNameText.Text) ||
                string.Equals(_solutionNameText.Text.Trim(), _lastProjectName, StringComparison.Ordinal))
            {
                _syncingSolutionName = true;
                _solutionNameText.Text = projectName;
                _syncingSolutionName = false;
            }

            _lastProjectName = projectName;
        }

        private void UpdateTemplateSelection()
        {
            if (SelectedTemplate.RequiresWindowsTargetFramework &&
                !_targetFrameworkCombo.Text.EndsWith("-windows", StringComparison.OrdinalIgnoreCase))
            {
                SelectTargetFramework(_targetFrameworkCombo.Text + "-windows");
            }
        }

        private void SelectTargetFramework(string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework))
                targetFramework = "net8.0";

            for (int i = 0; i < _targetFrameworkCombo.Items.Count; i++)
            {
                if (string.Equals(_targetFrameworkCombo.Items[i].ToString(), targetFramework,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _targetFrameworkCombo.SelectedIndex = i;
                    return;
                }
            }

            _targetFrameworkCombo.SelectedIndex = 4;
        }

        private ProjectTemplate SelectedTemplate =>
            _templateList.SelectedItem as ProjectTemplate ?? ProjectTemplate.All[0];

        private void CreateButton_Click(object sender, EventArgs e)
        {
            try
            {
                CreatedProject = CreateProject();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "New Project", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private NewProjectResult CreateProject()
        {
            string projectName = ValidateFileName(_projectNameText.Text, "project name");
            string solutionName = _createSolutionCheckBox.Checked
                ? ValidateFileName(
                    string.IsNullOrWhiteSpace(_solutionNameText.Text) ? projectName : _solutionNameText.Text,
                    "solution name")
                : projectName;
            string location = _locationText.Text.Trim();
            if (string.IsNullOrWhiteSpace(location))
                throw new InvalidOperationException("Select a project location.");

            location = Path.GetFullPath(location);
            string projectDirectory = Path.Combine(location, projectName);
            string projectPath = Path.Combine(projectDirectory, projectName + ".csproj");
            string starterPath = Path.Combine(projectDirectory, SelectedTemplate.StarterFileName);
            string solutionPath = _createSolutionCheckBox.Checked
                ? Path.Combine(projectDirectory, solutionName + ".sln")
                : string.Empty;

            if (Directory.Exists(projectDirectory) &&
                Directory.EnumerateFileSystemEntries(projectDirectory).Any())
            {
                throw new InvalidOperationException("The project folder already exists and is not empty.");
            }

            if (!string.IsNullOrEmpty(solutionPath) && File.Exists(solutionPath))
                throw new InvalidOperationException("A solution file with that name already exists.");

            Directory.CreateDirectory(projectDirectory);

            string targetFramework = NormalizeTargetFramework(_targetFrameworkCombo.Text, SelectedTemplate);
            string rootNamespace = ToSafeNamespace(projectName);

            File.WriteAllText(projectPath, SelectedTemplate.BuildProjectFile(targetFramework), Encoding.UTF8);
            File.WriteAllText(starterPath, SelectedTemplate.BuildStarterFile(projectName, rootNamespace), Encoding.UTF8);

            if (!string.IsNullOrEmpty(solutionPath))
                File.WriteAllText(solutionPath, BuildSolutionFile(projectName, projectPath, projectDirectory),
                    Encoding.UTF8);

            return new NewProjectResult
            {
                WorkspacePath = projectDirectory,
                ProjectFilePath = projectPath,
                SolutionFilePath = solutionPath,
                StarterFilePath = starterPath
            };
        }

        private static string ValidateFileName(string value, string fieldName)
        {
            string name = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name))
                throw new InvalidOperationException($"Enter a {fieldName}.");

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidOperationException($"The {fieldName} contains characters that cannot be used in a file name.");

            return name;
        }

        private static string NormalizeTargetFramework(string targetFramework, ProjectTemplate template)
        {
            targetFramework = string.IsNullOrWhiteSpace(targetFramework) ? "net8.0" : targetFramework.Trim();
            if (template.RequiresWindowsTargetFramework &&
                !targetFramework.EndsWith("-windows", StringComparison.OrdinalIgnoreCase))
            {
                targetFramework += "-windows";
            }

            return targetFramework;
        }

        private static string GetDefaultProjectLocation()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrWhiteSpace(documents))
                documents = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            return Path.Combine(documents, "CIARE Projects");
        }

        private static string ToSafeNamespace(string projectName)
        {
            var builder = new StringBuilder();
            foreach (char ch in projectName)
                builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');

            if (builder.Length == 0 || char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            return builder.ToString();
        }

        private static string BuildSolutionFile(string projectName, string projectPath, string solutionDirectory)
        {
            string projectGuid = "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";
            string relativeProjectPath = Path.GetRelativePath(solutionDirectory, projectPath);

            return
                "Microsoft Visual Studio Solution File, Format Version 12.00\r\n" +
                "# Visual Studio Version 17\r\n" +
                "VisualStudioVersion = 17.0.31903.59\r\n" +
                "MinimumVisualStudioVersion = 10.0.40219.1\r\n" +
                $"Project(\"{CSharpProjectTypeGuid}\") = \"{projectName}\", \"{relativeProjectPath}\", \"{projectGuid}\"\r\n" +
                "EndProject\r\n" +
                "Global\r\n" +
                "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\r\n" +
                "\t\tDebug|Any CPU = Debug|Any CPU\r\n" +
                "\t\tRelease|Any CPU = Release|Any CPU\r\n" +
                "\tEndGlobalSection\r\n" +
                "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n" +
                $"\t\t{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\r\n" +
                $"\t\t{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU\r\n" +
                $"\t\t{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU\r\n" +
                $"\t\t{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU\r\n" +
                "\tEndGlobalSection\r\n" +
                "\tGlobalSection(SolutionProperties) = preSolution\r\n" +
                "\t\tHideSolutionNode = FALSE\r\n" +
                "\tEndGlobalSection\r\n" +
                "EndGlobal\r\n";
        }

        private sealed class ProjectTemplate
        {
            public static readonly List<ProjectTemplate> All = new List<ProjectTemplate>
            {
                new ProjectTemplate(
                    "Console App",
                    "Program.cs",
                    false,
                    targetFramework =>
                        "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
                        "  <PropertyGroup>\r\n" +
                        "    <OutputType>Exe</OutputType>\r\n" +
                        $"    <TargetFramework>{targetFramework}</TargetFramework>\r\n" +
                        "    <ImplicitUsings>enable</ImplicitUsings>\r\n" +
                        "    <Nullable>enable</Nullable>\r\n" +
                        "  </PropertyGroup>\r\n" +
                        "</Project>\r\n",
                    (projectName, rootNamespace) =>
                        "Console.WriteLine(\"Hello, World!\");\r\n"),

                new ProjectTemplate(
                    "Class Library",
                    "Class1.cs",
                    false,
                    targetFramework =>
                        "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
                        "  <PropertyGroup>\r\n" +
                        $"    <TargetFramework>{targetFramework}</TargetFramework>\r\n" +
                        "    <ImplicitUsings>enable</ImplicitUsings>\r\n" +
                        "    <Nullable>enable</Nullable>\r\n" +
                        "  </PropertyGroup>\r\n" +
                        "</Project>\r\n",
                    (projectName, rootNamespace) =>
                        $"namespace {rootNamespace};\r\n\r\n" +
                        "public class Class1\r\n" +
                        "{\r\n" +
                        "}\r\n"),

                new ProjectTemplate(
                    "Windows Forms App",
                    "Program.cs",
                    true,
                    targetFramework =>
                        "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n" +
                        "  <PropertyGroup>\r\n" +
                        "    <OutputType>WinExe</OutputType>\r\n" +
                        $"    <TargetFramework>{targetFramework}</TargetFramework>\r\n" +
                        "    <UseWindowsForms>true</UseWindowsForms>\r\n" +
                        "    <ImplicitUsings>enable</ImplicitUsings>\r\n" +
                        "    <Nullable>enable</Nullable>\r\n" +
                        "  </PropertyGroup>\r\n" +
                        "</Project>\r\n",
                    (projectName, rootNamespace) =>
                        "using System;\r\n" +
                        "using System.Drawing;\r\n" +
                        "using System.Windows.Forms;\r\n\r\n" +
                        $"namespace {rootNamespace};\r\n\r\n" +
                        "internal static class Program\r\n" +
                        "{\r\n" +
                        "    [STAThread]\r\n" +
                        "    private static void Main()\r\n" +
                        "    {\r\n" +
                        "        Application.Run(new MainForm());\r\n" +
                        "    }\r\n" +
                        "}\r\n\r\n" +
                        "public sealed class MainForm : Form\r\n" +
                        "{\r\n" +
                        "    public MainForm()\r\n" +
                        "    {\r\n" +
                        $"        Text = \"{projectName}\";\r\n" +
                        "        ClientSize = new Size(800, 450);\r\n" +
                        "        Controls.Add(new Label\r\n" +
                        "        {\r\n" +
                        "            AutoSize = true,\r\n" +
                        "            Location = new Point(24, 24),\r\n" +
                        "            Text = \"Hello, Windows Forms!\"\r\n" +
                        "        });\r\n" +
                        "    }\r\n" +
                        "}\r\n")
            };

            private readonly Func<string, string> _projectFileFactory;
            private readonly Func<string, string, string> _starterFileFactory;

            private ProjectTemplate(
                string name,
                string starterFileName,
                bool requiresWindowsTargetFramework,
                Func<string, string> projectFileFactory,
                Func<string, string, string> starterFileFactory)
            {
                Name = name;
                StarterFileName = starterFileName;
                RequiresWindowsTargetFramework = requiresWindowsTargetFramework;
                _projectFileFactory = projectFileFactory;
                _starterFileFactory = starterFileFactory;
            }

            private string Name { get; }
            public string StarterFileName { get; }
            public bool RequiresWindowsTargetFramework { get; }

            public string BuildProjectFile(string targetFramework) => _projectFileFactory(targetFramework);

            public string BuildStarterFile(string projectName, string rootNamespace) =>
                _starterFileFactory(projectName, rootNamespace);

            public override string ToString() => Name;
        }
    }
}
