using NuGet.Protocol.Core.Types;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NuGet.Protocol;
using ILogger = NuGet.Common.ILogger;
using NullLogger = NuGet.Common.NullLogger;
using System.IO;
using NuGet.Packaging;
using System.Linq;
using System;
using Microsoft.CodeAnalysis;
using NuGet.Frameworks;

namespace CIARE.Utils.NuGetManage
{
    public class NuGetDownloader
    {
        private string PackageName { get; set; } = string.Empty;
        private string NugetApi { get; set; } = string.Empty;
        private List<string> Dependencies = new List<string>();
        private List<string> LocalDependencies = new List<string>();
        private readonly string _downloadPath = $"{Application.StartupPath}\\nuget\\";
        public NuGetDownloader(string packageName, string nugetApi)
        {
            PackageName = packageName;
            NugetApi = nugetApi;
        }

        /// <summary>
        /// NuGet pacakge downloader
        /// </summary>
        /// <param name="richTextBox"></param>
        public async void DownloadPackage(RichTextBox richTextBox)
        {

            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            if (!Directory.Exists(_downloadPath))
                Directory.CreateDirectory(_downloadPath);
            SourceRepository repository = Repository.Factory.GetCoreV3(NugetApi);
            SourceCacheContext cache = new SourceCacheContext();
            PackageSearchResource packageSearchResource = await repository.GetResourceAsync<PackageSearchResource>();
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            FindPackageByIdResource findPackageByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>();
            SearchFilter searchFilter = new SearchFilter(includePrerelease: false);
            int skip = 0;
            long bytesDownloaded = 0;

            var results = (await packageSearchResource.SearchAsync(
                PackageName,
                searchFilter,
                skip: skip,
                take: 1,
                logger,
                cancellationToken)).ToList();
            richTextBox.Text = "--";
            foreach (IPackageSearchMetadata result in results)
            {
                richTextBox.Text += $"package {result.Identity.Id} {result.Identity.Version}\n";
                var versions = await result.GetVersionsAsync();
                var version = versions.LastOrDefault();
                var fileStore = $"{_downloadPath}{result.Identity.Id}.{version.Version}.zip";
                using var packageStream = File.OpenWrite(fileStore);
                await findPackageByIdResource.CopyNupkgToStreamAsync(
                    result.Identity.Id,
                    version.Version,
                    packageStream,
                    cache,
                    logger,
                    cancellationToken);
                richTextBox.Text += $" downloaded version {version.Version} {packageStream.Length} bytes\n";
                bytesDownloaded += packageStream.Length;
                packageStream.Close();

                await Task.Run(() => ArchiveManager.Extract(fileStore, richTextBox));

                using MemoryStream packageStreamDep = new MemoryStream();

                await resource.CopyNupkgToStreamAsync(
                    result.Identity.Id,
                    version.Version,
                    packageStreamDep,
                    cache,
                    logger,
                    cancellationToken);

                await GetDependencies(packageStreamDep, cancellationToken, richTextBox);

            }

            richTextBox.Text += $"Downloaded {bytesDownloaded}";
        }

       /// <summary>
       /// Extract package after download.
       /// </summary>
       /// <param name="richTextBox"></param>
        public void Extract(RichTextBox richTextBox)
        {
            foreach(var file in GlobalVariables.downloadPackages)
                ArchiveManager.Extract(file, richTextBox);

        }

        /// <summary>
        /// Download dependecies from last package.
        /// </summary>
        /// <param name="packageStreamDep"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="richTextBox"></param>
        /// <returns></returns>
        private async Task GetDependencies(MemoryStream packageStreamDep, CancellationToken cancellationToken, RichTextBox richTextBox)
        {
            using var packageReader = new PackageArchiveReader(packageStreamDep);
            var nuspecReader = await packageReader.GetNuspecReaderAsync(cancellationToken);
            nuspecReader.GetDescription();
            NuGetFramework.ParseFolder("net6.0");
            foreach (var dependencyGroup in nuspecReader.GetDependencyGroups())
            {
                foreach (var dependecyPackage in dependencyGroup.Packages)
                {
                    var pakageName = GetPackageName(dependecyPackage.ToString());
                    var refList = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).ToList();
                    foreach (var dep in refList)
                    {
                        LocalDependencies.Add(GetRefName(dep));
                    }
                    if (!Dependencies.Contains(pakageName) && !LocalDependencies.Contains(pakageName))
                    {

                        Dependencies.Add(pakageName);
                        PackageName = pakageName;
                        if (!File.Exists($"{_downloadPath}{pakageName}.zip"))
                        {
                            DownloadPackage(richTextBox);
                        }
                    }
                }
            }
            Dependencies.Clear();
        }

        /// <summary>
        /// Get reference name. 
        /// </summary>
        /// <param name="refItem"></param>
        /// <returns></returns>
        private string GetRefName(string refItem)
        {
            var count = refItem.Split('\\').Count();
            return refItem.Split('\\')[count - 1].Replace(".dll", "");
        }

        /// <summary>
        /// Get package name.
        /// </summary>
        /// <param name="packageName"></param>
        /// <returns></returns>
        private string GetPackageName(string packageName) => packageName.Split(' ')[0];

    }
}
