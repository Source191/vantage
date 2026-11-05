# Proposal

## Why

Pressing `f` to open a folder in VS Code writes VS Code's own start-up messages (a Node.js deprecation warning and a storage line) over the top of Torni's screen (issue #210). The editor is started as a child that inherits Torni's console, so anything it prints lands in Torni's terminal until the next redraw.

## What Changes

- The editor key starts the editor the way the solution key already opens a solution: Windows starts it from a short-lived, cleaned PowerShell child, so the editor inherits none of Torni's console handles and nothing it prints reaches Torni's screen.
- The editor still gets no Claude Code session markers, the folder path still reaches it as one argument that no shell reads as source, and a refusal is still reported with Windows' reason for the error code alone.
- No setting, key, wording or state file changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities
- `folder-actions`: the editor key's requirement gains "without anything the editor writes reaching Torni's screen", with a scenario for an editor that prints as it starts.

## Impact

- **Code:** `Source191.Torni.Infrastructure\Processes\Desktop.StartAsync` and the PowerShell it hands Windows (beside `OpenFileScript`); `IDesktop.StartAsync`'s contract text. `OpenInEditorHandler` and `EditorCommand` are unchanged.
- **Tests:** `DesktopShould` (Infrastructure, real processes) gains a fact that a started program's output never reaches Torni's own standard output and error.
- **Cost:** opening a folder starts one short-lived `pwsh` first, as opening a solution already does (a few hundred milliseconds before the editor appears).
- **Docs:** `.claude\rules\windows.md` and `security.md`, which describe how `Desktop` starts programs.

## Out of scope

- Multiple Claude accounts, which is a later change.
- Moving to .NET 11's `ProcessStartInfo.StandardOutputHandle` and `File.OpenNullHandle`, which would allow a direct start with output discarded; this project targets .NET 10.
- Any change to how solutions, links or problem reports are opened, which already go through the cleaned child.
