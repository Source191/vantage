# Proposal

## Why

Several results and answers use null to carry a second fact the reader has to know about: `LaunchOutcome.JobId` is null unless `Kind` is `Started`, and its `Session` is null when the supervisor had not listed the job yet; a dialog's null means "cancelled"; a validation helper's null means "valid"; `DetailRequests.Accept`'s null means "dropped". The owner decided on 31 October 2026 that Vantage's own models and results are null-free, with named cases or null objects per concept (never a generic `Option<T>`) and null kept to the line that receives it from outside Vantage. This change takes the results and answers.

## What Changes

- **Starting a session:** the port's `StartResult` becomes a hierarchy (`Started(JobId, Message)`, `WorkspaceNotTrusted(Message)`, `Failed(Message)`) and the `StartOutcome` enum goes; the job id is parsed in `ClaudeCli`, where the CLI's text arrives. `LaunchOutcome` becomes a hierarchy (`Started(JobId)` with `Listed(JobId, BackgroundSession)` and `NotListedYet(JobId)` under it, `WorkspaceNotTrusted`, `Failed(Message)`) and `LaunchOutcomeKind` goes; `StartSessionHandler`, `HereCommandRunner` and `Wording.Here` switch over the cases.
- **Leaving the console:** `ConsoleExit.Effect` is non-null, and `ConsoleScreen` starts with `Quit` as the effect it leaves with.
- **Row keys:** the `(ViewEffect? Effect, Footer Footer)` tuples in `WorktreesView` and `ConfigView` become a `RowDecision` (`Act(ViewEffect)`, `Explain(Footer)`).
- **The update key:** `UpdateVantage.Refusal` (a `Footer?`) becomes `UpdateVantage.Decide`, answering `Ask(ReleaseVersion)` or `Refuse(Footer)`.
- **Resume notes:** `ResumeNote(Name, Failure?)` becomes `NeverSentAMessage(Name)` and `Failed(Name, Failure)`.
- **Validation:** `EditorChoice.Problem` (a `SettingsProblem?`) becomes `EditorChoice.Validate`, answering `Valid(EditorChoice)` or `Refused(SettingsProblem)`; `Settings.Validate` drops its nullable local; `SessionName` and `ReportTitle` check their text without a nullable private helper. Their public `Validate` and `TryParse` are unchanged.
- **Dialogs:** `Confirm.Query` (`int?`) becomes `Confirm.Ask` answering `bool`; `NameDialog` and `TypedConfirmationDialog` answer a `TypedAnswer` (`Typed(Text)`, `Cancelled`); `ChooseSolutionDialog` answers a `SolutionChoice` (`Chosen(File)`, `NoneChosen`); `LaunchWizardDialog` and `SettingsDialog` answer their reducer's last effect (`WizardEffect`, `SettingsEffect`), as `ProblemReportDialog` already answers `ProblemReportEffect`, and `WizardResult` goes. In Core, `RenameConfirmed(Session, Name?)` splits into `RenameConfirmed(Session, Name)` and `RenameCancelled(Session)`, and `ForceConfirmed(Row, Text, Typed?)` into `ForceConfirmed(Row, Text, Typed)` and `ForceCancelled(Row)`.
- **Lookups:** `DetailRequests.Accept` answers `Show(DetailContent)` or `Dropped`, and its selected key is a private sentinel rather than null; `ProcessTree.Under` answers a `ProcessTreeLookup` (`Found(ProcessTree)`, `NotMeasurable`); `PullRequests.ForBranch` answers a `PullRequestLookup` directly.
- **Version:** one step above main when it merges (projected `0.19.0-preview.5`).
- `skip_specs: true`: no behaviour changes. Every footer, outcome sentence, exit code and dialog answer stays as it is. The one path whose words could differ cannot happen: the CLI's backgrounded line is matched as eight hex characters, which is exactly what `JobId` accepts, so a started session always has a job id (design.md).

Out of scope: the `!` operators and `ReleaseVersion.TryParse`/`RemoteRepository.Parse` (`null-suppressions`, which merges first); row and model state such as `BranchName?` (becoming `Head`), `SessionRow`'s live tab and `WorktreeRow`'s session (`null-free-rows`); view and Tui state, including `SettingsState.Problem`, `ProblemReportState.Problem` and its `TooLong` helper, `TableBody`'s detail fields and `KeyMapping.From` (`null-free-screens`); port answers and programs, including the terminal setup's nullable sounds and its `CheckSounds`, `IGit`'s queries, `OpeningPrompt` and System.CommandLine's strings (`null-at-the-boundary`); `BackgroundSession.ProcessId`, `LaunchPlan`'s place and the wizard's fixed directory (`null-free-sessions`); the architecture rule (`null-guard-rule`); guard clauses, which stay; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Ports and adapters:** `Ports\IClaudeCli.cs` (`StartResult`), `Infrastructure\Claude\ClaudeCli.cs`.
- **Domain:** `EditorChoice.cs`, `Settings.cs`, `SessionName.cs`, `ReportTitle.cs`, `ProcessTree.cs`, `PullRequest.cs` (`PullRequests`).
- **Application:** `Launch\LaunchService.cs`, `Launch\HereCommandRunner.cs`, `Actions\StartSessionHandler.cs`, `Actions\ResumeSessionsHandler.cs`, `Views\Screens.cs`, `Views\WorktreesView.cs`, `Views\ConfigView.cs`, a new `Views\RowDecision.cs`, `Views\UpdateEffects.cs`, `Views\SessionsView.cs`, `Detail\DetailRequests.cs`, `PullRequestCatalogue.cs`, `SessionRowEnricher.cs`, `WordingOfOutcomes.cs`.
- **Tui:** `Confirm.cs`, `NameDialog.cs`, `TypedConfirmationDialog.cs`, `ChooseSolutionDialog.cs`, `LaunchWizardDialog.cs`, `SettingsDialog.cs`, `WizardResult.cs` (removed), `DialogEffects.cs`, `ConsoleScreen.cs`, `TableBody.cs` (the `Accept` call only).
- **Tests:** `ClaudeCliShould`, `ClaudeContractShould` (the start facts), `LaunchServiceShould`, `StartSessionHandlerShould`, `HereCommandRunnerShould`, `WordingOfOutcomesShould`, `ScreenLoopShould`, `WorktreesViewShould`, `ConfigViewShould`, `UpdateVantageShould`, `ResumeSessionsHandlerShould`, `EditorChoiceShould`, `SettingsShould`, `SessionsViewShould`, `DetailRequestsShould`, `ProcessTreeShould`, `MemoryBreakdownShould`, `PullRequestsShould`.

## Order

Change 3 of the null programme, which merges one change after another: 1 shared-snapshot (0.19.0-preview.3), 2 null-suppressions (preview.4), 3 null-free-outcomes (this one, preview.5), 4 null-free-rows (preview.6), 5 null-free-observations (preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (preview.9), 8 null-free-port-answers (preview.10), 9 null-free-sessions (preview.11), 10 null-guard-rule (no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between.
