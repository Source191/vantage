# Tasks

## 1. The start script

- [ ] 1.1 Red: `StartProgramScriptShould` names the script's lines for an executable and two arguments, each reaching the script only as Base64 of its UTF-8 bytes, `UseShellExecute` true, and a `Win32Exception` caught and written as its native error code alone. Green: add `StartProgramScript` beside `OpenFileScript` in `Adapters\Processes`. Verify the new test passes and `OneTypePerFileShould` still passes.

## 2. The adapter

- [ ] 2.1 Red: in `DesktopShould` (Infrastructure, real processes), add a fact that starts a small console program that writes to standard output and standard error through `Desktop.StartAsync`, and asserts that nothing it wrote reaches this test process's own captured output, while the program still ran (it writes a marker file). Green: change `Desktop.StartAsync` to run `StartProgramScript` through `IProcessRunner` with the PowerShell it was given. Verify the fact, `StartAProgramWithoutWaitingOrClaudeMarkers` and the other `DesktopShould` facts pass.
- [ ] 2.2 Red: `DesktopShould` facts that a refused start still throws `ProgramNotStartedException` with Windows' sentence for its error code alone (`ThrowProgramNotStartedWhenWindowsRefusesTheProgram` keeps its expectation), that PowerShell missing fails with the sentence naming it, and that a folder holding spaces, `&` and `'` arrives as one argument. Green: read the native error code from the child's standard error. Verify the facts pass and `OpenInEditorHandlerShould` is unchanged and green.
- [ ] 2.3 Refactor: update `IDesktop.StartAsync`'s and `Desktop`'s summaries, and the `Desktop` lines in `.claude\rules\security.md` and `windows.md`, to say editors are started through the cleaned child with ShellExecute and inherit no console. Verify `dotnet build -c Debug` with warnings as errors and a read-through of both rules files.

## 3. Release

- [ ] 3.1 Raise `Version` one patch or prerelease step in `src\Source191.Torni\Source191.Torni.csproj` and add a `### Fixed` line under Unreleased in `CHANGELOG.md` ending with the pull request number. Verify `Assert-VersionBumped.ps1` and `Assert-ChangelogUpdated.ps1` against `main`.
- [ ] 3.2 Manual (owner): with Open folder with set to VS Code, press `f` on a row in a local build (`tools\install-local.ps1`) and check that VS Code opens the folder and no text appears over Torni's screen. Verify by eye, and that the status line names the folder and the editor.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs` describes the code on `main`.
- Close issue #210 from that pull request.
