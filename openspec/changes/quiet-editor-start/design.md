# Design

## Context

`OpenInEditorHandler` builds the editor's command with `EditorCommand` (the executable by full path and the folder as one argument) and hands it to `IDesktop.StartAsync`. `Desktop.StartAsync` calls `Process.Start` with `UseShellExecute = false`, `CreateNoWindow = true` and no redirection. The child therefore inherits Torni's standard handles, which are the Windows Terminal console Terminal.Gui draws on. `CreateNoWindow` only matters to a console program, and editors are windowed programs, so VS Code (Electron, whose Node.js layer prints warnings to standard error) writes into Torni's terminal (#210).

`Desktop.OpenAsync`, used for solutions, links and problem reports, already avoids this. It runs a short-lived `pwsh` child through `IProcessRunner`, with its output captured and its environment cleaned by `InheritedEnvironment`. That child runs `OpenFileScript`, which calls `ShellExecute` (`ProcessStartInfo.UseShellExecute = true`). A ShellExecute start passes no standard handles, and the program it starts gets the child's cleaned environment (`windows.md`, ShellExecute fact).

## Goals / Non-Goals

**Goals:**
- Nothing an editor writes to standard output or standard error reaches Torni's screen.
- Keep every existing property of the editor start: not waited for, no Claude Code markers, the folder as one argument never read as script source, and Windows' reason for a refusal from the error code alone, without .NET's text naming the path.

**Non-Goals:**
- Showing or logging what the editor printed: it is discarded.
- Changing `OpenAsync`, which is already quiet.

## Decisions

### Start the editor through the cleaned PowerShell child, with ShellExecute
`StartAsync` runs the same kind of short-lived `pwsh` child as `OpenAsync`. A new script beside `OpenFileScript` (one type per file, for example `StartProgramScript`) sets `ProcessStartInfo.FileName` to the executable and adds each argument to `ArgumentList`, both decoded from Base64 of their UTF-8 bytes, as `OpenFileScript` does for its target, and sets `UseShellExecute = true`. The child waits only for `Process.Start` to return, not for the editor.
- **Why:** it reuses a tested path for the environment and quoting rules and adds no native code. A ShellExecute start gives the editor no inherited handles, so its output has nowhere to go.
- **Alternative: `CreateProcessW` through `LibraryImport`** with `bInheritHandles = FALSE` and `STARTF_USESTDHANDLES` with no handles (as `tools\HeadlessTerminal` starts its pseudo-console child). This is direct and needs no `pwsh`, but it means building a command line and an environment block by hand, with more native code to test. Rejected for this fix.
- **Alternative: redirect the editor's output and drain it.** Torni would hold pipes for a program that outlives it, and when Torni exits the editor's next write meets a broken pipe. Rejected.
- **Later:** .NET 11's `ProcessStartInfo.StandardOutputHandle` and `File.OpenNullHandle` would allow a direct start with output sent to `NUL`. This is out of scope while the project targets .NET 10.

### Windows' reason survives the hop
A refusal now happens inside the child. The script catches `System.ComponentModel.Win32Exception` and writes only its `NativeErrorCode` on a line of standard error, then exits 1. `Desktop` reads the code and throws `ProgramNotStartedException(new Win32Exception(code).Message)`, which is the sentence the handler shows today. Any other failure falls back to `OpenAsync`'s rule (the child's error text, or its exit code). PowerShell missing from the PATH fails the same way `OpenAsync` does.

## Risks / Trade-offs

- **[A few hundred milliseconds before the editor opens]** `pwsh` must start first. → Accepted: `s` already pays the same cost, and the status line shows the waiting spinner meanwhile.
- **[A console editor gets a console window of its own]** A Custom executable that is a console program opens its own window under ShellExecute instead of drawing into Torni's terminal. → Accepted, and arguably right: Torni's terminal was never usable by it.
- **[ShellExecute treats arguments differently]** `ArgumentList` with `UseShellExecute = true` is joined with .NET's quoting rules, as it is today. → `DesktopShould` starts a real program with a folder holding spaces, `&` and `'` and checks the argument it received.
