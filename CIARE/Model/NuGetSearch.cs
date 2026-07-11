using CIARE.GUI;
using CIARE.Reference;
using CIARE.Utils;
using CIARE.Utils.NuGet;
using CIARE.Utils.NuGetManage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Model
{
    [SupportedOSPlatform("windows")]

    public partial class NuGetSearch : Form
    {
        private static List<string> netFrameworksNet6 { get; } = new List<string>() { "net6.0", "net5.0", "netcoreapp3.1", "netcoreapp3.0", "netcoreapp2.2", "netcoreapp2.1", "netcoreapp2.0", "netcoreapp1.1", "netcoreapp1.0", "netstandard2.1", "netstandard2.0", "netstandard1.6", "netstandard1.5", "netstandard1.4", "netstandard1.3", "netstandard1.2", "netstandard1.1", "netstandard1.0", "netstandard1.6", "netstandard1.5", "netstandard1.4", "netstandard1.3", "netstandard1.2", "netstandard1.1", "netstandard1.0", "net481", "net48", "net472", "net471", "net47", "net462", "net461", "net46", "net452", "net451", "net45", "net40", "net35", "net30", "net20" };
        private static List<string> netFrameworksNet7 { get; } = new[] { "net7.0" }.Concat(netFrameworksNet6).ToList(); //For future tests
        private static List<string> netFrameworksNet8 { get; } = new[] { "net8.0" }.Concat(netFrameworksNet7).ToList(); //For future tests
        private static List<string> netFrameworksNet9 { get; } = new[] { "net9.0" }.Concat(netFrameworksNet8).ToList();
        private static List<string> netFrameworksNet10 { get; } = new[] { "net10.0" }.Concat(netFrameworksNet9).ToList();
        private List<string> netFrameworks = new List<string>();
        private int s_initialSizeForm = 0;
        private string s_packageName { get; set; }
        private readonly string _projectPackageProjectPath;
        public NuGetSearch()
            : this(string.Empty)
        {
        }

        public NuGetSearch(string projectPackageProjectPath)
        {
            _projectPackageProjectPath = File.Exists(projectPackageProjectPath) &&
                string.Equals(Path.GetExtension(projectPackageProjectPath), ".csproj", StringComparison.OrdinalIgnoreCase)
                    ? projectPackageProjectPath
                    : string.Empty;
            InitializeComponent();
        }

        private void NuGetSearch_Load(object sender, EventArgs e)
        {
            // Get initial width size of form.
            s_initialSizeForm = this.Size.Width;

            // Check if can access NuGet API website.
            Network network = new Network(GlobalVariables.nugetApi);
            bool isApiUp = network.IsWebResponding();
            if (!isApiUp)
            {
                MessageBox.Show($"NuGet API cannot be reached. Check your internet connection!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                this.Close();
            }

            netFrameworks = GetStandaloneNuGetFrameworks(GlobalVariables.Framework);

            // Set dark mode if enabled.
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);

            // Set water marg on search textbox.
            WaterMark.TextBoxWaterMark(SearchBox, "Enter package name...");

            if (HasProjectPackageTarget())
            {
                Text = $"NuGet Package Manager - {Path.GetFileName(_projectPackageProjectPath)}";
                addToReference.Text = "Add PackageReference to Project";
            }
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
                case Keys.F10 | Keys.Shift:
                    var focusedItem = packageList.SelectedItems;
                    if (focusedItem.Count > 0)
                        AddNuGetPackageToRef(packageList);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Method for get name of package, version and description
        /// </summary>
        private void GetNuGetSearhed(string packageName, string nugetApi, ListView refList)
        {
            NuGetSearcher nSearcher = new NuGetSearcher(packageName, nugetApi);
            nSearcher.Search();
            refList.Items.Clear();
            foreach (var version in GlobalVariables.nugetPackage)
                PopulateList(ref packageList);
            GlobalVariables.nugetPackage.Clear();
        }

        /// <summary>
        /// Populate list with found results
        /// </summary>
        /// <param name="refList"></param>
        private void PopulateList(ref ListView refList)
        {
            foreach (var nug in GlobalVariables.nugetPackage)
            {
                var name = nug.SplitByText(" | ", 0);
                var version = nug.SplitByText(" | ", 1);
                var description = nug.SplitByText(" | ", 2);
                ListViewItem item = new ListViewItem(new[] { name, version, description });
                var foudItem = refList.FindItemWithText(name);
                if (foudItem == null)
                    refList.Items.Add(item);
            }
        }

        /// <summary>
        /// Run search on nuget manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBtn_Click(object sender, EventArgs e) => GetNuGetSearhed(SearchBox.Text, GlobalVariables.nugetApi, packageList);

        private static List<string> GetStandaloneNuGetFrameworks(string framework)
        {
            string normalized = (framework ?? string.Empty).Trim();
            string baseFramework = normalized.Split('-')[0];
            IEnumerable<string> frameworks;

            if (baseFramework.StartsWith("net10.0", StringComparison.OrdinalIgnoreCase))
                frameworks = netFrameworksNet10;
            else if (baseFramework.StartsWith("net9.0", StringComparison.OrdinalIgnoreCase))
                frameworks = netFrameworksNet9;
            else if (baseFramework.StartsWith("net8.0", StringComparison.OrdinalIgnoreCase))
                frameworks = netFrameworksNet8;
            else if (baseFramework.StartsWith("net7.0", StringComparison.OrdinalIgnoreCase))
                frameworks = netFrameworksNet7;
            else
                frameworks = netFrameworksNet6;

            if (!string.IsNullOrWhiteSpace(normalized) &&
                !string.Equals(normalized, baseFramework, StringComparison.OrdinalIgnoreCase))
            {
                frameworks = new[] { normalized }.Concat(frameworks);
            }

            return frameworks
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Open right click menu event on package list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packageList_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var focusedItem = packageList.FocusedItem;
                if (focusedItem != null)
                    ActionNugetMenu.Show(Cursor.Position);
            }
        }

        /// <summary>
        /// Set text to clipborad from reference list.
        /// </summary>
        /// <param name="refList"></param>
        private void CopyNamespace(ListView packageList) => Clipboard.SetText(packageList.SelectedItems[0].Text);


        /// <summary>
        /// Copy package name from list event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyPackageName_Click(object sender, EventArgs e) => CopyNamespace(packageList);

        /// <summary>
        /// Add NuGet packages libs to reference list event on right click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addToReference_Click(object sender, EventArgs e) => AddNuGetPackageToRef(packageList);


        /// <summary>
        /// Add nuget package to 
        /// </summary>
        /// <param name="nugetListView"></param>
        private void AddNuGetPackageToRef(ListView nugetListView)
        {
            if (nugetListView.SelectedItems.Count == 0)
                return;

            var packageName = nugetListView.SelectedItems[0].Text;
            var version = nugetListView.SelectedItems[0].SubItems[1].Text;

            if (HasProjectPackageTarget())
            {
                AddNuGetPackageToProject(packageName, version);
                return;
            }

            if (GlobalVariables.customRefAsm.Any(x => x.Contains(packageName)) ||
                GlobalVariables.nugetNames.Any(x => x.StartsWith(packageName + "|")))
            {
                RefreshExistingStandaloneNuGetPackage(packageName);
                MessageBox.Show($"NuGet package {packageName} is already downloaded and added to reference!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                return;
            }
            var dialog = MessageBox.Show($"If the NuGet package contains dependencies it will be added to reference list!\nDo you want to download the package {packageName}?", "CIARE", MessageBoxButtons.YesNo,
  MessageBoxIcon.Information);

            if (dialog == DialogResult.No)
                return;

            s_packageName = packageName;

            HideControlers();
            var nameVersion = $"{packageName}|{version}";
            if (!GlobalVariables.nugetNames.Contains(nameVersion))
                GlobalVariables.nugetNames.Add(nameVersion);

            Task.Run(() => Download(s_packageName));
        }

        private void RefreshExistingStandaloneNuGetPackage(string packageName)
        {
            if (Directory.Exists(GlobalVariables.downloadNugetPath))
                FileManage.SearchFile(GlobalVariables.downloadNugetPath, netFrameworks);

            if (GlobalVariables.customRefAsm.Count > 0)
                CompleteDownload(packageName);
            else
                MainForm.Instance?.RefreshStandaloneReferenceContext();
        }

        private bool HasProjectPackageTarget()
        {
            return !string.IsNullOrWhiteSpace(_projectPackageProjectPath) &&
                File.Exists(_projectPackageProjectPath);
        }

        private void AddNuGetPackageToProject(string packageName, string version)
        {
            if (ProjectNuGetManager.HasPackageReference(_projectPackageProjectPath, packageName))
            {
                MessageBox.Show($"NuGet package {packageName} is already installed in {Path.GetFileName(_projectPackageProjectPath)}.",
                    "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialog = MessageBox.Show(
                $"Do you want to add {packageName} {version} to {Path.GetFileName(_projectPackageProjectPath)}?",
                "CIARE", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (dialog == DialogResult.No)
                return;

            if (!ProjectNuGetManager.AddPackageReference(_projectPackageProjectPath, packageName, version, out string message))
            {
                MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MainForm.Instance?.RefreshProjectPackageContext(_projectPackageProjectPath, restoreProject: true);
            MessageBox.Show(message, "CIARE", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hide controlers and display progress bar.
        /// </summary>
        private void HideControlers()
        {
            downloadLbl.Visible = true;
            downloadBar.Visible = true;
            packageList.Visible = false;
            SearchBtn.Visible = false;
            SearchBox.Visible = false;
            ControlBox = false;
        }

        /// <summary>
        /// Show controlers and hide progress bar.
        /// </summary>
        private void ShowControler()
        {
            downloadLbl.Visible = false;
            downloadBar.Visible = false;
            packageList.Visible = true;
            SearchBtn.Visible = true;
            SearchBox.Visible = true;
            ControlBox = true;
        }

        private void Download(string packageName)
        {
            try
            {
                NuGetDownloader nuGetDownloader = new NuGetDownloader(packageName, GlobalVariables.nugetApi, netFrameworks);
                nuGetDownloader.DownloadPackage();

                // Delete downloaded package.
                CustomRef.DelDownloadedPackage(GlobalVariables.downloadNugetPath);

                RunOnUiThread(() => CompleteDownload(packageName));
            }
            catch (Exception ex)
            {
                RunOnUiThread(() => HandleDownloadFailure(packageName, ex));
            }
        }

        private void CompleteDownload(string packageName)
        {
            if (RefManager.Instance?.refListView != null)
                CustomRef.PopulateListNuget(GlobalVariables.nugetNames, RefManager.Instance.refListView);

            string pathNugetFile = Path.Combine(GlobalVariables.downloadNugetPath, packageName + ".ddb");
            try
            {
                if (File.Exists(pathNugetFile))
                    File.Delete(pathNugetFile);
            }
            catch
            {
            }

            // Repopulate listview with ref. after loading list.
            CustomRef.PopulateList(GlobalVariables.customRefAsm, pathNugetFile, false);

            // Load assemblies from list.
            CustomRef.SetCustomRefDirective(GlobalVariables.customRefAsm);

            MainForm.Instance?.RefreshStandaloneReferenceContext();

            // Sow controlers after download.
            ShowControler();
        }

        private void HandleDownloadFailure(string packageName, Exception ex)
        {
            GlobalVariables.nugetNames.RemoveAll(package =>
                package.StartsWith(packageName + "|", StringComparison.OrdinalIgnoreCase));
            ShowControler();
            MessageBox.Show(ex.Message, "NuGet Package Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void RunOnUiThread(Action action)
        {
            if (action == null || IsDisposed)
                return;

            try
            {
                if (InvokeRequired)
                    BeginInvoke(action);
                else
                    action();
            }
            catch (InvalidOperationException)
            {
            }
        }

        /// <summary>
        /// Resize column of description on form resize
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NuGetSearch_Resize(object sender, EventArgs e)
        {
            int changedSize = this.Size.Width - s_initialSizeForm;
            int descriptionSize = packageList.Columns[2].Width;
            packageList.Columns[2].Width = descriptionSize + changedSize;
        }

        /// <summary>
        /// Action nuget package add to reference on double click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void packageList_DoubleClick(object sender, EventArgs e) => AddNuGetPackageToRef(packageList);
    }
}
