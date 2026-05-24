using System;
using System.Collections;
using System.Windows.Forms;

namespace CIARE.GUI
{
    /// <summary>
    /// Sorts a ListView by clicking column headers, toggling ascending/descending.
    /// </summary>
    internal sealed class ListViewColumnSorter : IComparer
    {
        private int _column;
        private SortOrder _order = SortOrder.None;

        public int SortColumn
        {
            get => _column;
            set
            {
                if (_column == value)
                    _order = _order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
                else
                {
                    _column = value;
                    _order = SortOrder.Ascending;
                }
            }
        }

        public SortOrder Order => _order;

        public int Compare(object x, object y)
        {
            if (_order == SortOrder.None) return 0;

            var lx = (ListViewItem)x;
            var ly = (ListViewItem)y;
            string sx = _column < lx.SubItems.Count ? lx.SubItems[_column].Text : string.Empty;
            string sy = _column < ly.SubItems.Count ? ly.SubItems[_column].Text : string.Empty;

            int result;
            // Numeric sort for the Line column (index 1).
            if (_column == 1 && int.TryParse(sx, out int nx) && int.TryParse(sy, out int ny))
                result = nx.CompareTo(ny);
            else
                result = StringComparer.OrdinalIgnoreCase.Compare(sx, sy);

            return _order == SortOrder.Ascending ? result : -result;
        }
    }
}
