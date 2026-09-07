Run on Windows with the .NET 10 SDK:

```powershell
pwsh -NoProfile -File Tests.Startup/Run.ps1
```

The runner builds an isolated copy under `.tmp`, substitutes dedicated test data and registry paths, and launches hidden test processes. It leaves logs and startup snapshots in the reported artifact directory. Existing CIARE settings and saved tabs are not used.

Checks cover first visibility, pane sizes, dark/light themes, restored tabs, maximized and corrupt window settings, completion enabled/disabled, Ollama settings, tab creation/removal, caret subscriptions, resize persistence, fullscreen, repeated theme changes, second-launch arguments, and completion during disposal.

The dark scenario also cancels the normal close sequence with two unsaved documents and verifies that CIARE stays open with both documents intact, including when Save As is cancelled after choosing to save. This covers the save prompt used when handing off to the updater. Isolated startup tests do not enable production GitHub checks.

Editor regressions also check bounds during continuous resizing, scrolling and immediate GDI resource release, suggestions during continued typing (including after a dot), completion insertion, and cancellation after surrounding edits or Escape. Completion tests deliberately hold the worker until more characters have been typed so they do not depend on machine speed.

First-open checks verify that a new page never exposes the empty plus page and that its editor has its final colors, font, and layout before becoming visible. Repeated tab switches must preserve the existing font instance.

File-opening checks also verify that command-line/file-association tabs have their file text, caption and editor bounds ready on first visibility, and release their temporary initial-text reference. Restored-session checks retain all documents and the saved active tab. Loading and completion timing are unchanged.

The `memory` scenario exercises completion and 1,200 diagnostics, reports managed/private memory and elapsed times, verifies shared metadata and released request state, and checks that cancelled diagnostics cannot replace newer results. Full garbage collections run only inside this measurement harness; the app does not force collections or trim its working set.
