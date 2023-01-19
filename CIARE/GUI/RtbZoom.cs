using System.Windows.Forms;

namespace CIARE.GUI
{
    /* Set zoom factor for richtextbox*/
    public class RtbZoom
    {
        public static void RichTextBoxZoom(RichTextBox richTextBox, float zoomValue)
        {
            richTextBox.ZoomFactor = 1f;
            if (zoomValue > 0.015625 || zoomValue < 64)
                richTextBox.ZoomFactor = zoomValue;
        }
    }
}
