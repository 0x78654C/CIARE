param(
    [ValidateSet('x64', 'x86')][string[]]$Architectures = @('x64', 'x86'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts\releases')
)
$ErrorActionPreference = 'Stop'
$output = [IO.Path]::GetFullPath($OutputDirectory)
$assemblyInfo = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'CIARE\Properties\AssemblyInfo.cs') -Raw
$versionMatch = [regex]::Match($assemblyInfo, 'AssemblyVersion\("(?<version>\d+\.\d+\.\d+(?:\.\d+)?)"\)')
if (-not $versionMatch.Success) { throw 'Cannot read CIARE AssemblyVersion.' }
$version = $versionMatch.Groups['version'].Value
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach ($architecture in $Architectures) {
    # A fresh output folder prevents stale files from previous publishes entering the release ZIP.
    $publish = Join-Path $output ('publish-' + $architecture + '-' + [guid]::NewGuid().ToString('N'))
    dotnet publish (Join-Path $PSScriptRoot 'CIARE\CIARE.csproj') -c Release -r "win-$architecture" --self-contained false -o $publish -m:1 -nr:false -p:UseSharedCompilation=false -p:PublishSingleFile=false -p:NuGetAudit=false -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw "CIARE publish failed for $architecture" }
    if (-not (Test-Path -LiteralPath (Join-Path $publish 'CIARE.Updater.dll'))) { throw 'The bundled installer is missing from the application output.' }
    foreach ($file in @('CIARE.UpdateCleanup.exe', 'CIARE.UpdateCleanup.dll', 'CIARE.UpdateCleanup.runtimeconfig.json')) {
        if (-not (Test-Path -LiteralPath (Join-Path $publish $file))) { throw "The bundled C# cleanup worker is missing: $file" }
    }
    $asset = Join-Path $output "CIARE_v$version-$architecture.zip"
    Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $asset -Force
    Get-FileHash -LiteralPath $asset -Algorithm SHA256 | Select-Object Path, Hash
}
Write-Output "Upload the CIARE_v$version ZIP files from $output to the GitHub release."
