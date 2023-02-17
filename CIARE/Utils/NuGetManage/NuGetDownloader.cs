using NuGet.Protocol.Core.Types;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NuGet.Protocol;
using ILogger = NuGet.Common.ILogger;
using NullLogger = NuGet.Common.NullLogger;
using NuGet.Versioning;
using System.IO;
using NuGet.Packaging;

namespace CIARE.Utils.NuGetManage
{
    public class NuGetDownloader
    {
        private string PackageName { get; set; } = string.Empty;
        private string Version { get; set; } = string.Empty;
        private string NugetApi { get; set; } = string.Empty;

        public NuGetDownloader(string packageName,string version, string nugetApi) 
        {
            PackageName= packageName;
            Version= version;
            NugetApi= nugetApi;
        }

        public async void DownloadPackage(RichTextBox richTextBox)
        {
            if (string.IsNullOrEmpty(NugetApi))
                return;
            if (string.IsNullOrEmpty(PackageName))
                return;

            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3(NugetApi);
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            NuGetVersion packageVersion = new NuGetVersion(Version);
            using MemoryStream packageStream = new MemoryStream();
            richTextBox.Text = $"Downloading....";
            await resource.CopyNupkgToStreamAsync(
                PackageName,
                packageVersion,
                packageStream,
                cache,
                logger,
                cancellationToken
                );
            richTextBox.Text = $"Downloaded package: {PackageName} {packageVersion} \n";
            using PackageArchiveReader packageArchiveReader = new PackageArchiveReader( packageStream );

            using var packageReader = new PackageArchiveReader(packageStream, leaveStreamOpen: true);
            PackageExtractionContext packageExtractionContext;
                 var packageSaveMode = packageExtractionContext.PackageSaveMode;
            var packageFiles = await packageReader.GetPackageFilesAsync(packageSaveMode, token);
            await packageArchiveReader.CopyFilesAsync(Application.ExecutablePath,);
            
            NuspecReader nuspecReader = await packageArchiveReader.GetNuspecReaderAsync(cancellationToken);


            richTextBox.Text += $"Tags: {nuspecReader.GetTags()}\n";
            richTextBox.Text += $"Tags: {nuspecReader.GetDescription()}\n";
        }
    }
}
