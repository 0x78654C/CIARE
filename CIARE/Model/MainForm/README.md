# MainForm code map

[`../MainForm.cs`](../MainForm.cs) contains the constructor, startup, shutdown, and activation lifecycle. The designer and its resources stay beside that file.

The files here are partial declarations of `CIARE.MainForm`. Keep each feature's state and event handlers together; shared controls remain available through the partial class.

| Location | Responsibility |
| --- | --- |
| `MainForm.Commands.cs` | Keyboard shortcuts and main menu commands |
| `MainForm.Window.cs` | Window placement, fullscreen, and Windows startup file selection |
| `MainForm.Theme.cs` | Editor highlighting and application colors |
| `MainForm.LiveShare.cs` | Live Share connection and events |
| `MainForm.Updates.cs` | Update checks and installer handoff |
| `Editor/` | Editor setup, text changes, tabs, layout, and diagnostics display |
| `Explorer/` | Explorer controls, pane sizing, tree state, file watching, and file operations |
| `Projects/` | Project and solution selection, source files, references, startup project, and builds |
| `NuGet/` | Package list, restore scheduling, updates, and package metadata |
| `Completion/` | Parsing, completion scope, references, syntax adaptation, suggestions, and caches |
| `Navigation/` | Go to definition, Find Usages, document caching, and results window |

Add form-specific behavior to the relevant feature file. Reusable logic belongs in the existing `GUI`, `Utils`, `Roslyn`, or other project folders. Feature partials are marked as code in `CIARE.csproj`; `MainForm.cs` remains the form's designer entry point.

The form uses `../MainForm.resx`. Resource files generated beside feature partials by the WinForms designer are excluded from embedding to prevent duplicate `CIARE.MainForm.resources` output names.
