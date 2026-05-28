using System.Runtime.Versioning;
using System.Windows.Forms;

namespace CIARE.GUI
{
    [SupportedOSPlatform("windows")]
    internal sealed class DoubleBufferedListView : ListView
    {
        internal DoubleBufferedListView()
        {
            DoubleBuffered = true;
        }
    }
}
