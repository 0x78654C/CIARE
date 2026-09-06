Run on Windows with the .NET 10 SDK:

```powershell
pwsh -NoProfile -File Tests.Startup/Run.ps1
```

The runner builds an isolated copy under `.tmp`, substitutes dedicated test data and registry paths, and launches hidden test processes. It leaves logs and startup snapshots in the reported artifact directory. Existing CIARE settings and saved tabs are not used.

Checks cover first visibility, pane sizes, dark/light themes, restored tabs, maximized and corrupt window settings, completion enabled/disabled, Ollama settings, tab creation/removal, caret subscriptions, resize persistence, fullscreen, repeated theme changes, second-launch arguments, and completion during disposal.
