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
        private List<string> netFrameworks = new List<string>();
        private int s_initialSizeForm = 0;
        private string s_packageName { get; set; }
        public NuGetSearch()
        {
            InitializeComponent();
        }

        private void NuGetSearch_Load(object sender, EventArgs e)
        {
            // Get initial width size of form.
            s_initialSizeForm = this.Size.Width;

            // Check if can access NuGet API website.
            Network network = new Network(GlobalVariables.nugetApiAddress);
            if (!network.PingHost())
            {
                MessageBox.Show($"NuGet API cannot be reached. Check your internet connection!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                this.Close();
            }

            // TEST: till find solution for use .net7 in assambly load.
            // Check framework target and add the specified list with it.
            if (GlobalVariables.Framework.Contains(@"net7.0"))
                netFrameworks = netFrameworksNet7;
            else if (GlobalVariables.Framework.Contains(@"net8.0"))
                netFrameworks = netFrameworksNet8;
            else
                netFrameworks = netFrameworksNet6;

            // Set dark mode if enabled.
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);

            // Set water marg on search textbox.
            WaterMark.TextBoxWaterMark(SearchBox, "Enter package name...");

            downloadBar.Style = ProgressBarStyle.Marquee;
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
                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
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
            var packageName = nugetListView.SelectedItems[0].Text;
            if (GlobalVariables.customRefAsm.Any(x => x.Contains(packageName)))
            {
                MessageBox.Show($"NuGet package {packageName} is already downloaded and added to reference!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
                return;
            }
          var dialog = MessageBox.Show($"If the NuGet package contains dependencies it will be added to reference list!\nDo you want to download the package?", "CIARE", MessageBoxButtons.YesNo,
MessageBoxIcon.Information);

            if (dialog == DialogResult.No)
                return;

            s_packageName = packageName;

            HideControlers();

            Task.Run(() => Download(s_packageName));
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

            NuGetDownloader nuGetDownloader = new NuGetDownloader(packageName, GlobalVariables.nugetApi, netFrameworks);
            nuGetDownloader.DownloadPackage();

            // Repopulate listview with ref. after loading list.
            CustomRef.PopulateList(GlobalVariables.customRefAsm, RefManager.Instance.refListView);

            // Load assemblies from list.
            CustomRef.SetCustomRefDirective(GlobalVariables.customRefAsm);

            // Delete downloaded package.
            DelDownloadedPackage(GlobalVariables.downloadNugetPath);

            // Sow controlers after download.
            ShowControler();
        }

        /// <summary>
        /// Function for delete zip file from nuget forlder.
        /// </summary>
        /// <param name="pathNugetDir"></param>
        private void DelDownloadedPackage(string pathNugetDir)
        {
            Thread.Sleep(3000);
            try
            {
                if (!Directory.Exists(pathNugetDir)) return;

                var files = Directory.GetFiles(pathNugetDir);
                foreach (var file in files)
                {
                    if (file.EndsWith(".zip"))
                        File.Delete(file);
                }
            }
            catch { }
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
