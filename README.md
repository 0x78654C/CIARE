<p align="center">
  <img src="https://github.com/0x78654C/CIARE/blob/main/CIARE/logoCiare.png" width=250>
</p>

<h1 align="center">CIARE</h1>
<p align="center">
  A lightweight Windows text editor with C# runtime compilation and code execution powered by Roslyn.<br>
  Write and run C# code on the fly — no project setup required.
</p>

---

![Preview](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_2.0.4.png?raw=true)

---

## ✨ Features

### Editor
- Open / Save files
- Search and replace text
- Go to line number
- Display total lines and caret position (Line, Column)
- Syntax highlighting for various programming languages
- Code folding (curly brackets, regions, and more)
- Code completion (IntelliSense)
- Split editor window (vertical & horizontal)
- Drag & Drop support
- Autosave font size on editor zoom
- Load predefined C# code sample templates
- Display current edited file state (`*` indicator in title bar)

### Compilation & Execution
- Compile and run C# code in memory using Roslyn
- Compile code to binary files (`.exe` or `.dll`)
- Publish code to binary files (`.exe` or `.dll`)
- Native AOT publish option
- Build options: Debug / Release — Any CPU / x64
- Target desired framework (.NET 6, .NET 7, .NET 8)
- Set command line parameters
- Show runtime (ms) for compile and execution
- Enable / disable compiler warning messages
- Usage of unsafe code
- Inline error window notifications
- Display output results and errors (ID and message)

### Workspace & Files
- Show / Hide output window (state persisted across sessions)
- Auto hide / show output window
- Mark files to open on next app start or Windows logon
- Add references to custom managed libraries
- NuGet Manager — download and use packages instantly

### AI Integration
- Generate code / data with AI or work with selected text
- ChatGPT (OpenAI) integration
- OpenRouter multi-model AI integration
- Ollama local LLM integration (auto-detects installed models)

### Collaboration
- Live Share — collaborate on the same file in real time

---

## 📋 Requirements

