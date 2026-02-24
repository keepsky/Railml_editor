# RailML Editor Build Policies

## Architecture Overview
The RailML Editor is built using **WPF (Windows Presentation Foundation)**. WPF is a Windows-only UI framework and requires the Windows operating system to compile and run.

## Build Environments

### Windows Environment (Supported)
This is the **primary and only supported environment** for building and running the RailML Editor application.

**Requirements:**
- OS: Windows 10 or later
- SDK: .NET 8.0 SDK or higher
- IDE: Visual Studio 2022 (recommended) or VS Code with C# Dev Kit

**Building the Application:**
You can build the application using Visual Studio or via the command line:
```powershell
# Restore dependencies and build
dotnet build RailmlEditor.sln /p:UICulture=en-US

# Run the application
dotnet run --project RailmlEditor/RailmlEditor.csproj
```

### Windows Subsystem for Linux (WSL) Environment (Unsupported for GUI)
Because the project targets `net8.0-windows` and relies on WPF, **you cannot build or run the main GUI application inside WSL** (e.g., Ubuntu).
Attempting to run `dotnet build` inside WSL will result in a framework reference error stating that Microsoft.WindowsDesktop.App.WPF is missing.

**What you CAN do in WSL:**
- You can edit source code and use Git commands.
- If you extract the non-GUI business logic (e.g. `RailmlService`, mappings) into a separate platform-agnostic Class Library (`net8.0`), you *can* run unit tests for that library in WSL. However, the `RailmlEditor` project itself must be built on the Windows host.

**Recommendation:**
If you are developing inside WSL, use the **Windows host** terminal (PowerShell or Command Prompt) to execute all `dotnet build` or `dotnet run` commands for this solution.
