using CIARE.GUI;
using CIARE.Reference;
using CIARE.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.Model
{
    [SupportedOSPlatform("Windows")]
    public partial class RefManager : Form
    {
        public RefManager()
        {
            InitializeComponent();
        }

        private void RefManager_Load(object sender, EventArgs e)
        {
            // Set dark mode if enabled.
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);

            // Populate listview with ref.
            PopulateList(GlobalVariables.customRefAsm, ref refListView);
        }

        /// <summary>
        /// Load reference assembly button control event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddRefFileBtn_Click(object sender, EventArgs e)
        {
            // Add lib's to list dialog.
            FileManage.AddReferenceDialog();

            // Repopulate listview with ref. after loading list.
            PopulateList(GlobalVariables.customRefAsm, ref refListView);

            // Load assemblies from list.
            CustomRef.SetCustomRefDirective(GlobalVariables.customRefAsm, MainForm.Instance.outputRBT);
        }

        /// <summary>
        /// Cancel current form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelBtn_Click(object sender, EventArgs e) => this.Close();

        /// <summary>
        /// Populate the listview with reference lib path and namespace.
        /// </summary>
        /// <param name="libPath"></param>
        /// <param name="refList"></param>
        private void PopulateList(List<string> libPath, ref ListView refList)
        {
            foreach (var lib in libPath)
            {
                string assemblyNamespace = CustomRef.GetAssemblyNamespace(lib);
                ListViewItem item = new ListViewItem(new[] { assemblyNamespace, lib });
                var foudItem = refList.FindItemWithText(assemblyNamespace);
                if (foudItem == null)
                    refList.Items.Add(item);
            }
        }

        /// <summary>
        /// Open delete menu for selected item on right click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var focusedItem = refListView.FocusedItem;
                if (focusedItem != null && focusedItem.Bounds.Contains(e.Location))
                    deleteStrip.Show(Cursor.Position);
            }
        }

        /// <summary>
        /// Delete reference library from lists.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteRefLibrary(refListView);
        }

        /// <summary>
        /// Remove reference library from list.
        /// </summary>
        /// <param name="refList"></param>
        private void DeleteRefLibrary(ListView refList)
        {
            var selecItem = refList.SelectedItems[0].Text;
            var dialogResult = MessageBox.Show($"You are about to remove {selecItem} reference. Are you sure? ", "CIARE", MessageBoxButtons.YesNo,
MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                GlobalVariables.customRefAsm.Remove(selecItem);
                refList.SelectedItems[0].Remove();
            }
        }

        /// <summary>
        /// Set namespace to clipborad.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyNamespaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyNamespace(refListView);
        }

        /// <summary>
        /// Set text to clipborad from reference list.
        /// </summary>
        /// <param name="refList"></param>
        private void CopyNamespace(ListView refList) => Clipboard.SetText(refList.SelectedItems[0].Text);

        /// <summary>
        /// Open NuGet manager.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NugetManagerBtn_Click(object sender, EventArgs e)
        {
            NuGetSearch nuGetSearch = new NuGetSearch();
            nuGetSearch.ShowDialog();
        }
    }
}
