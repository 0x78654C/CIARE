using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CIARE.GUI;
using CIARE.Utils;
using CIARE.Utils.Options;

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
        private static readonly string[] SolutionConfigurations = { "Debug", "Release" };
        private static readonly string[] SolutionPlatforms = { "Any CPU", "x64", "x86" };

        private readonly ListBox _templateList = new ListBox();
        private readonly TextBox _projectNameText = new TextBox();
        private readonly TextBox _locationText = new TextBox();
        private readonly TextBox _solutionNameText = new TextBox();
        private readonly ComboBox _targetFrameworkCombo = new ComboBox();
        private readonly CheckBox _createSolutionCheckBox = new CheckBox();
        private readonly Button _createButton = new Button();
        private readonly Button _cancelButton = new Button();
        private readonly Button _browseButton = new Button();
        private readonly string _existingSolutionPath;
        private bool _syncingSolutionName;
        private bool _syncingTargetFramework;
        private string _lastProjectName = "NewApp";
        private string _lastInstalledTargetFramework = "net8.0";

        public NewProjectResult CreatedProject { get; private set; }

        public NewProject()
        {
            InitializeDialog();
        }

        public NewProject(string existingSolutionPath)
        {
            _existingSolutionPath = existingSolutionPath;
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
            _projectNameText.Text = "NewApp";
            _projectNameText.TextChanged += ProjectNameText_TextChanged;

            _locationText.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            _locationText.Margin = new Padding(2, 4, 6, 0);
            _locationText.Text = GetDefaultProjectLocation();

            _browseButton.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            _browseButton.Height = _locationText.PreferredHeight;
            _browseButton.Margin = new Padding(0, 3, 0, 0);
            _browseButton.Text = "Browse";
            _browseButton.Click += BrowseButton_Click;

            var locationPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(1, 0, 0, 0),
                Padding = Padding.Empty,
                RowCount = 1
            };
            locationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            locationPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            locationPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            locationPanel.Controls.Add(_locationText, 0, 0);
            locationPanel.Controls.Add(_browseButton, 1, 0);

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
            _lastInstalledTargetFramework = _targetFrameworkCombo.Text;
            ValidateSelectedTargetFrameworkInstalled(showMessage: false);
            _targetFrameworkCombo.SelectedIndexChanged += TargetFrameworkCombo_SelectedIndexChanged;

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
            AddOptionRow(optionsLayout, 1, "Location:", locationPanel);
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
            ConfigureExistingSolutionMode();
            UpdateTemplateSelection();
        }

        private void ConfigureExistingSolutionMode()
        {
            if (!IsAddingToExistingSolution)
                return;

            string solutionDirectory = Path.GetDirectoryName(_existingSolutionPath);
            Text = "Add New Project";
            _createButton.Text = "Add";
            _locationText.Text = solutionDirectory ?? string.Empty;
            _locationText.ReadOnly = true;
            _browseButton.Enabled = false;
            _solutionNameText.Text = Path.GetFileNameWithoutExtension(_existingSolutionPath);
            _solutionNameText.ReadOnly = true;
            _createSolutionCheckBox.Checked = true;
            _createSolutionCheckBox.Enabled = false;
            _createSolutionCheckBox.Text = "Add to existing solution (.sln)";
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
            layout.SetColumnSpan(control, 2);
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

        private void TargetFrameworkCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_syncingTargetFramework)
                return;

            UpdateTemplateSelection();
            ValidateSelectedTargetFrameworkInstalled(showMessage: true);
        }

        private void SelectTargetFramework(string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework))
                targetFramework = "net8.0";

            _syncingTargetFramework = true;
            try
            {
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
            finally
            {
                _syncingTargetFramework = false;
            }
        }

        private ProjectTemplate SelectedTemplate =>
            _templateList.SelectedItem as ProjectTemplate ?? ProjectTemplate.All[0];

        private bool IsAddingToExistingSolution =>
            !string.IsNullOrWhiteSpace(_existingSolutionPath) &&
            File.Exists(_existingSolutionPath) &&
            string.Equals(Path.GetExtension(_existingSolutionPath), ".sln", StringComparison.OrdinalIgnoreCase);

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
            bool addToExistingSolution = IsAddingToExistingSolution;
            bool createNewSolution = !addToExistingSolution && _createSolutionCheckBox.Checked;
            string solutionName = createNewSolution
                ? ValidateFileName(
                    string.IsNullOrWhiteSpace(_solutionNameText.Text) ? projectName : _solutionNameText.Text,
                    "solution name")
                : projectName;
            string location = _locationText.Text.Trim();
            if (string.IsNullOrWhiteSpace(location))
                throw new InvalidOperationException("Select a project location.");

            location = Path.GetFullPath(location);
            string solutionDirectory = addToExistingSolution
                ? Path.GetDirectoryName(Path.GetFullPath(_existingSolutionPath))
                : createNewSolution
                    ? Path.Combine(location, solutionName)
                    : string.Empty;
            if ((addToExistingSolution || createNewSolution) && string.IsNullOrWhiteSpace(solutionDirectory))
                throw new InvalidOperationException("The solution directory was not found.");

            string projectParentDirectory = addToExistingSolution || createNewSolution
                ? solutionDirectory
                : location;
            string projectDirectory = Path.Combine(projectParentDirectory, projectName);
            string projectPath = Path.Combine(projectDirectory, projectName + ".csproj");
            string starterPath = Path.Combine(projectDirectory, SelectedTemplate.StarterFileName);
            string solutionPath = addToExistingSolution
                ? Path.GetFullPath(_existingSolutionPath)
                : createNewSolution
                    ? Path.Combine(solutionDirectory, solutionName + ".sln")
                    : string.Empty;

            if (createNewSolution &&
                Directory.Exists(solutionDirectory) &&
                Directory.EnumerateFileSystemEntries(solutionDirectory).Any())
            {
                throw new InvalidOperationException("The solution folder already exists and is not empty.");
            }

            if (Directory.Exists(projectDirectory) &&
                Directory.EnumerateFileSystemEntries(projectDirectory).Any())
            {
                throw new InvalidOperationException("The project folder already exists and is not empty.");
            }

            if (createNewSolution && File.Exists(solutionPath))
                throw new InvalidOperationException("A solution file with that name already exists.");

            Directory.CreateDirectory(projectDirectory);

            string targetFramework = NormalizeTargetFramework(_targetFrameworkCombo.Text, SelectedTemplate);
            if (!IsTargetFrameworkInstalled(targetFramework))
                throw new InvalidOperationException(
                    $"The targeted framework ({GetTargetFrameworkDisplayName(targetFramework)}) is not installed.");

            string rootNamespace = ToSafeNamespace(projectName);

            File.WriteAllText(projectPath, SelectedTemplate.BuildProjectFile(targetFramework), Encoding.UTF8);
            File.WriteAllText(starterPath, SelectedTemplate.BuildStarterFile(projectName, rootNamespace), Encoding.UTF8);

            if (addToExistingSolution)
                AddProjectToSolution(solutionPath, projectName, projectPath);
            else if (createNewSolution)
                File.WriteAllText(solutionPath, BuildSolutionFile(projectName, projectPath, solutionDirectory),
                    Encoding.UTF8);

            return new NewProjectResult
            {
                WorkspacePath = addToExistingSolution || createNewSolution ? solutionDirectory : projectDirectory,
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

        private bool ValidateSelectedTargetFrameworkInstalled(bool showMessage)
        {
            string targetFramework = NormalizeTargetFramework(_targetFrameworkCombo.Text, SelectedTemplate);
            if (IsTargetFrameworkInstalled(targetFramework))
            {
                _lastInstalledTargetFramework = targetFramework;
                return true;
            }

            if (showMessage)
            {
                MessageBox.Show(
                    $"The targeted framework ({GetTargetFrameworkDisplayName(targetFramework)}) is not installed!",
                    "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            string fallback = NormalizeTargetFramework(_lastInstalledTargetFramework, SelectedTemplate);
            if (!IsTargetFrameworkInstalled(fallback))
                fallback = GetFirstInstalledTargetFramework(SelectedTemplate) ?? "net8.0";

            SelectTargetFramework(fallback);
            if (IsTargetFrameworkInstalled(fallback))
                _lastInstalledTargetFramework = fallback;

            return false;
        }

        private static string GetFirstInstalledTargetFramework(ProjectTemplate template)
        {
            foreach (string targetFramework in new[]
            {
                "net10.0",
                "net9.0",
                "net8.0",
                "net7.0",
                "net6.0"
            })
            {
                string normalizedTargetFramework = NormalizeTargetFramework(targetFramework, template);
                if (IsTargetFrameworkInstalled(normalizedTargetFramework))
                    return normalizedTargetFramework;
            }

            return null;
        }

        private static bool IsTargetFrameworkInstalled(string targetFramework)
        {
            string sdkVersion = GetTargetFrameworkSdkVersion(targetFramework);
            return !string.IsNullOrEmpty(sdkVersion) && SdkVersion.CheckSdk(sdkVersion);
        }

        private static string GetTargetFrameworkSdkVersion(string targetFramework)
        {
            if (string.IsNullOrWhiteSpace(targetFramework) ||
                !targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            string version = targetFramework.Substring(3);
            int separatorIndex = version.IndexOf('.');
            return separatorIndex > 0 ? version.Substring(0, separatorIndex) : version;
        }

        private static string GetTargetFrameworkDisplayName(string targetFramework)
        {
            string sdkVersion = GetTargetFrameworkSdkVersion(targetFramework);
            string windowsSuffix = targetFramework.EndsWith("-windows", StringComparison.OrdinalIgnoreCase)
                ? " Windows"
                : string.Empty;

            return string.IsNullOrEmpty(sdkVersion)
                ? targetFramework
                : $".NET {sdkVersion}{windowsSuffix}";
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

            var builder = new StringBuilder();
            builder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            builder.AppendLine("# Visual Studio Version 17");
            builder.AppendLine("VisualStudioVersion = 17.0.31903.59");
            builder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
            builder.AppendLine($"Project(\"{CSharpProjectTypeGuid}\") = \"{projectName}\", \"{relativeProjectPath}\", \"{projectGuid}\"");
            builder.AppendLine("EndProject");
            builder.AppendLine("Global");
            builder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            foreach (string configuration in SolutionConfigurations)
            {
                foreach (string platform in SolutionPlatforms)
                    builder.AppendLine($"\t\t{configuration}|{platform} = {configuration}|{platform}");
            }

            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (string configuration in SolutionConfigurations)
            {
                foreach (string platform in SolutionPlatforms)
                {
                    builder.AppendLine($"\t\t{projectGuid}.{configuration}|{platform}.ActiveCfg = {configuration}|{platform}");
                    builder.AppendLine($"\t\t{projectGuid}.{configuration}|{platform}.Build.0 = {configuration}|{platform}");
                }
            }

            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
            builder.AppendLine("\t\tHideSolutionNode = FALSE");
            builder.AppendLine("\tEndGlobalSection");
            builder.AppendLine("EndGlobal");
            return builder.ToString();
        }

        public static void AddProjectToSolution(string solutionPath, string projectName, string projectPath)
        {
            if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
                throw new InvalidOperationException("The solution file was not found.");
            if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
                throw new InvalidOperationException("The project file was not found.");

            string solutionDirectory = Path.GetDirectoryName(solutionPath);
            if (string.IsNullOrWhiteSpace(solutionDirectory))
                throw new InvalidOperationException("The solution directory was not found.");

            string solutionContent = File.ReadAllText(solutionPath);
            string newLine = solutionContent.Contains("\r\n") ? "\r\n" : Environment.NewLine;
            var lines = solutionContent
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .ToList();

            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
                lines.RemoveAt(lines.Count - 1);

            if (SolutionContainsProject(lines, solutionDirectory, projectPath))
                throw new InvalidOperationException("The project is already included in the solution.");

            int globalIndex = FindGlobalLineIndex(lines);
            if (globalIndex < 0)
            {
                lines.Add("Global");
                lines.Add("EndGlobal");
                globalIndex = lines.Count - 2;
            }

            string projectGuid = "{" + Guid.NewGuid().ToString().ToUpperInvariant() + "}";
            string relativeProjectPath = Path.GetRelativePath(solutionDirectory, projectPath)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            lines.InsertRange(globalIndex, new[]
            {
                $"Project(\"{CSharpProjectTypeGuid}\") = \"{projectName}\", \"{relativeProjectPath}\", \"{projectGuid}\"",
                "EndProject"
            });

            var configurationPlatforms = ReadSolutionConfigurationPlatforms(lines).ToList();
            if (configurationPlatforms.Count == 0)
            {
                configurationPlatforms = GetDefaultSolutionConfigurationPlatforms().ToList();
                InsertSolutionConfigurationPlatforms(lines, configurationPlatforms);
            }

            InsertProjectConfigurationPlatforms(lines, projectGuid, configurationPlatforms);
            File.WriteAllText(solutionPath, string.Join(newLine, lines) + newLine, Encoding.UTF8);
        }

        private static bool SolutionContainsProject(IEnumerable<string> lines, string solutionDirectory,
            string projectPath)
        {
            string normalizedProjectPath = NormalizeSolutionPath(projectPath);
            foreach (string line in lines)
            {
                Match match = Regex.Match(line,
                    @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success)
                    continue;

                string existingProjectPath = match.Groups[1].Value;
                if (!Path.IsPathRooted(existingProjectPath))
                    existingProjectPath = Path.GetFullPath(Path.Combine(solutionDirectory, existingProjectPath));

                if (string.Equals(NormalizeSolutionPath(existingProjectPath), normalizedProjectPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> GetDefaultSolutionConfigurationPlatforms()
        {
            foreach (string configuration in SolutionConfigurations)
            {
                foreach (string platform in SolutionPlatforms)
                    yield return $"{configuration}|{platform}";
            }
        }

        private static IEnumerable<string> ReadSolutionConfigurationPlatforms(List<string> lines)
        {
            int sectionIndex = FindGlobalSectionIndex(lines, "SolutionConfigurationPlatforms");
            if (sectionIndex < 0)
                yield break;

            int endIndex = FindEndGlobalSectionIndex(lines, sectionIndex);
            if (endIndex < 0)
                yield break;

            for (int i = sectionIndex + 1; i < endIndex; i++)
            {
                string line = lines[i].Trim();
                int equalsIndex = line.IndexOf('=');
                string configurationPlatform = (equalsIndex >= 0 ? line.Substring(0, equalsIndex) : line).Trim();
                if (configurationPlatform.Contains("|"))
                    yield return configurationPlatform;
            }
        }

        private static void InsertSolutionConfigurationPlatforms(List<string> lines,
            IEnumerable<string> configurationPlatforms)
        {
            int globalIndex = FindGlobalLineIndex(lines);
            int insertIndex = globalIndex >= 0 ? globalIndex + 1 : lines.Count;
            var sectionLines = new List<string> { "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution" };
            foreach (string configurationPlatform in configurationPlatforms)
                sectionLines.Add($"\t\t{configurationPlatform} = {configurationPlatform}");
            sectionLines.Add("\tEndGlobalSection");
            lines.InsertRange(insertIndex, sectionLines);
        }

        private static void InsertProjectConfigurationPlatforms(List<string> lines, string projectGuid,
            IEnumerable<string> configurationPlatforms)
        {
            var entries = new List<string>();
            foreach (string configurationPlatform in configurationPlatforms.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string projectConfigurationPlatform = GetProjectConfigurationPlatform(configurationPlatform);
                entries.Add($"\t\t{projectGuid}.{configurationPlatform}.ActiveCfg = {projectConfigurationPlatform}");
                entries.Add($"\t\t{projectGuid}.{configurationPlatform}.Build.0 = {projectConfigurationPlatform}");
            }

            int sectionIndex = FindGlobalSectionIndex(lines, "ProjectConfigurationPlatforms");
            if (sectionIndex >= 0)
            {
                int endIndex = FindEndGlobalSectionIndex(lines, sectionIndex);
                if (endIndex >= 0)
                {
                    lines.InsertRange(endIndex, entries);
                    return;
                }
            }

            int solutionConfigurationIndex = FindGlobalSectionIndex(lines, "SolutionConfigurationPlatforms");
            int insertIndex = FindEndGlobalLineIndex(lines);
            if (solutionConfigurationIndex >= 0)
            {
                int solutionConfigurationEndIndex = FindEndGlobalSectionIndex(lines, solutionConfigurationIndex);
                if (solutionConfigurationEndIndex >= 0)
                    insertIndex = solutionConfigurationEndIndex + 1;
            }

            var sectionLines = new List<string> { "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution" };
            sectionLines.AddRange(entries);
            sectionLines.Add("\tEndGlobalSection");
            lines.InsertRange(insertIndex, sectionLines);
        }

        private static string GetProjectConfigurationPlatform(string solutionConfigurationPlatform)
        {
            int separatorIndex = solutionConfigurationPlatform.IndexOf('|');
            if (separatorIndex <= 0 || separatorIndex >= solutionConfigurationPlatform.Length - 1)
                return solutionConfigurationPlatform;

            string configuration = solutionConfigurationPlatform.Substring(0, separatorIndex).Trim();
            string platform = solutionConfigurationPlatform.Substring(separatorIndex + 1).Trim();
            if (string.Equals(platform, "Any CPU", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platform, "x64", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platform, "x86", StringComparison.OrdinalIgnoreCase))
            {
                return configuration + "|" + platform;
            }

            return configuration + "|Any CPU";
        }

        private static int FindGlobalLineIndex(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (string.Equals(lines[i].Trim(), "Global", StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static int FindEndGlobalLineIndex(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (string.Equals(lines[i].Trim(), "EndGlobal", StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return lines.Count;
        }

        private static int FindGlobalSectionIndex(List<string> lines, string sectionName)
        {
            string prefix = "GlobalSection(" + sectionName + ")";
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static int FindEndGlobalSectionIndex(List<string> lines, int sectionIndex)
        {
            for (int i = sectionIndex + 1; i < lines.Count; i++)
            {
                if (string.Equals(lines[i].Trim(), "EndGlobalSection", StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private static string NormalizeSolutionPath(string path)
        {
            try
            {
                return Path.GetFullPath(path)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path ?? string.Empty;
            }
        }

        private static string BuildSdkProjectFile(string targetFramework, string outputType = null, bool useWindowsForms = false)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?><Project Sdk=\"Microsoft.NET.Sdk\">");
            builder.AppendLine("  <PropertyGroup>");
            if (!string.IsNullOrWhiteSpace(outputType))
                builder.AppendLine($"    <OutputType>{outputType}</OutputType>");
            builder.AppendLine($"    <TargetFramework>{targetFramework}</TargetFramework>");
            if (useWindowsForms)
                builder.AppendLine("    <UseWindowsForms>true</UseWindowsForms>");
            builder.AppendLine("    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>");
            builder.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
            builder.AppendLine("    <Nullable>enable</Nullable>");
            builder.AppendLine("    <DefaultItemExcludes>$(DefaultItemExcludes);**\\bin\\**;**\\obj\\**</DefaultItemExcludes>");
            builder.AppendLine("    <Platforms>AnyCPU;x64;x86</Platforms>");
            builder.AppendLine("  </PropertyGroup>");
            builder.AppendLine("  <PropertyGroup Condition=\"'$(Platform)' == 'x64'\">");
            builder.AppendLine("    <PlatformTarget>x64</PlatformTarget>");
            builder.AppendLine("  </PropertyGroup>");
            builder.AppendLine("  <PropertyGroup Condition=\"'$(Platform)' == 'x86'\">");
            builder.AppendLine("    <PlatformTarget>x86</PlatformTarget>");
            builder.AppendLine("  </PropertyGroup>");
            builder.AppendLine("</Project>");
            return builder.ToString();
        }

        private sealed class ProjectTemplate
        {
            public static readonly List<ProjectTemplate> All = new List<ProjectTemplate>
            {
                new ProjectTemplate(
                    "Console App",
                    "Program.cs",
                    false,
                    targetFramework => BuildSdkProjectFile(targetFramework, "Exe"),
                    (projectName, rootNamespace) =>
                        "Console.WriteLine(\"Hello, World!\");\r\n"),

                new ProjectTemplate(
                    "Class Library",
                    "Class1.cs",
                    false,
                    targetFramework => BuildSdkProjectFile(targetFramework),
                    (projectName, rootNamespace) =>
                        $"namespace {rootNamespace};\r\n\r\n" +
                        "public class Class1\r\n" +
                        "{\r\n" +
                        "}\r\n"),

                new ProjectTemplate(
                    "Windows Forms App",
                    "Program.cs",
                    true,
                    targetFramework => BuildSdkProjectFile(targetFramework, "WinExe", useWindowsForms: true),
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
