<p align="center">
  <img src="https://github.com/0x78654C/CIARE/blob/main/Media/ciare.png" width=150>
</p>

# CIARE
Simple text editor for Windows with C# runtime compiler and code execution using Roslyn.
Useful to run code on the fly and get instant result.

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_2.0.4.png?raw=true)

# Features

 - Open/Save current text.
 - Search for text in editor.
 - Load predefined C# code sample template.
 - Display current edited file (Using * on application title near file path ).
 - Compile and run code on ram using Roslyn.
 - Compile code to binary files(.exe or .dll).
 - Show runtime(ms) for code compile and exectuion or compile only.
 - Hotkeys for mentioned above features. 
 - Drag & Drop
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
 - Target desired framework(.NET6, .NET 7 or .NET 8) for use when a application is compiled.
 - Live share: share and work in same time at a project/file data.
 - Generate code/data with chatGPT from OpenAI.
 - Add reference to custom managed libraries.
 - NuGet Manager
 - Usage of unsafe code.
 - Error window notification in line.

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

 When live share is started and connection to API message box will be prompted a red notification bubble will appear in right-up corner that notify you that is broadcasting and current tab is colored red aswel. 
 In the process of live share tabs cannot be closed.

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ls_notify.png?raw=true)
![live2](https://github.com/0x78654C/CIARE/assets/13780514/fc1a8915-4439-4b5c-88f4-d957ece90f2e)

 3. To start a remote connection go to Live tab -> Live Share Manage and add your given session id and password 
    and add them to 'Remote Session Id/Password' text boxes. After click on 'Remote Connect' button.
    Same here will be notified with a the red dot when broadcast is started.

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ls_remote.png?raw=true)

INFO: Maximum connection are two / session id.
After connection is made the remote connected user can see host user data but can edit just after host made the first edit on data.

# Live share video sample

https://user-images.githubusercontent.com/13780514/201774933-e53d3ba2-95e0-434e-aa9a-16489169afd5.mp4

# ChatGPT autocomplete
  
 ChatGPT is chatbot from https://openai.com that uses AI with GPT3 integration that answers questions with a very precise output.
 That been said chatGPT can be even be used as code generator for your projects.
 * Usage:
 1. Add your OpenAI API key in Settings>Options
 2. Add the amount of maxim tokens to be displayed. More info at https://beta.openai.com/tokenizer
 1. Write your question in editor using the following format ![image](https://user-images.githubusercontent.com/13780514/208530240-81cc2960-c6a8-484b-9e35-06f5a1f151ba.png)
 2. Got to Edit>ChatGPT or press hotkeys CTRL + Shift + P

 Answer will be display automatically in your editor.

# ChatGPT code generator video sample

https://user-images.githubusercontent.com/13780514/208532011-bd2327fd-fcdd-47ad-8818-306739317326.mp4


# NuGet Manager

Download NuGet packages directly from https://www.nuget.org/ and use it in CIARE instantly. It will be downloded latest package autmatic. 


https://user-images.githubusercontent.com/13780514/223232524-22b1c5a3-795e-4735-b6c0-375e99d9069c.mp4


# Sample pictures
![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_inteli.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split2.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_menu.png?raw=true)

![alt text](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_recursion.png?raw=true)

# HotKeys
```
----------- File management --------------- 
CTRL + N         : Empty the current tab file and sets to new page.
CTRL + O         : Open file.
CTRL + S         : Save to data to current file if changed.
CTRL + Shift + S : Save data to a new file name or existing one.
CTRL + T         : Load C# Main template.

------------ Editor management ------------
CTRL + Z         : Undo last modifications.
CTRL + Y         : Redo last modifications.
CTRL + Delete    : Delete words to right.
CTRL + Backspace : Delete words to left.
CTRL + D         : Delete current line.
CTRL + Shift + D : Delete from cursor to end of line.
CTRL + X         : Cut selection.
CTRL + C         : Copy selection.
CTRL + V         : Paste selection.
DEL              : Delete Selection.
CTRL + F         : Find text in current tab.
CTRL + H         : Replace text in current tab.
CTRL + G         : Go to line number in current tab.
CTRL + A         : Select all text in current tab
CTRL + Shift + P : Get data from chatGPT by your provided text pattern.

---------------- Compile ------------------
F5               : Run current cod.                
CTRL + B         : Compile code from current tab to executable file. (.exe)
CTRL + Shift + B : Compile code from current tab to dynamic-link library. (.dll)
CTRL + L         : Add command line arguments.
CTRL + R         : Add external reference or download from NuGet.

------------------ View -------------------
CTRL + W         : Split window vertically.
CTRL + Shift + W : Split window horizontally.
CTRL + U         : Switch between splited window area.
CTRL + K         : Show/Hide output window.

------------- Tabs management -------------
CTRL + Tab       : Adds new tab.
CTRL + Left      : Switches tabs to left.
CTRL + Right     : Switches tabs to right.

----------- Live share management ---------
CTRL + Q         : Start live share management window.

------------ NuGet Search Window ----------
SHIFT + F10      : Download selected NuGet package.
```
