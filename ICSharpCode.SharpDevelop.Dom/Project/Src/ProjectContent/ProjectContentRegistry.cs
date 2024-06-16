// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace ICSharpCode.SharpDevelop.Dom
{


    /// <summary>
    /// Contains project contents read from external assemblies.
    /// Caches loaded assemblies in memory and optionally also to disk.
    /// </summary>
    public class ProjectContentRegistry : IDisposable
    {
        private static object s_lockAssembly = new object();
        internal DomPersistence persistence;
        Dictionary<string, IProjectContent> contents = new Dictionary<string, IProjectContent>(StringComparer.OrdinalIgnoreCase);
        static (bool isManaged, Assembly assembly, string name) IsManaged(string name)
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


        private static Dictionary<string, Assembly> _assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
    .Select(e => IsManaged(e))
    .Select(e => new { e.name, e.assembly })
    .GroupBy(e => e.name)
    .ToDictionary(e => e.Key, e => e.First().assembly);

        public void LoadCustomAssembly(string assemblyName)
        {
            lock (s_lockAssembly)
            {
                var asmName = AssemblyName.GetAssemblyName(assemblyName);
                var asm = Assembly.LoadFile(assemblyName);
                if (!_assemblies.ContainsKey(asmName.Name))
                    _assemblies.Add(asmName.Name, asm);
            }
        }

        public void UnloadCustomAssembly(string assemblyName)
        {
            lock (s_lockAssembly)
            {
                var asmName = AssemblyName.GetAssemblyName(assemblyName);
                if (_assemblies.ContainsKey(asmName.Name))
                    _assemblies.Remove(asmName.Name);
            }
        }

        public IProjectContent[] LoadAll()
        {
            lock (s_lockAssembly)
                    return _assemblies.Select(e => e.Value).Select(e => load(e)).Where(e => e != null).ToArray();
        }

        public IProjectContent load(Assembly assembly)
        {
            try
            {
                return new ReflectionProjectContent(assembly, this);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Disposes all project contents stored in this registry.
        /// </summary>
        public virtual void Dispose()
        {
            List<IProjectContent> list;
            lock (contents)
            {
                list = new List<IProjectContent>(contents.Values);
                contents.Clear();
            }
            // dispose outside the lock
            foreach (IProjectContent pc in list)
            {
                pc.Dispose();
            }
        }

        /// <summary>
        /// Activate caching assemblies to disk.
        /// Cache files will be saved in the specified directory.
        /// </summary>
        public DomPersistence ActivatePersistence(string cacheDirectory)
        {
            if (cacheDirectory == null)
            {
                throw new ArgumentNullException("cacheDirectory");
            }
            else if (persistence != null && cacheDirectory == persistence.CacheDirectory)
            {
                return persistence;
            }
            else
            {
                persistence = new DomPersistence(cacheDirectory, this);
                return persistence;
            }
        }


        ReflectionProjectContent mscorlibContent;


        public virtual IProjectContent Mscorlib
        {
            get
            {
                if (mscorlibContent != null) return mscorlibContent;
                lock (contents)
                {
                    if (contents.ContainsKey("mscorlib"))
                    {
                        mscorlibContent = (ReflectionProjectContent)contents["mscorlib"];
                        return contents["mscorlib"];
                    }
                    int time = LoggingService.IsDebugEnabled ? Environment.TickCount : 0;
                    LoggingService.Debug("Loading PC for mscorlib...");
                    if (persistence != null)
                    {
                        mscorlibContent = persistence.LoadProjectContentByAssemblyName(MscorlibAssembly.FullName);
                        if (mscorlibContent != null)
                        {
                            if (time != 0)
                            {
                                LoggingService.Debug("Loaded mscorlib from cache in " + (Environment.TickCount - time) + " ms");
                            }
                        }
                    }
                    if (mscorlibContent == null)
                    {
                        // We're using Cecil now for everything to find bugs in CecilReader faster
                        //mscorlibContent = CecilReader.LoadAssembly(MscorlibAssembly.Location, this);

                        // After SD 2.1 Beta 2, we're back to Reflection
                        mscorlibContent = new ReflectionProjectContent(MscorlibAssembly, this);
                        if (time != 0)
                        {
                            //LoggingService.Debug("Loaded mscorlib with Cecil in " + (Environment.TickCount - time) + " ms");
                            LoggingService.Debug("Loaded mscorlib with Reflection in " + (Environment.TickCount - time) + " ms");
                        }
                        if (persistence != null)
                        {
                            persistence.SaveProjectContent(mscorlibContent);
                            LoggingService.Debug("Saved mscorlib to cache");
                        }
                    }
                    contents["mscorlib"] = mscorlibContent;
                    contents[mscorlibContent.AssemblyFullName] = mscorlibContent;
                    contents[mscorlibContent.AssemblyLocation] = mscorlibContent;
                    return mscorlibContent;
                }
            }
        }



        public IProjectContent GetExistingProjectContent(DomAssemblyName assembly)
        {
            return GetExistingProjectContent(assembly.FullName);
        }

        public virtual IProjectContent GetExistingProjectContent(string fileNameOrAssemblyName)
        {
            lock (contents)
            {
                if (contents.ContainsKey(fileNameOrAssemblyName))
                {
                    return contents[fileNameOrAssemblyName];
                }
            }

            // GetProjectContentForReference supports redirecting .NET base assemblies to the correct version,
            // so GetExistingProjectContent must support it, too (otherwise assembly interdependencies fail
            // to resolve correctly when a .NET 1.0 assembly is used in a .NET 2.0 project)
            int pos = fileNameOrAssemblyName.IndexOf(',');
            if (pos > 0)
            {
                string shortName = fileNameOrAssemblyName.Substring(0, pos);
                Assembly assembly = GetDefaultAssembly(shortName);
                if (assembly != null)
                {
                    lock (contents)
                    {
                        if (contents.ContainsKey(assembly.FullName))
                        {
                            return contents[assembly.FullName];
                        }
                    }
                }
            }

            return null;
        }




        public static Assembly MscorlibAssembly
        {
            get
            {
                return typeof(object).Assembly;
            }
        }

        public static Assembly SystemAssembly
        {
            get
            {
                return typeof(Uri).Assembly;
            }
        }
        //work


        protected virtual Assembly GetDefaultAssembly(string shortName)
        {
            // These assemblies are already loaded by SharpDevelop, so we
            // don't need to load them in a separate AppDomain/with Cecil.


            switch (shortName)
            {
                case "mscorlib":
                    return MscorlibAssembly;
                case "System": // System != mscorlib !!!
                    return SystemAssembly;
                case "System.Core":
                    return typeof(System.Linq.Enumerable).Assembly;
                case "System.Xml":
                case "System.XML":
                    return typeof(XmlReader).Assembly;
                case "System.Data":
                case "System.Windows.Forms":
                case "System.Runtime.Remoting":
                    return Assembly.Load(shortName + ", Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
                case "System.Configuration":
                case "System.Design":
                case "System.Deployment":
                case "System.Drawing":
                case "System.Drawing.Design":
                case "System.ServiceProcess":
                case "System.Security":
                case "System.Management":
                case "System.Messaging":
                case "System.Web":
                case "System.Web.Services":
                    return Assembly.Load(shortName + ", Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                case "Microsoft.VisualBasic":
                    return Assembly.Load("Microsoft.VisualBasic, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                default:
                    return null;
            }
        }
    }
}


