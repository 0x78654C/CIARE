# Editor feature code map

[`../Model/MainForm.cs`](../Model/MainForm.cs) owns the constructor, startup, shutdown, activation lifecycle, and feature instances. Its event handlers and existing public entry points delegate to those instances. The designer and its resources stay beside that file.

The extracted features are independent classes named after their files, in namespaces matching their directories. Stateful classes receive the owning `MainForm` through their constructor and keep their own state. `MainForm` creates them before `InitializeComponent` wires events, and exposes assembly-internal feature properties for explicit calls between related features. Pure helpers (`CompletionItems`, `UsageDocumentCache`, `ProjectFiles`, and `WorkspaceFiles`) are static classes.

| Location | Responsibility |
| --- | --- |
| [Editor/EditorCommands.cs](Editor/EditorCommands.cs) | Keyboard shortcuts and main menu commands |
| [Window/WindowState.cs](Window/WindowState.cs) | Window placement, fullscreen, and Windows startup file selection |
| [Window/MenuStatusLayout.cs](Window/MenuStatusLayout.cs) | Menu button placement and responsive line, column, and diagnostic indicators |
| [Window/WindowTheme.cs](Window/WindowTheme.cs) | Editor highlighting and application colors |
| [LiveShareManage/LiveShareEvents.cs](../LiveShareManage/LiveShareEvents.cs) | Live Share connection and events |
| [Window/UpdateEvents.cs](Window/UpdateEvents.cs) | Update checks and installer handoff |
| [Editor/](Editor/) | Editor setup, text changes, tabs, layout, and diagnostics display |
| [Explorer/](Explorer/) | Explorer controls, pane sizing, tree state, file watching, and file operations |
| [Projects/](Projects/) | Project and solution selection, source files, references, startup project, and builds |
| [NuGetManage/](NuGetManage/) | Package list, restore scheduling, updates, and package metadata |
| [Completion/](Completion/) | Parsing, completion scope, references, syntax adaptation, suggestions, and caches |
| [Navigation/](Navigation/) | Go to definition, Find Usages, document caching, and results window |

Add behavior to the relevant feature class and call it from `MainForm`. Keep members private unless another feature needs them, and use assembly-internal access for those dependencies. Do not add `partial MainForm` declarations to utility files. `MainForm.ProcessCmdKey` delegates shortcut handling to `EditorCommands` and falls back to the base form for unhandled keys.

The `UtilityFeature` entries in `CIARE.csproj` keep these classes opening as code even when existing `.csproj.user` metadata still labels them as forms. Stale resources generated beside the former form partials are excluded from embedding. The form uses `Model/MainForm.resx`.
