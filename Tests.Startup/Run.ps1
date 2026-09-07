param(
    [string]$Configuration = 'Debug',
    [string[]]$Scenarios = @('dark', 'light', 'restored', 'maximized', 'corrupt', 'completion', 'ollama', 'memory')
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$source = Join-Path $workspace 'CIARE'
$runRoot = Join-Path $workspace ('.tmp\startup-tests-' + [guid]::NewGuid().ToString('N'))
$copy = Join-Path $runRoot 'CIARE'
New-Item -ItemType Directory -Path $copy -Force | Out-Null

# Compile a copy with isolated data and registry paths; never launch against real user settings.
Get-ChildItem -LiteralPath $source -Force |
    Where-Object { $_.Name -notin @('bin', 'obj') } |
    Copy-Item -Destination $copy -Recurse -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'StartupRegression.cs') -Destination $copy
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ResourceRegression.cs') -Destination $copy
$globalsPath = Join-Path $copy 'Utils\GlobalVariables.cs'
$globals = [IO.File]::ReadAllText($globalsPath)
$globals = [regex]::Replace($globals, '(?m)^        public static readonly string userProfileDirectory = .*;\r?$',
    '        public static readonly string userProfileDirectory = Environment.GetEnvironmentVariable("CIARE_STARTUP_TEST_DATA") + Path.DirectorySeparatorChar;')
$globals = $globals.Replace('"SOFTWARE\\CIARE"', '"SOFTWARE\\CIARE.StartupTests\\' + (Split-Path $runRoot -Leaf) + '"')
$globals = $globals.Replace('@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"', 'registryPath + "\\Run"')
[IO.File]::WriteAllText($globalsPath, $globals)

$projectPath = Join-Path $copy 'CIARE.csproj'
[xml]$project = Get-Content -LiteralPath $projectPath -Raw
foreach ($reference in $project.Project.ItemGroup.ProjectReference) {
    if ($null -ne $reference) {
        $reference.Include = [IO.Path]::GetFullPath((Join-Path $source $reference.Include))
    }
}
$properties = $project.CreateElement('PropertyGroup')
foreach ($entry in @{ StartupObject = 'CIARE.StartupRegression'; OutputType = 'Exe' }.GetEnumerator()) {
    $property = $project.CreateElement($entry.Key)
    $property.InnerText = $entry.Value
    $properties.AppendChild($property) | Out-Null
}
$project.Project.AppendChild($properties) | Out-Null
$project.Save($projectPath)

dotnet build $projectPath -c $Configuration -m:1 -nr:false -p:UseSharedCompilation=false -p:NuGetAudit=false -clp:ErrorsOnly "-flp:logfile=$runRoot\build.log"
if ($LASTEXITCODE -ne 0) { throw "Test build failed. See $runRoot\build.log" }
$executable = Join-Path $copy "bin\$Configuration\net10.0-windows8.0\CIARE.exe"
$previousDataPath = $env:CIARE_STARTUP_TEST_DATA
try {
    foreach ($scenario in $Scenarios) {
        $env:CIARE_STARTUP_TEST_DATA = Join-Path $runRoot $scenario
        $output = Join-Path $runRoot "$scenario.log"
        $errors = Join-Path $runRoot "$scenario.errors.log"
        $process = Start-Process -FilePath $executable -ArgumentList $scenario -WindowStyle Hidden -PassThru -RedirectStandardOutput $output -RedirectStandardError $errors
        if (-not $process.WaitForExit(60000)) {
            Stop-Process -Id $process.Id
            throw "Startup regression timed out: $scenario"
        }
        Get-Content -LiteralPath $output
        if ($process.ExitCode -ne 0) {
            Get-Content -LiteralPath $errors
            throw "Startup regression failed: $scenario"
        }
    }
}
finally {
    if ($null -eq $previousDataPath) {
        Remove-Item Env:CIARE_STARTUP_TEST_DATA -ErrorAction SilentlyContinue
    } else {
        $env:CIARE_STARTUP_TEST_DATA = $previousDataPath
    }
    [Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree(('SOFTWARE\CIARE.StartupTests\' + (Split-Path $runRoot -Leaf)), $false)
}
Write-Output "Validation artifacts: $runRoot"
