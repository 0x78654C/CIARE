<p align="center">
  <img src="https://github.com/0x78654C/CIARE/blob/main/Media/ciare.png" width=150>
</p>

# CIARE
Simple text editor with C# runtime compiler and code execution using Roslyn.
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

## Requirements:

.NET Framework 4.7.2

 For Roslyn C# code runner use NuGet command in Commands project:
 ```
 Install-Package Microsoft.CodeAnalysis.CSharp -pre
 ```

 For Roslyn C# compiler to binary use NuGet command in Commands project:
 ```
 Install-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform -Version 4.1.0-preview1
 ```

 As code highlighter I use ICSharpCode.TextEditor.Extended forked from https://github.com/megakraken/ICSharpCode.TextEditor

# Sample pictures
![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split2.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_menu.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_recursion.png?raw=true)
