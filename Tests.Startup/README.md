Run on Windows with the .NET 10 SDK:

```powershell
pwsh -NoProfile -File Tests.Startup/Run.ps1
```

The runner builds an isolated copy under `.tmp`, substitutes dedicated test data and registry paths, and launches hidden test processes. It leaves logs and startup snapshots in the reported artifact directory. Existing CIARE settings and saved tabs are not used.

Checks cover first visibility, pane sizes, dark/light themes, restored tabs, maximized and corrupt window settings, completion enabled/disabled, Ollama settings, tab creation/removal, caret subscriptions, resize persistence, fullscreen, repeated theme changes, second-launch arguments, and completion during disposal.

Status layout checks resize between narrow and wide windows with large line/column values and a larger font. They verify that the indicators remain visible, never cover menu commands or Run, and reserve editor space when they wrap below the menu. Reflection checks target the independent feature instances that now own editor and explorer state.

The dark scenario also cancels the normal close sequence with two unsaved documents and verifies that CIARE stays open with both documents intact, including when Save As is cancelled after choosing to save. This covers the save prompt used when handing off to the updater. Isolated startup tests do not enable production GitHub checks.

Editor regressions also check bounds during continuous resizing, scrolling and immediate GDI resource release, suggestions during continued typing (including after a dot), completion insertion, and cancellation after surrounding edits or Escape. Completion tests deliberately hold the worker until more characters have been typed so they do not depend on machine speed.

First-open checks verify that a new page never exposes the empty plus page and that its editor has its final colors, font, and layout before becoming visible. Repeated tab switches must preserve the existing font instance.

File-opening checks also verify that command-line/file-association tabs have their file text, caption and editor bounds ready on first visibility, and release their temporary initial-text reference. Restored-session checks retain all documents and the saved active tab. Loading and completion timing are unchanged.

The `memory` scenario exercises completion and 1,200 diagnostics, reports managed/private memory and elapsed times, verifies shared metadata and released request state, and checks that cancelled diagnostics cannot replace newer results. The measurement harness uses full collections to distinguish retained objects; these memory changes add no forced collections or working-set trimming.

Run the typing and idle resource comparison in Release:

```powershell
& .\Tests.Startup\Run.ps1 -Configuration Release -Scenarios @('resources')
```

The `resources` scenario warms completion references, measures an idle project and edits, then types member and ordinary prefixes in small and 10,000-line documents. It verifies that unchanged completion units are reused, external source/global-using changes are detected, framework and Roslyn fallback suggestions remain available, and cancelled context scans stop. CPU and memory measurements are in `resources.errors.log`; timing includes the hidden UI message loop and background analysis. `resources-baseline` runs the same workload with assertions for the new reuse/cancellation behavior disabled. See [ResourceResults.md](ResourceResults.md) for the comparison and its limits.

The `retention` scenario measures framework metadata and ordinary/member suggestions without requiring keyboard focus. It records managed, private and working-set memory before and after test-only GC, tests a 10,000-line document, and verifies immutable member tables, concurrent first access, correct return types and extension methods. `retention-baseline` skips assertions specific to the new metadata implementation. Numeric suffixes allow repeated fresh-process measurements in a single run. Older baselines' disk metadata caches are redirected into isolated test data.

The `project-build` scenario invokes the explorer command with another project selected as startup and another file active. It verifies that modified project files are saved, unrelated edits stay unsaved, referenced projects are not built, configuration and output paths are respected, the UI responds during compilation, and failed builds restore the command.

```powershell
& .\Tests.Startup\Run.ps1 -Configuration Release -Scenarios @('retention', 'project-build')
```
