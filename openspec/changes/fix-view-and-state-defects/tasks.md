# Tasks

## 1. Domain rules

- [x] 1.1 `RemovalDecisionShould.SayWhetherARefusalCanBeForced`: lock and live-session refusals report `CanBeForced` false, dirty, unpushed and leftover-with-files refusals report true; add the field and make it pass
- [x] 1.2 `WorktreesViewShould.NotSuggestForceWhenTheRefusalCannotBeForced`: a locked row's footer has no force hint even when its reason text is reworded; replace `IsFinal` with the field and make it pass
- [x] 1.3 `SessionsViewShould.SelectASessionByIdWhenAskedTo`: a `SelectSession` input selects the matching row and leaves the selection alone when the id is absent; implement the reducer case
- [x] 1.4 `SessionsViewShould.StopCarriesTheSelectedRow`: pressing x yields a `StopSession` holding the row; change the effect record and make it pass
- [x] 1.5 Remove the `HasTranscript = true` default from `SessionRow`; verify `dotnet build -c Debug` lists every construction site and fix each by naming the value

## 2. Configuration and inventory

- [x] 2.1 `StateStoreShould.ReadAndWriteTheWorktreeBaseAsAnEnum`: `config.json` with `"defaultBase": "Head"` reads as `WorktreeBase.Head` and writes back the same text; change `VantageConfig` and make it pass
- [x] 2.2 `WorktreeInventoryShould.ReportTheSessionIdOfTheLiveSession`: a row carries the session id and name of the session running inside it; extend `WorktreeRow` and `LiveSessionIn`
- [ ] 2.3 `WorktreeInventoryShould.StopCountingLeftoverFilesAtTheLimit`: a leftover holding more files than the limit reports the limit, using a temporary folder; cap `CountLeftoverFiles` with the Domain limit
- [ ] 2.4 `RemovalDecisionShould.SayAtLeastTheLimitWhenTheCountWasCapped`: a leftover count at the limit is refused as "at least N files"; implement the wording in `RemovalDecision`
- [ ] 2.5 `SessionRowEnricherShould.SurviveOverlappingRefreshes`: two concurrent `EnrichAsync` calls over the same directories complete and agree; switch the cache to `ConcurrentDictionary`

## 3. State store

- [ ] 3.1 `StateStoreShould.KeepRecentRepositoriesWrittenByTwoWriters`: two stores over one folder each record a repository and both are read back, newest first, capped at ten; implement one record per file with a `UsedAt` timestamp
- [ ] 3.2 `StateStoreShould.KeepDescriptionsWrittenByTwoWriters`: the same for descriptions keyed by session id
- [ ] 3.3 `StateStoreShould.ImportLegacyRecentAndSessionsFiles`: a folder holding the old `recent.json` and `sessions.json` reads their contents once and removes the files; implement the import

## 4. Screens and wiring

- [ ] 4.1 `SessionsScreen` handles `StopSession` from the row it carries; `WorktreesScreen` and `LaunchWizardDialog` take `WorktreeBase`; `Program.cs` reduces a `SelectSession` input for `JumpToSession` instead of editing state; verify `dotnet build -c Debug`, `dotnet test test\Source191.Vantage.UnitTests -c Debug --no-build` and `dotnet format --verify-no-changes` pass
- [ ] 4.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal` and verify exit code 0
- [ ] 4.3 Update the README's worktrees-view key table (Enter goes to the session by id) and the "Where Vantage keeps things" section for the new state layout; verify the text matches the delta specs

## Workflow follow-up

- PR 1: task groups 1, 2 and 4 (the view, inventory and wiring fixes). No version bump.
- PR 2: task group 3 (one record per file and the legacy import). Independent of PR 1, so it can be developed alongside it. Bump `Version` to 0.3.0 because the state layout changes. Archive the change in this PR; `openspec archive fix-view-and-state-defects` merges the four delta specs.
- Both PRs must merge before `untangle-layers` PR 5 (state ports); `untangle-layers` PRs 3 and 4 need only PR 1.
