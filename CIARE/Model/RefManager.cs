using CIARE.GUI;
using CIARE.Reference;
using CIARE.Utils;
using CIARE.Utils.Options;
using System;
using System.Runtime.Versioning;
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

            // Populate listview with ref.
            CustomRef.PopulateList(GlobalVariables.customRefAsm, refListView);
        }

        /// <summary>
        /// Load reference assembly button control event.
        /// </summary>
        /// <param name="sender"></param>
        private void AddRefFileBtn_Click(object sender, EventArgs e)
        {
            // Add lib's to list dialog.
            FileManage.AddReferenceDialog();

            // Repopulate listview with ref. after loading list.
            CustomRef.PopulateList(GlobalVariables.customRefAsm, refListView);

            // Load assemblies from list.
            CustomRef.SetCustomRefDirective(GlobalVariables.customRefAsm);
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
            var dialogResult = MessageBox.Show($"You are about to remove {selecItem} reference. Are you sure? ", "CIARE", MessageBoxButtons.YesNo,
MessageBoxIcon.Warning);
            if (dialogResult == DialogResult.Yes)
            {
                GlobalVariables.customRefAsm.RemoveAll(x => x.Contains(pathItem));
                refList.SelectedItems[0].Remove();
            }
            try
            {
                WeakReference testAlcWeakRef;
                ExecuteAndUnload(pathItem, out testAlcWeakRef);
                for (int i = 0; testAlcWeakRef.IsAlive && (i < 10); i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }catch(Exception e)
            {
                MessageBox.Show(e.ToString(), "CIARE", MessageBoxButtons.OK,
MessageBoxIcon.Error);
            }
        }

        //TEST
        private static void ExecuteAndUnload(string assemblyPath, out WeakReference alcWeakRef)
        {
            var alc = new AsmLoad(assemblyPath);
            alcWeakRef = new WeakReference(alc, trackResurrection: true);
            alc.Unload();
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
    }
}
