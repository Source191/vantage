## 1. Script

- [x] 1.1 `OpenFileScriptShould.HoldNoPartOfThePathInClear`, `DecodeToTheSamePath` (a path with `'`, `&`, `%`, `é` and a URL), `ReportAFailureOnStandardErrorAndExitOne`: add `Adapters\Processes\OpenFileScript.For(string target)`; verify the facts pass

## 2. Adapter

- [ ] 2.1 `DesktopShould.OpenAFileWithoutClaudeMarkers` (a `.cmd` file writing its environment to a report, `CLAUDECODE` set in the test process, a name holding `' ( ) & ^ % !` and `é☃`; the report lacks every marker and has PATH): give `Desktop` an `IProcessRunner` and `OpenAsync` the PowerShell child; verify the fact passes and fails first against the ShellExecute call
- [ ] 2.2 `DesktopShould.ThrowWhenTheFileIsGone` (the message says why, no dialog, the call returns): throw `InvalidOperationException` with the child's message on a non-zero exit; verify
- [ ] 2.3 Pass the process runner to `new Desktop(...)` in `Program.cs`; verify `dotnet build -c Debug`

## 3. Wording of the contract

- [ ] 3.1 Update the `IDesktop.OpenAsync` and `Desktop` summaries (markers left out of an opened application's environment, the child that does it); verify the build

## 4. Release

- [ ] 4.1 Raise `Version` to the next patch above `origin/main`; verify `Assert-VersionBumped.ps1`
- [ ] 4.2 Archive this change with its deltas to `folder-actions` and `pull-requests`; verify `openspec validate --all`
- [ ] 4.3 Gate: build, tests, `dotnet format --verify-no-changes`, the version check; the manual check (an IDE launched by `v` from a Vantage started in a Claude Code session: no markers in its environment and its window comes forward) stays with the pull request
