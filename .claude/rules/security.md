---
paths:
  - "src/Source191.Vantage.Infrastructure/Processes/**"
  - "src/Source191.Vantage.Infrastructure/PullRequests/**"
  - "src/Source191.Vantage.Infrastructure/Terminal/**"
  - "src/Source191.Vantage.Infrastructure/Diagnostics/**"
  - "src/Source191.Vantage.Core/Application/Diagnostics/**"
  - "src/Source191.Vantage.Core/Application/Reporting/**"
  - "**/TabScripts*.cs"
  - "**/PowerShellScript*.cs"
  - "**/BatchFileArgument*.cs"
  - "**/RemoteRepository*.cs"
  - "**/Editor*.cs"
  - "**/WorktreeRemover*.cs"
  - "**/LeftoverFiles*.cs"
  - "**/RemoveWorktreeHandler*.cs"
  - "**/DeleteSessionHandler*.cs"
  - "**/SessionId*.cs"
  - "**/ProcessIdentity*.cs"
  - "**/ProgramLabel*.cs"
  - "**/PathFolders*.cs"
  - "**/ProgramLocat*.cs"
  - "**/*Executable*.cs"
  - "**/SettingsProblem*.cs"
  - "**/LaunchPlanBuilder*.cs"
  - "**/HereCommand*.cs"
  - "**/GitPointer*.cs"
  - "**/WorktreeHealth*.cs"
  - "**/WorktreeInventory*.cs"
  - "**/TerminalTab*.cs"
  - "**/TabRegistration*.cs"
  - "**/*Privacy*.cs"
---

# Security and diagnostics

The detail behind the security and privacy invariants in `CLAUDE.md`.

## Programs and arguments

