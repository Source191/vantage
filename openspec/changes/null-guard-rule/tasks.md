# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. Start this change only after null-free-sessions has merged, on a branch from main. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. The probe

- [x] 1.1 `NullFreeModelsShould.SeeEveryKindOfNullable`: over a private nested probe type declared in the test file (a nullable and a non-nullable property, parameter and return, a `List<string?>`, an `int?`, and a positional record), the member finder reports exactly the nullable ones and none of the record's compiler-generated members. Red, then the finder (namespace filter aside) green. Verify the fact passes and fails when one probe annotation is flipped by hand.
- [x] 1.2 `NullFreeModelsShould.ExemptTheHouseShapes`: a probe with `TryParse(string?, out T)` carrying `[MaybeNullWhen(false)]`, `Equals(T?)` and `CompareTo(T?)` reports nothing. Red, then the exemptions green. Verify the fact passes and a `TryParse` without the attribute is reported.

## 2. The rule over Core

- [x] 2.1 `NullFreeModelsShould.FindTheModelTypes`: the type finder covers `Domain`, `Ports` and `Application` and their sub-namespaces, public and nested-public types only, and finds a known type from each (`SessionName`, `IGit`, `CurrentSettings`, `SessionsView`). Red, then green. Verify the fact passes.
- [x] 2.2 `NullFreeModelsShould.HoldNoNullableMember`: run over Core with an empty allow-list and record what it reports. Every reported member is either named as kept in a sibling change's design (add it to `Kept` with a one-line reason) or a gap (list it in this task and ask the owner: allow with a reason, or a follow-up change). Green once every report is allowed or the owner has decided. Verify the fact passes, `Kept` has a reason per entry, and the gaps and the owner's decisions are recorded here.
  - Recorded 1 November 2026 (raw reports in the job's `tmp\null-guard-rule`): 91 members reported with an empty allow-list. 25 were the finder's own blind spot, `NullabilityInfoContext` reading an unconstrained type parameter as nullable with or without `?` (fixed with a probe: at a type parameter the member's own nullable flag decides); 2 were Try-pattern `out` parameters (`JobId.TryParse(SessionId, ...)`, `ConfigView.TryPathOf`), now exempt by rule. The other 64 are in `Kept`, one reason each.
  - Named as kept by a sibling's design or hand-over notes: the six `IGit` queries, `IProcessRegistry`'s three, `ProcessTree.Under`, `TerminalTab`'s two, `FolderPath.Parent`, `TextValues.Parsed`, `VantageConfig`, `EditorSetting`, `EditorChoice.FromFile`, `DiagnosticLogSetting.Read`, `Wording.ClaudeBellPlan`, the selection (`ListSelection`, `TableTitle`, the three view states' keys and `Selected`), `WizardState.SelectedIndex` and the `DiagnosticEvent` optionals.
  - Gaps no sibling handled: the owner was not available, so each is allowed with an honest reason, as the caller of this overnight run directed, and listed in the pull request as a candidate for a follow-up change: `WorktreeRow.LastCommitAt` and `WorktreeCounts.LastCommitAt`, `IListViewState.Banner` and the three view states' `Banner`, `SettingsState.BrowseFrom`, `SessionsViewState.SelectedSessionId` (with `SelectedPath` and `SelectedKey`, the same shape), `TabKeys.For`, `SessionActivity.LastPromptedAt` and `SessionDetail.LastPromptedAt`, `WorktreeDetail.Created`, `Attempt.RunAsync`'s `what`, `ProcessIdentity.StillTheSession`'s `processStartedAt`, `Settings.Resolve`'s five optional parameters, `ToolVersion.FromInformational`, `UpdateCheckOptOut.Applies` and `TerminalGuiBridge`'s three `ILogger` members.
- [x] 2.3 `NullFreeModelsShould.KeepNoStaleEntry`: every `Kept` entry names an existing member that is still nullable. Red with a deliberately stale entry, then green with it removed. Verify the fact passes.

## 3. The rule in words, and the whole build

- [x] 3.1 `CLAUDE.md`'s code rules gain the sentence in proposal.md, and the layering sentence names `NullFreeModelsShould` beside `LayeringShould`, `TextValuesShould` and `DiagnosticEventsShould`. Verify by reading both against design.md and checking no em dash was introduced.
- [ ] 3.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate null-guard-rule` all pass; `git diff --stat origin/main` shows nothing under `src\` (no version raise needed).

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
