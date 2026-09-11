# Editor feature code map

[`../Model/MainForm.cs`](../Model/MainForm.cs) contains the constructor, startup, shutdown, and activation lifecycle. The designer and its resources stay beside that file.

The extracted form features use descriptive filenames grouped by responsibility. They remain partial declarations of `CIARE.MainForm` so event handlers can share the form's controls and state. Existing utility classes retain their own types and namespaces.

| Location | Responsibility |
| --- | --- |
| [Editor/EditorCommands.cs](Editor/EditorCommands.cs) | Keyboard shortcuts and main menu commands |
| [Window/WindowState.cs](Window/WindowState.cs) | Window placement, fullscreen, and Windows startup file selection |
| [Window/WindowTheme.cs](Window/WindowTheme.cs) | Editor highlighting and application colors |
| [LiveShareManage/LiveShareEvents.cs](../LiveShareManage/LiveShareEvents.cs) | Live Share connection and events |
| [Window/UpdateEvents.cs](Window/UpdateEvents.cs) | Update checks and installer handoff |
| [Editor/](Editor/) | Editor setup, text changes, tabs, layout, and diagnostics display |
| [Explorer/](Explorer/) | Explorer controls, pane sizing, tree state, file watching, and file operations |
| [Projects/](Projects/) | Project and solution selection, source files, references, startup project, and builds |
| [NuGetManage/](NuGetManage/) | Package list, restore scheduling, updates, and package metadata |
| [Completion/](Completion/) | Parsing, completion scope, references, syntax adaptation, suggestions, and caches |
| [Navigation/](Navigation/) | Go to definition, Find Usages, document caching, and results window |

Add form behavior to the relevant feature file and reusable logic to a utility class. The `FormFeature` entries in `CIARE.csproj` mark the extracted files as code; `Model/MainForm.cs` remains the form's designer entry point.

The form uses `Model/MainForm.resx`. Resource files generated beside feature files by the WinForms designer are excluded from embedding, including explicit resource entries added by the designer, to prevent duplicate `CIARE.MainForm.resources` output names.
