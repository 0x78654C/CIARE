using System.IO;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using System;

namespace CIARE.Utils
{
    [SupportedOSPlatform("windows")]
    public class ControlWriter : TextWriter
    {
        private Control textbox;
        public ControlWriter(Control textbox)
        {
            this.textbox = textbox;
        }

        public override void Write(char value)
        {
            Append(value.ToString());
        }

        public override void Write(string value)
        {
            Append(value);
        }

        private void Append(string value)
        {
            if (string.IsNullOrEmpty(value) || textbox == null || textbox.IsDisposed)
                return;

            if (textbox.InvokeRequired)
            {
                try
                {
                    textbox.BeginInvoke(new Action<string>(Append), value);
                }
                catch (InvalidOperationException)
                {
                }
                return;
            }

            if (textbox is RichTextBox richTextBox)
                richTextBox.AppendText(value);
            else if (textbox is TextBoxBase textBox)
                textBox.AppendText(value);
            else
                textbox.Text += value;
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }
}
