## Why

The solution key (`v`) opens a solution with Windows' default application through ShellExecute, and ShellExecute starts the application with the environment of the process that called it. When Vantage itself was started from a Claude Code session it carries that session's `NO_COLOR`, `CLAUDECODE`, `CLAUDE_PID` and `CLAUDE_CODE_*`, so the IDE inherits them too and runs colourless and believing it is nested. Every other child of Vantage drops these (`InheritedEnvironment`), and so does the editor key; the solution key is the one that leaks.

## What Changes

- The solution is opened by a short-lived PowerShell child that Vantage starts through `IProcessRunner`, so it gets the same cleaned environment as every other child, and that child performs the ShellExecute. The IDE therefore inherits no Claude Code markers.
- The path travels Base64-encoded inside the child's script, so no part of it is read by a command interpreter, whatever the folder is called.
- A failure to open (the file vanished, no application is associated) is reported to the user as before, from the child's exit code and message.
- The pull request key opens its URL through the same call, so the browser it starts is cleaned the same way.
- Out of scope: the editor key (already clean), multiple Claude accounts, a different default application, and whether the IDE comes to the foreground (a manual check, below).

## Capabilities

### New Capabilities

### Modified Capabilities
- `folder-actions`: the solution key opens the solution without Vantage's Claude Code session markers in the application's environment, and without any part of the path read as a command.
- `pull-requests`: the pull request key's browser likewise starts without the markers.

## Impact

- `Source191.Vantage.Infrastructure\Processes\Desktop` (takes an `IProcessRunner`; `OpenAsync` changes) and its construction in `Program.cs`; a new script builder beside it. `IDesktop`, the handlers and `InheritedEnvironment` are unchanged.
- `DesktopShould` gains real-process facts.
- Packaged tool changes, so a patch version bump.
