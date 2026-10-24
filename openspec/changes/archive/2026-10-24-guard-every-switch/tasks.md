# Tasks

## 1. Compiler exhaustiveness for closed enums

- [x] 1.1 Add CS8524 to `NoWarn` in `Directory.Build.props` with a comment stating the stance (enums hold only named members, so a switch expression that names every member is exhaustive and a new member fails with CS8509); verify `dotnet build -c Debug` still passes with zero warnings
- [x] 1.2 `Domain\Sessions.cs` `AttentionRank`: replace `_ => 6` with `DisplayStateKind.Unknown => 6`; verify `SessionsShould` passes and the build is clean
- [x] 1.3 `Application\Views\ListSelection.cs` `EditFilter`: replace `_ => this` with `KeyName.Up => this` and `KeyName.Down => this`; verify the view tests that cover filter editing pass and the build is clean
- [x] 1.4 `Application\WordingOfOutcomes.cs` `Here`: replace `_` with `LaunchOutcomeKind.Failed`; verify `WordingOfOutcomesShould.SayWhyVantageHereCouldNotStart` passes and the build is clean
- [x] 1.5 `Application\Actions\RemoveWorktreeHandler.cs` `RemoveAsync`: replace `_` with `RemovalOutcome.RemoveWorktree`; verify `RemoveWorktreeHandlerShould` passes and the build is clean
- [x] 1.6 `RemoveWorktreeHandler.RemoveThroughGitAsync`: convert the `BranchAction` switch statement to a switch expression over the (`BranchAction`, `BranchName?`) tuple with arms for `(Delete, BranchName)`, `(Ask, BranchName)` and `(None, _) or (_, null)`, the delete-then-report arm extracted to a private async method; verify `RemoveWorktreeHandlerShould` passes (the branch-deleted, branch-kept and plain-removed cases) and the build is clean
- [x] 1.7 Prove the guard once: add a temporary member to `BranchAction`, confirm `dotnet build` fails with CS8509 naming `RemoveWorktreeHandler.cs`, then remove the member; verify the build passes again (record the failing diagnostic line in the pull request description)

## 2. Enum switches in the Tui layer

- [x] 2.1 `Tui\Palette.cs` `ForState`: replace `_ => Grey` with `DisplayStateKind.Stopped => Grey` and `DisplayStateKind.Unknown => Grey`; verify the build is clean (the colours are asserted by the session-listing spec's manual check, not a unit test; no value changes)
- [x] 2.2 `Tui\WorktreesScreenDefinition.cs` `RowColour`: replace `_ => Palette.Red` with `WorktreeHealthKind.Orphan => Palette.Red` and `WorktreeHealthKind.Broken => Palette.Red`; verify the build is clean

## 3. Dialog completeness fact

- [x] 3.1 `test\Source191.Vantage.Core.Tests\Tui\DialogEffectsShould.HaveAFlowForEveryDialogEffect`: a theory over every non-abstract `IDialogEffect` implementer in the `Core` assembly asserting `DialogEffects.Handles(type)`, plus `NotClaimABackgroundEffect` asserting `Handles(typeof(StopSession))` is false; confirm it fails to compile because `Handles` does not exist
- [x] 3.2 `Tui\DialogEffects.cs`: add the static `FrozenDictionary<Type, Action<DialogEffects, IEffectContext, ViewEffect>>` with one entry per existing `case`, `public static bool Handles(Type effectType)`, and rewrite `Show` as a lookup with the `NoDialogFor` fallback; verify both facts pass and `dotnet test -c Debug --no-build` at the solution is green
- [ ] 3.3 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal`; verify exit 0, then a manual pass over each dialog key on both views (`?`, `n`, `r`, `d`, `C`, `R` on sessions; `s`, `d`, `D` on worktrees) confirming each opens the same dialog as before; verify no footer text changed

## 4. DisplayState observation

- [ ] 4.1 Start a background session, let it complete a task (state `done`), attach and send it a new prompt, and run `claude agents --json` while it works and again after it finishes; record every `status`/`state` pairing seen, with the CLI version, as a bullet in CLAUDE.md's "Claude Code CLI facts"; verify the bullet is present and, if `busy` paired with `done`, `blocked` or `failed`, that a follow-up OpenSpec change against `session-listing` has been proposed and is named in the bullet

## 5. Documentation and release

- [x] 5.1 CLAUDE.md code rules: name `DialogEffectsShould` beside `EffectDispatcherShould` and `ScreenLoopShould` in the "a new key" recipe, and add one sentence on the enum stance (no discard arm on a closed enum; CS8524 is suppressed so CS8509 fails the build on a new member); verify `dotnet format --verify-no-changes` and `openspec validate guard-every-switch` pass
- [x] 5.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` from 0.4.1 to 0.4.3 (main reached 0.4.2 while this change was open); verify `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` passes

## Workflow follow-up

- One pull request for the whole change; archive the change in it.
- If task 4.1 finds a `busy` pairing that should read as working, the follow-up change modifies the `session-listing` spec and the mapping wherever it lives when it lands (`Domain\DisplayState.From` today, `Adapters\Claude\ClaudeStates` after `consolidate-helpers`).
- Archived on 2026-10-24 with 3.3 (the manual dialog pass) and 4.1 (the busy pairing observation) outstanding: both need a terminal and run against the released 0.4.3; a busy pairing with done, blocked or failed becomes a change against session-listing.
