using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace CIARE.Roslyn
{
    [SupportedOSPlatform("Windows")]
    public class LibLoaded
    {
        /// <summary>
        /// Check if file is managed
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static (bool isManaged, Assembly assembly, string name) IsManaged(string name)
        {
            try
            {
                var b = AssemblyName.GetAssemblyName(name);
                return (true, Assembly.Load(b), b.Name);
            }
            catch
            {
            }

            return (false, null, null);
        }

        /// <summary>
        /// Grab loaded libraies in CIARE.
        /// </summary>
        private static Dictionary<string, Assembly> _assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Select(e => IsManaged(e))
            .Select(e => new { e.name, e.assembly })
            .GroupBy(e => e.name)
            .ToDictionary(e => e.Key, e => e.First().assembly);


        /// <summary>
        /// Return if a package is loaded in CIARE.
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        public static bool CheckLoadedAssembly(string libPath)
        {
            if (!File.Exists(libPath))
                return false;
            FileInfo fileInfo = new FileInfo(libPath);
            var NameSpace =fileInfo.Name.Replace(".dll",string.Empty);
            return _assemblies.Where(key => key.Key == NameSpace).Any();
        }
    }
}
