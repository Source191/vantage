# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, adding `test\Source191.Vantage.Infrastructure.Tests` where the task names an Infrastructure test. Re-read each file before changing it: `null-suppressions` and `null-free-outcomes` will have merged first.

## 1. Domain: the new values

- [ ] 1.1 `HeadShould.KeepTheBranchOnABranch` (and that `Detached` and `Unknown` are distinct values): red, then `Domain\Head.cs` (`OnBranch(BranchName Branch)`, `Detached`, `Unknown`) green. Verify the facts pass.
- [ ] 1.2 `RemovalDecisionShould`'s live-session facts build `WorktreeRemovalFacts` with `new WorktreeSession.Running(SessionId.From(...), "fix-123")` and `WorktreeSession.None`, expecting the same decisions: red, then `Domain\WorktreeSession.cs`, `WorktreeRemovalFacts.Session` in place of `LiveSessionName` and `RemovalDecision`'s `{ Session: WorktreeSession.Running running }` arm green. Verify every `RemovalDecisionShould` fact passes with unchanged expectations.
- [ ] 1.3 `Domain\LiveTab.cs` (`None`, `Open(TabColour Colour)`), first exercised by 4.2 (no fact of its own: it holds no rule). Verify the build.
- [ ] 1.4 `WhereLabelShould` builds labels with `WhereLabel.For(repositoryName, folderLabel, inRepository)` and expects the same texts and fits as today; the facts that pass a branch are deleted with the ability, each named in the commit message: red, then `WhereLabel` as `Outside(Place)` and `Inside(Repository, Place)` with no branch green. Verify every remaining fact passes with unchanged expected text, `WhereLabel.cs` declares no nullable reference type and has no `(`-bracketed branch text, and `CLAUDE.md`'s `WhereLabel` sentence reads "repository and place".
- [ ] 1.5 `RowFilterShould` passes non-nullable fields (a former null field dropped from the list) with the same answers: red, then `RowFilter.Matches(IReadOnlyList<string>)` green. Verify the facts pass.

## 2. Port and adapter: git's head

- [ ] 2.1 `GitWorktreeListingShould.ReadABranchLineAsOnBranch`, `ReadADetachedLineAsDetached` and `ReadABranchItCannotNameAsUnknown`, and the existing facts reading `Commit` where they read `Head`: red, then `GitWorktreeEntry(Path, Commit, Head, IsMain, LockReason, PrunableReason)` and `ParseBlock` translating the lines green. Verify `GitWorktreeListingShould` passes and `GitWorktreeListing.cs` keeps no `BranchName?`.

## 3. Application: worktree rows

- [ ] 3.1 `WorktreeTestData.Row` takes a `Head` (default `OnBranch("fix-123")`) and a `WorktreeSession` (default `None`); `WorktreeRow` holds `Head Head` and `WorktreeSession Session` in place of `Branch`, `SessionId` and `SessionName`, and `Facts` gives the branch for `OnBranch` and the folder name otherwise (refactor step through the builder: no expectation changes). Verify `RemoveWorktreeHandlerShould`, `WorktreesViewShould` and the other worktree facts pass unchanged.
- [ ] 3.2 `PullRequestCatalogueShould`: `Find` with `new Head.Detached()` and `new Head.Unknown()` answers `Missing(PullRequestGap.NoBranch)` as `null` did: red, then `Find(FolderPath, Head)` green. Verify the catalogue's facts pass.
- [ ] 3.3 `WorktreeInventoryShould` (Infrastructure tests, real git): a detached worktree's row has `Head.Detached`, a leftover's `Head.Unknown`, and only `OnBranch` rows are refreshed in the catalogue: red, then `WorktreeInventory` (the `row.Branch!` goes, `WithPullRequest` and `WithLiveSession` build the new values) and `DeleteSessionHandler`'s row green. Verify the inventory facts pass and `WorktreeInventory.cs` has no `!`.
- [ ] 3.4 `DetailLoaderShould`: a `Detached` row's merge state is `NotApplicable` as a branchless row's was: red, then `DetailLoader` matching `OnBranch` green. Verify the facts pass.
- [ ] 3.5 `RemoveWorktreeHandlerShould.KeepTheFolderNameAsTheBranchOfADetachedWorktree` (the existing stand-in, now named): red if the switch is missing, then `WorktreeRemover` switching on the head green. Verify the handler's facts pass with today's outcomes.
- [ ] 3.6 `WorktreesViewShould`'s jump facts use `new WorktreeSession.Running(...)` and `WorktreeSession.None`, and its filter fact matches a branch and skips a detached row's missing one: red, then `WorktreesView`'s jump and filter fields on the new values green. Verify the view's facts pass with unchanged footers and effects.

## 4. Application: session rows

- [ ] 4.1 `SessionTestData.Interactive` and `Background` build rows with `new Head.OnBranch(...)` and `LiveTab.None`; `SessionRow(Session, Repository, Head, LiveTab, HasTranscript)` loses `TabColour`, and `ListSelection`'s filter fields become non-nullable (refactor step through the builders and `SessionsView`'s and `ConfigView`'s filter lines). Verify `SessionsViewShould`, `ConfigViewShould` and `ListSelectionShould` pass unchanged.
- [ ] 4.2 `SessionRowEnricherShould`: a session with a live tab has `new LiveTab.Open(colour)`, one without `LiveTab.None`, one outside a repository `Head.Unknown`, one on a branch `OnBranch`: red, then `SessionRowEnricher` building them (its `Location` holds a `Head`) green. Verify the enricher's facts pass.
- [ ] 4.3 `OpenTabHandlerShould` raises `OpenTabForSession(session, new LiveTab.Open(...))` to switch and `LiveTab.None` to open: red, then `OpenTabForSession(BackgroundSession, LiveTab)`, `SessionsView`'s Enter and `OpenTabHandler` green. Verify the handler's and `SessionsViewShould`'s Enter facts pass with today's outcomes.
- [ ] 4.4 `WordingOfDetailShould`: the session pane reads "Branch none" for `Detached` and `Unknown`, "Tab open, <colour>" for `Open`, and the worktree pane's branch line reads "none" for both: red, then `WordingOfDetail` switching on `Head` and `LiveTab` green. Verify the facts pass with unchanged expected text.

## 5. Tui

- [ ] 5.1 `SessionsScreenDefinition` (TAB cell paint and swatch on `LiveTab.Open`, REPOSITORY as `WhereLabel.Outside` or `Inside`), `WorktreesScreenDefinition` (WORKTREE through `WhereLabel.For` with no branch, SESSION from `WorktreeSession`) and `ResumeChecklistDialog` ("-" for a head that is not `OnBranch`) (refactor step: `Tui` has no test project). Verify the build, and the smoke check: `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>` against a locally packed tool passes.

## 6. Version and the whole build

- [ ] 6.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to one step above main (projected `0.19.0-preview.6`). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 6.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` pass; `Get-ChildItem src -Recurse -Filter *.cs | Select-String -Pattern '(?<!out )BranchName\? (Branch|branch)\b|HasLiveTab|TabColour\? TabColour|LiveSessionName|SessionName\?'` finds nothing (the `TryParse` outs keep `out BranchName?`, the house pattern); `openspec validate null-free-rows` passes.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
