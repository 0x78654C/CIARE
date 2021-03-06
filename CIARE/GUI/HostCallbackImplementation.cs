using System;
using System.Windows.Forms;
using ICSharpCode.SharpDevelop.Dom;

namespace CIARE.GUI
{
	static class HostCallbackImplementation
	{
		public static void Register(Form1 mainForm)
		{
			// Must be implemented. Gets the project content of the active project.
			HostCallback.GetCurrentProjectContent = delegate {
				return Form1.myProjectContent;
			};

			// The default implementation just logs to Log4Net. We want to display a MessageBox.
			// Note that we use += here - in this case, we want to keep the default Log4Net implementation.
			HostCallback.ShowError += delegate (string message, Exception ex) {
				MessageBox.Show(message + Environment.NewLine + ex.ToString());
			};
			HostCallback.ShowMessage += delegate (string message) {
				MessageBox.Show(message);
			};
			HostCallback.ShowAssemblyLoadError += delegate (string fileName, string include, string message) {
				MessageBox.Show("Error loading code-completion information for "
								+ include + " from " + fileName
								+ ":\r\n" + message + "\r\n");
			};
		}
	}
}
