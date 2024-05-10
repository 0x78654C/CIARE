using System;
using System.Reflection;
using System.Runtime.Loader;

namespace CIARE.Utils.Options
{
    public class AsmLoad : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public AsmLoad(string mainAssemblyToLoadPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
        }

        protected override Assembly? Load(AssemblyName name)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(name);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
}
