using System.Reflection;

namespace CIARE.Roslyn
{
    class ManageCheck
    {
        /// <summary>
        /// Check if file is managed code lib.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsManaged(string path)
        {
            try
            {
                var b = AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch
            {}
            return false;
        }
    }
}
