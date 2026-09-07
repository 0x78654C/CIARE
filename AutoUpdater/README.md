# CIARE ZIP updates

Publish only the application ZIPs to <https://github.com/0x78654C/CIARE/releases>. CIARE accepts uploaded stable-release assets that start with `CIARE_v` and end with `-x64.zip` or `-x86.zip`. The version between them must have three or four numeric parts, for example:

- `CIARE_v3.2.3-x64.zip`
- `CIARE_v3.2.3.1-x64.zip`
- `CIARE_v3.2.3-x86.zip`
- `CIARE_v3.2.3.1-x86.zip`

Three-part versions have a zero revision, so `3.2.3.1` is newer than `3.2.3`. The highest matching filename version is compared with the installed assembly version. CIARE selects the running app’s architecture, ignores draft/prerelease releases and unrelated assets, and never downgrades. The suffix requires a hyphen; `_x86.zip` does not match.

CIARE checks six seconds after its main window appears and through **Help → Check for updates…**. Automatic network failures are silent; manual checks report them.

## How installation works

After confirmation, CIARE prepares a temporary copy of its application/runtime files and launches that copy with `--apply-update`. This opens the graphical installer built into CIARE, before any editor or user-settings initialization. The installer is compiled as a normal application dependency (`CIARE.Updater.dll`) and is automatically included in builds and application ZIPs. There is no separate updater executable to publish or download.

The installer downloads the approved release ZIP from GitHub, verifies its SHA-256 digest and byte count, unpacks it, validates the contained version and architecture, and waits for the original CIARE process to close. CIARE’s usual save prompts remain available; cancelling the close cancels installation. Application files are backed up before replacement and restored on caught installation errors. Files outside the package and AppData/registry settings are preserved. CIARE restarts after success.

The installation folder must be writable and other CIARE windows using it must be closed. If rollback itself fails, the installer reports the retained `.ciare-update-*/backup` path. A machine shutdown during installation can require manual recovery.

Temporary application files and the downloaded ZIP live in `%TEMP%\CIARE-Updates\<unique-id>`. The ZIP (including an incomplete `.partial` download) is removed when the operation finishes. When the updater closes after success, cancellation or an error, it starts the bundled C# cleanup worker from the installation folder. The worker waits for the updater to exit, then removes its entire private folder, retrying locked files for up to one minute. CIARE also watches for updater exit while the editor remains open, and removes copies that were prepared but never launched. No PowerShell or scripts are used for cleanup. `CIARE.UpdateCleanup.exe` and its runtime files are included inside the normal application ZIP; there is no separate download or release asset. Unpacking and rollback files live in `<CIARE installation>\.ciare-update-<unique-id>` and are removed when installation or successful rollback finishes; a failed rollback retains its recovery backup.

## Build release ZIPs

Run on Windows with the .NET 10 SDK:

```powershell
pwsh -NoProfile -File PublishRelease.ps1
```	

This publishes CIARE for x64 and x86, including the built-in installer, and creates the two application ZIPs under `artifacts/releases`. The filename version comes from `CIARE/Properties/AssemblyInfo.cs`. Upload those ZIPs to a published GitHub release.

If packaging manually, ZIP the complete CIARE publish output, including all DLLs and runtime configuration. Files may be at the ZIP root or inside a single enclosing folder. Keep the assembly version aligned with the filename. A three-part ZIP version can contain the same major/minor/build with a nonzero revision. GitHub must provide a SHA-256 digest and positive size for the completed ZIP upload.

Existing builds without the built-in installer need one manual upgrade to a build containing it.

## Validation

```powershell
dotnet run --project Tests.Updater/Tests.Updater.csproj
pwsh -NoProfile -File Tests.Startup/Run.ps1
```

Tests use fake HTTP responses, disposable installation folders and parent processes. They cover ZIP-only release discovery, both version formats, local installer preparation, checksums, cancellation, safe extraction, version/architecture validation, rollback, restart, and theme colors. The shared installer interface is rendered under `artifacts/updater-tests`.
