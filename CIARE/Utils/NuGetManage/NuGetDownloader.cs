using Microsoft.CodeAnalysis;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using ILogger = NuGet.Common.ILogger;
using NullLogger = NuGet.Common.NullLogger;

namespace CIARE.Utils.NuGetManage
{
    [SupportedOSPlatform("windows")]
    public class NuGetDownloader
    {
        private string PackageName { get; set; } = string.Empty;
        private string NugetApi { get; set; } = string.Empty;
        private List<string> Dependencies = new List<string>();
        private List<string> LocalDependencies = new List<string>();
        private List<string> FrameworkList = new List<string>();


        public NuGetDownloader(string packageName, string nugetApi, List<string> frameworkList)
        {
            PackageName = packageName;
            NugetApi = nugetApi;
            FrameworkList = frameworkList;
        }

        /// <summary>
        /// NuGet pacakge downloader
        /// </summary>
        /// <param name="richTextBox"></param>
        public void DownloadPackage()
        {

            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            if (!Directory.Exists(GlobalVariables.downloadNugetPath))
                Directory.CreateDirectory(GlobalVariables.downloadNugetPath);
            SourceRepository repository = Repository.Factory.GetCoreV3(NugetApi);
            SourceCacheContext cache = new SourceCacheContext();
            PackageSearchResource packageSearchResource = Task.Run(() => repository.GetResourceAsync<PackageSearchResource>()).Result;
            FindPackageByIdResource resource = Task.Run(() => repository.GetResourceAsync<FindPackageByIdResource>()).Result;
            FindPackageByIdResource findPackageByIdResource = Task.Run(() => repository.GetResourceAsync<FindPackageByIdResource>()).Result;
            SearchFilter searchFilter = new SearchFilter(includePrerelease: false);
            int skip = 0;
            long bytesDownloaded = 0;

            var results = (Task.Run(() => packageSearchResource.SearchAsync(
                PackageName,
                searchFilter,
                skip: skip,
                take: 1,
                logger,
                cancellationToken)).Result).ToList();
            foreach (IPackageSearchMetadata result in results)
            {
                var versions = Task.Run(() => result.GetVersionsAsync()).Result;
                var version = versions.LastOrDefault();
                var fileStore = $"{GlobalVariables.downloadNugetPath}{result.Identity.Id}.{version.Version}.zip";
                using var packageStream = File.OpenWrite(fileStore);
                Task.Run(()=> findPackageByIdResource.CopyNupkgToStreamAsync(
                    result.Identity.Id,
                    version.Version,
                    packageStream,
                    cache,
                    logger,
                    cancellationToken)).Wait();
                bytesDownloaded += packageStream.Length;
                packageStream.Close();

                ArchiveManager.Extract(fileStore);
                using MemoryStream packageStreamDep = new MemoryStream();

                Task.Run(() => resource.CopyNupkgToStreamAsync(
                    result.Identity.Id,
                    version.Version,
                    packageStreamDep,
                    cache,
                    logger,
                    cancellationToken)).Wait();
                GetLatestFrameworkFile(FrameworkList);
                GetDependencies(packageStreamDep, cancellationToken, version.Version.ToString());
            }
        }

        /// <summary>
        /// Download dependecies from last package.
        /// </summary>
        /// <param name="packageStreamDep"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="richTextBox"></param>
        /// <returns></returns>
        private void  GetDependencies(MemoryStream packageStreamDep, CancellationToken cancellationToken, string version)
        {
            using var packageReader = new PackageArchiveReader(packageStreamDep);
            var nuspecReader = Task.Run(() => packageReader.GetNuspecReaderAsync(cancellationToken)).Result;
            nuspecReader.GetDescription();
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
                        DownloadPackage();
                    }
                }
            }
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


        /// <summary>
        /// Get file from downloaded path.
        /// </summary>
        /// <param name="listFramework"></param>
        private void GetLatestFrameworkFile(List<string> listFramework)
        {
            if (!Directory.Exists(GlobalVariables.downloadNugetPath))
                return;
            FileManage.SearchFile(GlobalVariables.downloadNugetPath, listFramework);
        }
    }
}