- **[.NET 10 SDK](https://dotnet.microsoft.com/download)**

**NuGet packages** (install via Package Manager Console in the Commands project):

```powershell
# Roslyn in-memory C# runner
Install-Package Microsoft.CodeAnalysis.CSharp -pre

# Roslyn binary compiler
Install-Package Microsoft.CodeDom.Providers.DotNetCompilerPlatform -Version 4.1.0-preview1

# Mono.Cecil (older version — also available in the Lib directory)
Install-Package Mono.Cecil -Version 0.9.5.4

# Logging
Install-Package log4net -Version 2.0.14
```

**Third-party libraries:**
- **Code highlighter:** [ICSharpCode.TextEditor.Extended](https://github.com/megakraken/ICSharpCode.TextEditor)
- **Code completion:** `ICSharpCode.SharpDevelop.Dom`, `ICSharpCode.Core`, `NRefactory` from [SharpDevelop 3.2](https://sourceforge.net/projects/sharpdevelop/files/SharpDevelop%203.x/3.2/)

---

## 🔴 Live Share

Collaborate on the same project or file in real time with another person.

**Built on:** [ASP.NET SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr)

**Requires:** A hosted [CIARE Live Share API](https://github.com/0x78654C/CIARELiveShareAPI)

### Setup

**1.** Save your API link in **Settings → Options → Live Share**

![Live Share Settings](https://github.com/0x78654C/CIARE/blob/main/Media/ls_setting.png?raw=true)

**2.** Go to **Live → Live Share Manage** and click **Start Live Share**. Share the generated Session ID and Password with your collaborator.

![Live Share Start](https://github.com/0x78654C/CIARE/blob/main/Media/ls_start.png?raw=true)

When broadcasting, a red notification bubble appears in the top-right corner and the active tab is highlighted red. Tabs cannot be closed during a live share session.

![Live Share Notification](https://github.com/0x78654C/CIARE/blob/main/Media/ls_notify.png?raw=true)
![Live Share Active](https://github.com/0x78654C/CIARE/assets/13780514/fc1a8915-4439-4b5c-88f4-d957ece90f2e)

**3.** To join a session, go to **Live → Live Share Manage**, enter the Session ID and Password in the **Remote Session Id/Password** fields, then click **Remote Connect**.

![Live Share Remote](https://github.com/0x78654C/CIARE/blob/main/Media/ls_remote.png?raw=true)

> **Note:** Maximum 2 connections per session ID. After connecting, the remote user can view the host's data and edit once the host makes the first change.

### 🎥 Live Share Demo

https://user-images.githubusercontent.com/13780514/201774933-e53d3ba2-95e0-434e-aa9a-16489169afd5.mp4

---

## 🤖 AI Integration

### ChatGPT / OpenRouter

- [ChatGPT](https://openai.com) — precise AI answers and code generation
- [OpenRouter](https://openrouter.ai/) — access multiple AI models from different providers

**Usage:**
1. Add your OpenAI or OpenRouter API key in **Settings → Options** (model and token limit configurable)
2. Go to **Edit → Ask AI** or press `CTRL + Shift + P`
3. Optionally select text in the editor to send it as context

The AI response is inserted directly into your editor.

### 🎥 AI Code Generator Demo

https://github.com/user-attachments/assets/65b82bf9-de19-4ca7-8b51-bac53a8ca31c

### Ollama (Local LLMs)

CIARE automatically detects your Ollama installation and available models. Use it the same way as ChatGPT.

---

## 📦 NuGet Manager

Download NuGet packages directly from [nuget.org](https://www.nuget.org/) and use them in CIARE instantly. The latest package version is downloaded automatically.

https://user-images.githubusercontent.com/13780514/223232524-22b1c5a3-795e-4735-b6c0-375e99d9069c.mp4

---

## 🖼️ Screenshots

![IntelliSense](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_inteli.png?raw=true)

![Split View 2](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split2.png?raw=true)

![Split View](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_split.png?raw=true)

![Menu](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_menu.png?raw=true)

![Recursion](https://github.com/0x78654C/CIARE/blob/main/Media/ciare_recursion.png?raw=true)

---

## ⌨️ Hotkeys

```
----------- File Management ---------------
CTRL + N         : New file (clears current tab)
CTRL + O         : Open file
CTRL + S         : Save current file
CTRL + Shift + S : Save As
CTRL + T         : Load C# Main template

------------ Editor Management ------------
CTRL + Z         : Undo
CTRL + Y         : Redo
CTRL + Delete    : Delete word to the right
CTRL + Backspace : Delete word to the left
CTRL + D         : Delete current line
CTRL + Shift + D : Delete from cursor to end of line
CTRL + X         : Cut selection
CTRL + C         : Copy selection
CTRL + V         : Paste
DEL              : Delete selection
CTRL + F         : Find text
CTRL + H         : Find and replace text
CTRL + G         : Go to line number
CTRL + A         : Select all
CTRL + Shift + P : Ask AI (optionally with selected text)

---------------- Compile ------------------
F5               : Run current code
CTRL + B         : Compile to binary (.dll/.exe)
CTRL + Shift + B : Publish to binary (.dll/.exe)
CTRL + L         : Set command line arguments
CTRL + R         : Add reference / download from NuGet

------------------ View -------------------
CTRL + W         : Split window vertically
CTRL + Shift + W : Split window horizontally
CTRL + U         : Switch between split areas
CTRL + K         : Show / Hide output window
F11              : Toogle full screen

------------- Tabs Management -------------
CTRL + Tab       : Add new tab
CTRL + Left      : Switch to left tab
CTRL + Right     : Switch to right tab

----------- Live Share Management ---------
CTRL + Q         : Open Live Share management window

------------ NuGet Search Window ----------
SHIFT + F10      : Download selected NuGet package
```