- Every program Vantage starts is found once as it starts and run by its full path: `Adapters\Processes\ProgramLocator` searches the PATH folders (only full paths: `PathFolders.Searchable` is the one filter every PATH search applies, az.cmd's and the VS Code shim's included) for the `.exe` only (CreateProcess would otherwise look in the parent's current directory before the PATH for a bare name, so a `git.exe` planted in the folder Vantage was started from would run; `ProgramLocatorShould` and the fact in the security review of 2026-10-29 cover it).
- `ClaudeExecutable.Locate` answers a `ClaudeLocation` (`Found`, `BatchFileOnly`, `Missing`), `Application\RequiredPrograms.Check` turns it and git's location into both paths or `Wording.ClaudeIsABatchFile`, `ClaudeNotFound` or `GitNotFound` (Claude Code's first), and the tool's `Composition\LaunchParts` prints that sentence and exits 1; `ProgramLocator.Locate` answers a `Ports\ProgramLocation` (`Found`, `Missing`), and `GitHubPullRequests`, `AzureDevOpsPullRequests` (through `AzureCliExecutable.Locate`), `WindowsTerminal` (wt.exe and the tab's pwsh.exe, which `WindowsTerminalArguments` names by its full path) and `Desktop` take a `ProgramLocation` and report a `Missing` one where it is needed.
- A folder reaches an editor as one element of an argument list built by `EditorCommand`, never through a command line or cmd.exe, and the locator never answers a `.cmd` shim. `IEditorLocator` finds a preset's real executable as a `ProgramLocation` (Visual Studio's through `vswhere -latest -property productPath`, run by its full path); `IDesktop` opens a file with its associated application or starts a program detached with `InheritedEnvironment` applied; `IClipboard` is implemented over Terminal.Gui's `app.Clipboard` by `Tui\ScreenClipboard`, attached while a table screen runs.
- No batch file is ever given text from a person, a repository or a listing: a custom editor's executable must be a full path (`SettingsProblem.ExecutableNotFullPath`), and `Domain\BatchFileArgument.IsSafe` (letters, digits, space, dot, underscore, hyphen, slash) is the one rule for what `az.cmd` may be given, applied to the remote names in `RemoteRepository` and to the branch in `AzureDevOpsPullRequests` (`PullRequestGap.BranchNotGivenToAz`).
- User text must never be interpolated into PowerShell source: a tab's script reaches `wt.exe` Base64-encoded for [`pwsh -EncodedCommand`](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_pwsh), and every value inside it (paths, ids, folder names, whole messages) is built by `TabScripts` and goes through `PowerShellScript.Quote`, which doubles all five characters [PowerShell reads as a single quote](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_quoting_rules) (`'` and U+2018 to U+201B). `PowerShellQuotingShould` checks this against real `pwsh`. Every semicolon reaching `wt.exe` is escaped by `WindowsTerminalArguments` (`windows.md`).
- An opening prompt that starts with `-` is `LaunchProblem.PromptLooksLikeAnOption`.
- Release notes come from outside Vantage (nuget.org's nuspec, the changelog beside the tool): `Domain\ReleaseNotes.TryParse` drops every control character as it reads each line and keeps only headings and one-line entries, the nuspec is read with DTD processing prohibited and no resolver, the dialog shows the lines as text, and the release page's URL is built from the version alone by `Application\ReleasePage`.

## Processes and identities

- A listed process id is ended only while `Domain\ProcessIdentity.StillTheSession` holds (the process started no later than the session's listed start, plus five seconds), as `TabRegistration.IsLive` already checks for tabs; close-empty lists such sessions in `Gone` and adopt answers `ProcessGone`.
- A process's command line carries secrets, so `ProgramLabel` reduces one to a package or executable name and nothing else is kept, logged or shown.
- A `SessionId` is ASCII letters, digits and hyphens with a letter or digit first, so no id from the listing can be read as an option or hold a path separator; `TerminalTab` walks up only through parents that started no later than their child, so a reused parent id never names another tab's shell.

## Deleting folders

- A leftover folder is deleted only by `WorktreeRemover`, only strictly inside a repository root or the worktree root (`RemovalReason.OutsideTheRoots`, never forced), and `RemoveWorktreeHandler` and `DeleteSessionHandler` read its existence and files (`Application\LeftoverFiles`) from the disc as the key is pressed.
- A worktree whose `.git` file points at a live registration of a repository outside the roots is `WorktreeHealthKind.Elsewhere` (`HealthReason.RepositoryOutsideRoots`), a real worktree to the keys but refused removal with `RemovalReason.RepositoryOutsideRoots`, never deleted as a leftover; so is any `.git` file whose target exists (a bare repository's worktree, a `--separate-git-dir` checkout), its repository read from the gitdir's shape and a relative gitdir from the `.git` file's folder.

## Diagnostics

- Diagnostics live in `Application\Diagnostics`: one `DiagnosticLog` (the latest 500 events in memory, each also written to the `VANTAGE_LOG` file when it is set), `ProcessCounts` filled by `RecordedProcessRunner` around the one process runner, `RefreshCounts` (timed in `ConsoleLoads` and `PullRequestCatalogue`) and `StallCounts` (filled by `Heartbeat`), the three held in one `PerformanceCounts` (each a `DurationSample`: runs and longest over every run, median and 95th percentile over the latest 1,000) and written to the sinks every 10 minutes by `PeriodicCounts` while `VANTAGE_LOG` is set and at exit, and `CrashRecorder` writing `%LOCALAPPDATA%\Vantage\last-crash.log`.
- A new fact is a new `DiagnosticEvent` case whose members are numbers, durations, versions, enums, `CodeName` (built only from a `Type` or `MethodBase`), `ExceptionSummary` (types and frames, never the message) or `ChildCommand` (subcommand words from `ChildCommandWords` only), which `Architecture\DiagnosticEventsShould` enforces, and `DiagnosticsPrivacyShould` runs the daily flows over Northwind fixtures and fails on any fixture value in the output. Never log an effect's `ToString()`, an exception's message, program output or anything typed.
- The rendered lines (`DiagnosticLine`) are the one text outside `Wording`: they are a maintainer's format, not interface text. Terminal.Gui's logger is `TerminalGuiBridge`, which keeps only the level and an exception summary, Warning and above, because its messages can quote typed or pasted input.
