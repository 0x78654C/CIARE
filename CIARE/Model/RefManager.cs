﻿using CIARE.GUI;
using CIARE.Reference;
using CIARE.Roslyn;
using CIARE.Utils;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Model
{
    [SupportedOSPlatform("Windows")]
    public partial class RefManager : Form
    {
        public static RefManager Instance { get; private set; }
        private int s_initialSizeForm = 0;
        public RefManager()
        {
            InitializeComponent();
        }

        private void RefManager_Load(object sender, EventArgs e)
        {
            //Set instance for usage on cross GUI.
            Instance = this;

            // Get initial width size of form.
            s_initialSizeForm = this.Size.Width;

            // Set dark mode if enabled.
            FrmColorMod.ToogleColorMode(this, GlobalVariables.darkColor);
                
            // Populate listview with local ref.
            CustomRef.PopulateList(GlobalVariables.filteredCustomRef,"", true);

            // Repopulate listview with ref. from local after loading list.
            CustomRef.PopulateListLocal(GlobalVariables.filteredCustomRef, refListView);

            // Populate listview with nuget packages.
            CustomRef.PopulateListNuget(GlobalVariables.nugetNames, refListView);
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
                case Keys.A:
                    LoadReference();
                    return true;
                case Keys.N:
                    LoadNugetSearch();
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Load reference assembly button control event.
        /// </summary>
        /// <param name="sender"></param>
        private void AddRefFileBtn_Click(object sender, EventArgs e)
        {
            LoadReference();
        }

        /// <summary>
        /// Cancel current form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelBtn_Click(object sender, EventArgs e) => this.Close();

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
            var pathItem = refList.Items[refList.Items.IndexOf(refList.SelectedItems[0])].SubItems[1].Text;
            DialogResult dialogResult;
            if (pathItem.Contains(GlobalVariables.downloadNugetPath))
                dialogResult = MessageBox.Show($"You are about to remove {selecItem} reference.\nNuGet package's can be only readded after application restart.\nAre you sure that you want to remove? ", "CIARE", MessageBoxButtons.YesNo,
   MessageBoxIcon.Warning);
            else
                dialogResult = MessageBox.Show($"You are about to remove {selecItem} reference.\nAre you sure that you want to remove? ", "CIARE", MessageBoxButtons.YesNo,
   MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                FileInfo fileInfo = new FileInfo(pathItem);
                foreach (var item in GlobalVariables.customRefAsm)
                    if (item.EndsWith(fileInfo.Name) && item.Contains(GlobalVariables.downloadNugetPath))
                        GlobalVariables.blackRefList.Add(item);
                RemoveFromList(fileInfo.Name);
                refList.SelectedItems[0].Remove();
                GlobalVariables.nugetNames.RemoveAll(x => x.StartsWith(selecItem));
            }

            // Remove all libs that was lodead with nuget download.
            if (!pathItem.EndsWith(".dll"))
            {
                var pathNugetFile = $"{GlobalVariables.downloadNugetPath}{selecItem}.ddb";
                if (!File.Exists(pathNugetFile))
                    return;
                var libsNug = File.ReadAllLines(pathNugetFile);
                
                foreach (var lib in libsNug)
                {
                    RemoveFromList(lib);
                    LibLoaded.RemoveRef(lib);
                }
                File.Delete(pathNugetFile);
                MainForm.Instance.ReloadRef();
            }
            else
            {
                LibLoaded.RemoveRef(pathItem);
                MainForm.Instance.ReloadRef();
            }
        }

        /// <summary>
        /// Clean ref lists with unused libraries.
        /// </summary>
        /// <param name="lib"></param>
        private void RemoveFromList(string lib)
        {
            GlobalVariables.customRefAsm.RemoveAll(x => x.EndsWith(lib));
            GlobalVariables.customRefList.RemoveAll(x => x.EndsWith(lib));
            GlobalVariables.filteredCustomRef.RemoveAll(x => x.EndsWith(lib));
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
            LoadNugetSearch();
        }

        /// <summary>
        /// Resize column of description on form resize.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefManager_Resize(object sender, EventArgs e)
        {
            int changedSize = this.Size.Width - s_initialSizeForm;
            int descriptionSize = refListView.Columns[1].Width;
            refListView.Columns[1].Width = descriptionSize + changedSize;
        }

        /// <summary>
        /// Load NuGet search manager.
        /// </summary>
        private void LoadNugetSearch()
        {
            NuGetSearch nuGetSearch = new NuGetSearch();
            nuGetSearch.ShowDialog();
        }

        /// <summary>
        /// Load local library reference.
        /// </summary>
        private void LoadReference()
        {
            // Add lib's to list dialog.
            FileManage.AddReferenceDialog();

            // Repopulate listview with ref. after loading list.
            CustomRef.PopulateList(GlobalVariables.filteredCustomRef,"",false);

            // Repopulate listview with ref. from local after loading list.
            CustomRef.PopulateListLocal(GlobalVariables.filteredCustomRef, refListView);

            // Load assemblies from list.
            CustomRef.SetCustomRefDirective(GlobalVariables.filteredCustomRef);
        }
    }
}
