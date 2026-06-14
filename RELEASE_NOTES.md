# CIARE 3.2.0

CIARE 3.2.0 expands the editor from primarily single-file workflows into a more complete C# project and solution environment.

## Highlights

- Create new console, class library, and Windows Forms projects directly in CIARE.
- Create solutions, add projects to existing solutions, and choose the target framework and Native AOT option.
- Manage files, folders, project references, and the startup project from the file explorer.
- Build, run, and publish the active project or solution with project-aware configuration and platform selection.
- Honor custom project output paths, including conditional `OutputPath` settings such as `Release|AnyCPU`.
- Fall back to Visual Studio MSBuild for projects that require full .NET Framework build support, including COM references.

## NuGet And References

- Added a project NuGet package panel to the file explorer.
- Install, update, and remove project packages without leaving CIARE.
- Check for newer package versions and identify packages that appear unused.
- Improved package restore, dependency detection, project references, and package-backed code completion.

## Editor Intelligence

- Improved completion, diagnostics, Go to Definition, and Find Usages across project source files and referenced projects.
- Added better support for SDK implicit usings, generated XAML, WPF, Windows Forms, framework references, and custom project output layouts.
- Improved suggestions from local libraries and fixed several completion and XML diagnostic issues.

## Performance And Interface

- Reduced editor flicker and CPU usage.
- Improved Find Usages performance and project source scanning.
- Added persistent file explorer width, NuGet panel height, expanded folders, and startup project selection.
- Redesigned project explorer actions, About dialog, hotkey documentation, and several editor controls.

## Fixes

- Fixed build platform handling by consistently using `AnyCPU`.
- Fixed Native AOT project creation and publishing.
- Fixed project build output path detection.
- Fixed running code from new unsaved tabs.
- Fixed Go to Definition and WPF code completion.
- Fixed close-tab actions, including Live Share behavior.
- Fixed NuGet dependency and unused-package checks.

Full changes: [Pull request #59](https://github.com/0x78654C/CIARE/pull/59)
