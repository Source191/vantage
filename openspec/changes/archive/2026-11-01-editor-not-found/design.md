# Design

## Context

`OpenInEditorHandler` asks `IEditorLocator` for a preset's executable and answers `EditorNotFound` when it is `Missing`, but takes a Custom executable as `Found` without looking, so `IDesktop.StartAsync`'s `Process.Start` throws a `Win32Exception` that `TableBody` turns into `StatusMessage.Failed(exception.Message)`. `OpenSolutionHandler` calls `IDesktop.OpenAsync`, whose PowerShell child (`OpenFileScript`) writes any exception's message to standard error and exits 1, which `Desktop` rethrows as an `InvalidOperationException` and the screen shows the same way. `EditorLocator.NewestRider` searches `Program Files\JetBrains\*` and Toolbox 1.x's `Toolbox\apps\Rider\<channel>\<build>`. `Settings.Validate` already takes a `folderExists` function for the roots, carried by `SettingsState.FolderExists`; `SettingsState.Browses` decides from the scope alone whether the dialog has Browse, and `SettingsDialog` adds the button once, as it opens.

## Goals / Non-Goals

**Goals:**
- Every editor and solution failure the user can fix on their machine is an outcome with words in `WordingOfOutcomes`, not an exception's message.
- The dialog's preset marks never delay the dialog opening or a keypress.

**Non-Goals:**
- Changing what `IDesktop.OpenAsync` does for URLs (pull requests, the problem report): their failures still reach the screen as now.
- Recording a missing editor in diagnostics beyond the existing outcome kind.

## Decisions

### A Custom executable is checked by the handler, not the locator
`OpenInEditorHandler` already holds `IFolders`; for Custom it answers `Missing` when `folders.FileExists(editor.Executable)` is false, so both cases reach the one `EditorNotFound` outcome and its words. `IEditorLocator.LocateAsync` stays about presets (its Custom arm keeps answering `Missing`, which the handler never asks for). Alternative: pass the whole `EditorChoice` to the locator, which would move a one-line file check into an adapter that would need another fake path.

### Start and open failures are typed exceptions declared in Ports
As `IGit` throws `GitException`, `IDesktop` gains two exceptions beside it: `ProgramNotStartedException` (thrown by `StartAsync` when `Process.Start` throws a `Win32Exception`, its message Windows' reason) and `NoApplicationException` (thrown by `OpenAsync` when Windows has no application for the file's type, carrying the extension). `OpenInEditorHandler` catches the first into `OpenInEditorOutcome.EditorNotStarted(Editor, Reason)`; `OpenSolutionHandler` catches the second into a new no-application case. Every other failure still throws as now. Alternative: return a result from both methods, which would make the pull request and problem report handlers handle cases that cannot arise for an https address. Showing Windows' reason is allowed by the wording rule (an exception's message only to report what failed, in a handler's failed outcome).

### No application is a distinct exit code from the open script
`OpenFileScript` exits 2, instead of 1, when the exception is a `Win32Exception` (directly or as the inner exception of PowerShell's `MethodInvocationException`) whose `NativeErrorCode` is 1155, `ERROR_NO_ASSOCIATION`. `Desktop.OpenAsync` maps exit 2 to `NoApplicationException(Path.GetExtension(file))`, so no error text is ever parsed. That .NET's shell start with `UseShellExecute` and no error dialog reports 1155 for an unassociated type is checked by the first task before anything relies on it; if it reports a different code (such as `SE_ERR_NOASSOC`, 31), the script tests for that one instead, and the fact goes in `.claude\rules\windows.md`.

### Rider in the user's Programs folder
`EditorFolders` gains `UserPrograms`, `%LOCALAPPDATA%\Programs` (the default of `FOLDERID_UserProgramFiles`, which .NET's `Environment.SpecialFolder` does not name, and the folder VS Code's user install already uses through `LocalAppData`). `NewestRider` adds every folder directly under it holding `bin\rider64.exe` to its candidates, whatever the folder is called, so a `Rider` or a versioned name both count; the newest is still chosen by `NumberAwareOrder` over the full path. Listing that folder's children (a few dozen at most) is cheap and happens only on `f` and when the dialog opens.

### Preset marks come from a background look as the dialog opens
`SettingsState` gains `Presets`, a `PresetSearch` closed hierarchy: `NotYetLooked` as it opens and `Looked(IReadOnlySet<EditorPreset> Missing)`. `SettingsDialog`, for the editor scope only, starts the four lookups in parallel off the UI thread (Visual Studio's runs vswhere) and applies `SettingsInput.PresetsLocated` on the UI thread when all have answered; a dialog closed before then ignores it. The labels come from `Wording.Describe(EditorPreset, PresetSearch)`, so `(not found)` lives in `Wording`. Explorer is never looked for (its answer is a path built from `%WINDIR%`), and Custom never marked. Alternative: look before opening the dialog, which would hold up Enter by up to vswhere's run time.

### Browse for a file is a third kind of Browse
`SettingsBrowse` gains `OneFile`, answered by `SettingsState.Browses` for the editor scope while `Text.Preset` is Custom, and `No` otherwise. `SettingsInput.FileChosen(path)` replaces `Text.Executable`; an empty answer (a cancelled picker) changes nothing. `SettingsDialog` reads `state.Browses` when F3 is pressed rather than once as it opens, and its Browse button follows `ShowsCustomEditor`'s visibility as the executable fields do. The picker is Terminal.Gui's `OpenDialog` in file mode, opening in the folder of the executable typed when it is a full path, and the second task checks its behaviour in 2.5.0 (file mode, an `.exe` filter, `FilePaths` for one file) by a spike, recorded in `.claude\rules\terminal-gui.md`.

### A missing Custom executable is a new settings problem
`Settings.Validate` takes a `fileExists` function beside `folderExists`, and `EditorChoice.Validate` refuses an executable that does not exist with `SettingsProblem.ExecutableMissing(path)`, checked after the full-path and batch-file rules so their reasons still win. `SettingsFile` keeps passing no existence check when it reads `config.json`, so a missing executable there is kept, as the spec requires.

### The not-found words
`EditorNotFound` reads "Could not find Rider; choose another in Open folder with on the Config tab (3)." and `EditorNotStarted` "Rider would not start: <reason>. Choose another in Open folder with on the Config tab (3)."; both are Failure tone. The solution case reads "No application opens .slnx files; install one, or choose one in Windows' Default apps." with Failure tone.

## Risks / Trade-offs

- [The `NativeErrorCode` for an unassociated type differs from 1155] → the first task checks it on this machine before the script relies on it; the fallback is the existing "Failed:" line, never a wrong message.
- [vswhere is slow or hangs while the dialog is open] → the look is off the UI thread and the dialog works unmarked until it answers; a closed dialog drops the answer.
- [A file chosen by the picker is a `.cmd` or `.bat`] → F5 checks it by the same rules as a typed one and refuses it.
- [Toolbox 2.1 lets the user move the install folder] → out of scope; Custom (now with Browse) covers it.
