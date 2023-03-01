using NuGet.Protocol.Core.Types;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        public async Task Search()
        {
            if (string.IsNullOrEmpty(NugetApi))
                return;

            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceRepository repository = Repository.Factory.GetCoreV3(NugetApi);
            PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>();
            SearchFilter searchFilter = new SearchFilter(includePrerelease: false);

            IEnumerable<IPackageSearchMetadata> results = await resource.SearchAsync(
                PackageName,
                searchFilter,
                skip:0,
                take:99,
                logger,
                cancellationToken);
            foreach (var result in results)
            {
                GlobalVariables.nugetPackage.Add($"{result.Identity.Id} | {result.Identity.Version} | {result.Description} ");
            }
        }
    }
}
