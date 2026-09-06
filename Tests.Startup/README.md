Run on Windows with the .NET 10 SDK:

```powershell
pwsh -NoProfile -File Tests.Startup/Run.ps1
```

The runner builds an isolated copy under `.tmp`, substitutes dedicated test data and registry paths, and launches hidden test processes. It leaves logs and startup snapshots in the reported artifact directory. Existing CIARE settings and saved tabs are not used.

Checks cover first visibility, pane sizes, dark/light themes, restored tabs, maximized and corrupt window settings, completion enabled/disabled, Ollama settings, tab creation/removal, caret subscriptions, resize persistence, fullscreen, repeated theme changes, second-launch arguments, and completion during disposal.

Editor regressions also check bounds during continuous resizing, scrolling and immediate GDI resource release, suggestions during continued typing (including after a dot), completion insertion, and cancellation after surrounding edits or Escape. Completion tests deliberately hold the worker until more characters have been typed so they do not depend on machine speed.

First-open checks verify that a new page never exposes the empty plus page and that its editor has its final colors, font, and layout before becoming visible. Repeated tab switches must preserve the existing font instance.

The `memory` scenario exercises completion and 1,200 diagnostics, reports managed/private memory and elapsed times, verifies shared metadata and released request state, and checks that cancelled diagnostics cannot replace newer results. Full garbage collections run only inside this measurement harness; the app does not force collections or trim its working set.
