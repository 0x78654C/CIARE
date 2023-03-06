using NuGet.Protocol.Core.Types;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol;
using ILogger = NuGet.Common.ILogger;
using NullLogger = NuGet.Common.NullLogger;

namespace CIARE.Utils.NuGet
{
    public class NuGetSearcher
    {
        /// <summary>
        /// Package name to be searched.
        /// </summary>
        private string PackageName { get; set; } = string.Empty;
        private string NugetApi { get; set; } = string.Empty; 

        public NuGetSearcher(string packageName, string nugetApi)
        {
            PackageName = packageName;
            NugetApi = nugetApi;
        }

        /// <summary>
        /// Get Search rezult from nuget with name of package
        /// </summary>
        /// <param name="richTextBox"></param>
        /// <returns></returns>
        public void Search()
        {
            if (string.IsNullOrEmpty(NugetApi))
                return;

            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceRepository repository = Repository.Factory.GetCoreV3(NugetApi);
            PackageSearchResource resource = Task.Run(() =>repository.GetResourceAsync<PackageSearchResource>()).Result;
            SearchFilter searchFilter = new SearchFilter(includePrerelease: false);

            IEnumerable<IPackageSearchMetadata> results = Task.Run(() => resource.SearchAsync(
                PackageName,
                searchFilter,
                skip:0,
                take:99,
                logger,
                cancellationToken)).Result;
            foreach (var result in results)
            {
                GlobalVariables.nugetPackage.Add($"{result.Identity.Id} | {result.Identity.Version} | {result.Description} ");
            }
        }
    }
}
