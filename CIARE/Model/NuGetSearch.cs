using CIARE.GUI;
using CIARE.Reference;
using CIARE.Utils;
using CIARE.Utils.NuGet;
using CIARE.Utils.NuGetManage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Model
{
    [SupportedOSPlatform("windows")]

    public partial class NuGetSearch : Form
    {
        private List<string> netFrameworks = new List<string>() { "net7.0", "net6.0", "net5.0", "netstandard2.1", "netstandard2.0", "netstandard1.6", "netstandard1.5", "netstandard1.4", "netstandard1.3", "netstandard1.2", "netstandard1.1", "netstandard1.0", "net481", "net48", "net472", "net471", "net47", "net462", "net461", "net46", "net452", "net451", "net45", "net40", "net35", "net30", "net20" };

        public NuGetSearch()
        {
            InitializeComponent();
        }

        private void NuGetSearch_Load(object sender, EventArgs e)
        {
            // Set dark mode if enabled.
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);

            // Set water marg on search textbox.
            WaterMark.TextBoxWaterMark(SearchBox, "Enter package name...");
        }

        /// <summary>
        /// Method for get name of package, version and description
        /// </summary>
        private void GetNuGetSearhed(string packageName, string nugetApi, ListView refList)
        {
            NuGetSearcher nSearcher = new NuGetSearcher(packageName, nugetApi);
            Task.Run(() => nSearcher.Search()).Wait();
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
        /// Add NuGet packages libs to reference list event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addToReference_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"If the NuGet package contains dependencies it will be added to reference list!", "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Warning);
            var packageName = packageList.SelectedItems[0].Text;
            NuGetDownloader nuGetDownloader = new NuGetDownloader(packageName,GlobalVariables.nugetApi);
            nuGetDownloader.Extract(netFrameworks);
      
            // Repopulate listview with ref. after loading list.
            CustomRef.PopulateList(GlobalVariables.customRefAsm, ref RefManager.Instance.refListView);

            // Load assemblies from list.
            CustomRef.SetCustomRefDirective(GlobalVariables.customRefAsm, MainForm.Instance.outputRBT);
            GlobalVariables.depNugetFiles.Clear();

        }
    }
}
