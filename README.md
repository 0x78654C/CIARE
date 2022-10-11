<p align="center">
  <img src="https://github.com/0x78654C/CIARE/blob/main/Media/ciare.png" width=150>
</p>

# CIARE
Simple text editor for Windows with C# runtime compiler and code execution using Roslyn.
Useful to run code on the fly and get instant result.

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_1.0.2.png?raw=true)

# Features

 - Open/Save current text.
 - Search for text in editor.
 - Load predefined C# code sample template.
 - Display current edited file (Using * on application title near file path ).
 - Compile and run code on ram using Roslyn.
 - Compile code to binary files(.exe or .dll).
 - Show runtime(ms) for code compile and exectuion or compile only.
 - Hotkeys for mentioned above features. 
 - Display output result and errors (ID and message).
 - Highlight code fore various of programing languages.
 - Find and replace text.
 - Show/Hide output window with current state stored for next run.
 - Auto hide/show output window.
 - Split editor window.
 - Set Command Line Parameters.
 - Go to line number.
 - Display total lines for editor data.
 - Display caret position (Line, Column)
 - Folding by curly brackets, region and others.
 - Autosave font size on editor zoom.
 - Code completion (intellisense)
 - Build options(Debug/Release - Any CPU/x64)
 - Enable/disable warnings messages on compile.
 - Mark files for open on next application start or Windows logon.
 - Target desired framework(.NET6 or .NET 7) for use when a application is compiled.

## Requirements:

.NET 6 SDK

 For Roslyn C# code runner use NuGet command in Commands project:
 ```
 Install-Package Microsoft.CodeAnalysis.CSharp -pre
 ```

 For Roslyn C# compiler to binary use NuGet command in Commands project:
 ```
 Install-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform -Version 4.1.0-preview1
 ```

 Mono.Cecil library.(Used in current project is an older version. Can be found on 'Lib' directory.)
 ```
 Install-Package Mono.Cecil -Version 0.9.5.4
 ```

 log4net library.
 ```
 Install-Package log4net -Version 2.0.14
 ```

 As code highlighter I use ICSharpCode.TextEditor.Extended forked from https://github.com/megakraken/ICSharpCode.TextEditor
 For code completion I use the libraries(ICSharpCode.SharpDevelop.Dom,ICSharpCode,Core and NRefactory)
 from https://sourceforge.net/projects/sharpdevelop/files/SharpDevelop%203.x/3.2/ version.

# Sample pictures
![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_inteli.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split2.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_menu.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_recursion.png?raw=true)
