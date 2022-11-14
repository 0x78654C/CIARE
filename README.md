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
 - Live share: share and work in same time at a project/file data.

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

## Live share

 With Live Share you can work on same project/file in same time with another person. 

 Project is based on https://dotnet.microsoft.com/en-us/apps/aspnet/signalr library.

 Requirements: 
 
  A hosted CIARE Live Share API: https://github.com/0x78654C/CIARELiveShareAPI 
  for managing connection.

 Setup:
 1. Save your API link in Settings tab -> Options -> Live Share

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ls_setting.png?raw=true)

 2. To start a Live Share connection go to Live tab -> Live Share Manage and click on 'Start Live share' button.
    After the live share is started give the Session Id and Password to the person you want to share with.

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ls_start.png?raw=true)

 When live share is started and connection to API message box will be prompted a red notification bubble will appear in right-up corner that notify you that is broadcasting.

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ls_notify.png?raw=true)

 3. To start a remote connection go to Live tab -> Live Share Manage and add your given session id and password 
    and add them to 'Remote Session Id/Password' text boxes. After click on 'Remote Connect' button.
    Same here will be notified with a the red dot when broadcast is started.

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ls_remote.png?raw=true)

INFO: Maximum connection are two / session id.
After connection is made the remote connected user can see host user data but can edit just after host made the first edit on data.

# Live share video sample

https://user-images.githubusercontent.com/13780514/201774933-e53d3ba2-95e0-434e-aa9a-16489169afd5.mp4



# Sample pictures
![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_inteli.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split2.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_menu.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_recursion.png?raw=true)
