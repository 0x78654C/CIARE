using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using ILogger = NuGet.Common.ILogger;
using NullLogger = NuGet.Common.NullLogger;

namespace CIARE.Utils.NuGet
{
    public class NuGetVersions
    {
        /// <summary>
        /// NuGet package name.
        /// </summary>
        private string PackageName { get; set; } = string.Empty;
        private string NugetApi { get; set; }=string.Empty;
        
        public NuGetVersions(string packageName, string nugetApi)
        {
            PackageName = packageName;
            NugetApi = nugetApi;
        }

        /// <summary>
        /// Get nuget package version.
        /// </summary>
        /// <param name="richTextBox"></param>
        public async void GetVerions(RichTextBox richTextBox)
        {
            if (string.IsNullOrEmpty(NugetApi))
                return;
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(NugetApi);
            FindPackageByIdResource resource = await repository.GetResourceAsync <FindPackageByIdResource>();

            IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                PackageName,
                cache,
                logger,
                cancellationToken);
            foreach(var version in versions)
            {
                GlobalVariables.packageVersions.Add(version.Version.ToString());
            }
            richTextBox.Clear();
        }
    }
}
