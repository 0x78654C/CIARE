using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Reference
{
    public class CustomRef
    {
        private static bool IsManaged(string libName)
        {
            try
            {
                var b = AssemblyName.GetAssemblyName(libName);
                return true;
            }
            catch
            {
                // Ignore
            }
            return false;
        }

        public static void LoadCustomAssembly(string libName, RichTextBox logOutput)
        {
            if (!IsManaged(libName))
                return;
            try
            {
              //  Assembly.LoadFrom(libName);
            }
            catch (Exception ex)// Exception is for tests at this point
            {
                logOutput.Text = ex.ToString();
            }
        }

    }
}
