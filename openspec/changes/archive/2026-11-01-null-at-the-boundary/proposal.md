# Proposal

## Why

The adapters and the command line hand Vantage's own code nulls that stand for a named fact: a program that is not on the PATH, a pull-request host not yet read, a bell sound left to Windows Terminal, a child that inherits Vantage's folder, a session name or prompt left off the command line, a tab whose terminal window is not known, a log that is turned off. Each null travels a layer or more before anything says what it means, and the compiler cannot make every reader ask. The owner decided on 31 October 2026 that null stays only on the line that receives it from outside Vantage, with a named case or a null object per concept and no generic `Option<T>`.

## What Changes

- **`ProgramLocation`** in `Ports` (`Found(string Path)`, `Missing`), following `ClaudeLocation`: `ProgramLocator.Locate` and `AzureCliExecutable.Locate` answer it, and `GitHubPullRequests`, `AzureDevOpsPullRequests`, `Desktop` and `WindowsTerminal` take it in place of a `string?`. `IEditorLocator.LocateAsync` answers it too. The words for a missing program (`PullRequestGap.CliMissing`, the two exception sentences, `OpenInEditorOutcome.EditorNotFound`) do not change.
- **`LookupHost`** in `Domain` (`Known(PullRequestHost Host)`, `NotRead`) in place of `PullRequestHost?` on `PullRequestLookup.Missing`, `Footer.NoPullRequest` and `PullRequestCatalogue`'s answers; `PullRequestHost` itself keeps its two members.
- **Terminal sounds:** `BellSound` in `Ports` (`TerminalDefault`, `File(string Path)`) on `TerminalProfiles`, and `SoundArgument` in `Application\TerminalSetup` (`Omitted`, `Given(string Path)`) on `TerminalSetupRequest`; `TerminalSetupRunner`'s sound check answers a `TerminalSetupProblem` with a `None` case. The fragment's JSON is byte for byte the same.
- **`WorkingFolder`** in `Ports` (`Inherited`, `At(string Path)`) on `ProcessRequest`, in place of `string? WorkingDirectory`; `ProcessRunner` alone turns `Inherited` into the empty string `ProcessStartInfo` reads as "inherit".
- **The command line:** `CommandLine.Build` translates System.CommandLine's `string?` values on the lines that read them: `vantage here`'s name becomes a `HereName` (`FolderName`, `Typed(string Text)`), its prompt the typed text (empty when left off, as the wizard's box is), and `setup-terminal`'s sounds `SoundArgument`s.
- **`OpeningPrompt`** in `Domain` (`None`, `Text(string Value)`) on `LaunchPlan` and `IClaudeCli.StartBackgroundAsync`, in place of `string?`; `IClaudeCli.ResumeBackgroundAsync` gains an overload without a name in place of its `SessionName?`.
- **Windows Terminal:** `WindowsTerminalArguments.ForTab` takes the profile as an argument list (empty, or `--profile` and the name); `TerminalFragment` requires its icon source; `TerminalWindow` in `Domain` (`Owning(int ProcessId)`, `Any`) replaces `IWindowsTerminal.FocusTabAsync`'s `int?`; `OwnTab.ClaimAsync` answers `OwnTabClaim` (`Claimed(TabColour Colour)`, `Unregistered`) and `ITerminalConsole.ShowOwnTab` has one overload with a colour and one without.
- **`VANTAGE_LOG`:** `DiagnosticLogSetting` in `Application\Diagnostics` (`Off`, `File(string Path)`), read once from the variable and handed to the diagnostic log and `SetupFactsReader`.
- **`CLAUDE.md`:** the sentence on `GitHubPullRequests`, `WindowsTerminal` and `Desktop` taking "a nullable path" names `ProgramLocation`.
- **Version:** one step above main when it merges (projected `0.19.0-preview.9`).
- `skip_specs: true`: no behaviour changes. Every message, key, tab, file and argument list is the same as before.

Out of scope: the port answers that are absences (`IVantageConfig.ReadConfigAsync`, the update-check record and `SetupState.Checked.Newest`, `ICrashFile.ReadAsync`, `IClaudeCli.VersionAsync`, `IPackageVersions.ListAsync`), which are `null-free-port-answers`, split from this change to keep each pull request reviewable; `ClaudeStates.From`, which is the adapter's own reader of the CLI's words and so already the line that receives them; `Session.WaitingFor` and `TabRegistration`'s own shape (`null-free-observations`); `VantageIcon.Fit`'s Sixel parameter (`null-free-screens`); `EditorChoice.Executable` (a Domain fact, not a boundary); `BackgroundSession.ProcessId`, `LaunchPlan`'s place and the wizard's fixed directory (`null-free-sessions`); the architecture rule (`null-guard-rule`); what stays nullable by decision (the six `IGit` queries, `IProcessRegistry`'s reads, `FolderPath.Parent`, the JSON data objects, the `TryParse` outs, `Equals(T?)`, `Program.cs`'s `LocateGitAsync` and `LocateClaudeAsync`, which exit when they answer null); guard clauses, which stay; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Domain:** new `LookupHost`, `OpeningPrompt`, `TerminalWindow`; `PullRequestLookup`, `TabRegistration.OwningTerminal`.
- **Ports:** new `ProgramLocation`, `BellSound`, `WorkingFolder`; `IEditorLocator`, `IClaudeCli`, `IWindowsTerminal`, `ITerminalConsole`, `IProcessRunner` (`ProcessRequest`), `TerminalSetupPorts` (`TerminalProfiles`).
- **Application:** `CommandLine`, `CommandActions`, `Launch\HereCommand`, `HereCommandRunner`, `LaunchPlan`, `LaunchService`, `TerminalSetup\TerminalSetupRequest` and `TerminalSetupRunner`, `PullRequestCatalogue`, `Views\Footer`, `WordingOfPullRequests`, `Wording` (`SoundText`), `EditorCommand`, `Actions\OpenInEditorHandler`, `OpenTabHandler`, `OwnTab`, `AdoptSessionHandler`, `RenameSessionHandler`, `ResumeSessionsHandler`, `ScreenLoop`, `SetupFactsReader`; new `Diagnostics\DiagnosticLogSetting`, `Launch\HereName`.
- **Adapters:** `Processes\ProgramLocator`, `ProcessRunner`, `Desktop`, `EditorLocator`; `PullRequests\GitHubPullRequests`, `AzureDevOpsPullRequests`, `PullRequestHosts` (`AzureCliExecutable`); `Terminal\WindowsTerminal`, `TerminalFragment`, `TerminalConsole`, `UiAutomationTabs`; `Claude\ClaudeCli`, `ClaudeExecutable`; `Git\GitCli`.
- **Composition:** `Program.cs`.
- **Tests:** the matching `...Should` classes in Core.Tests and Infrastructure.Tests, and `FakeEditorLocator`, `FakeProcessRunner`, `FakeWindowsTerminal`, `FakeTerminalConsole` in TestDoubles; about 60 test lines build a `ProcessRequest`.
- **Docs:** `CLAUDE.md` (one sentence); `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

Change 7 of the null programme, which merges one change after another in this order: 1 shared-snapshot (0.19.0-preview.3), 2 null-suppressions (preview.4), 3 null-free-outcomes (preview.5), 4 null-free-rows (preview.6), 5 null-free-observations (preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (this one, preview.9), 8 null-free-port-answers (preview.10), 9 null-free-sessions (preview.11), 10 null-guard-rule (no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between.
