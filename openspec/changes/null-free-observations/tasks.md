# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, adding `test\Source191.Vantage.Infrastructure.Tests` where the task names an Infrastructure test. Re-read each file before changing it: `null-suppressions`, `null-free-outcomes` and `null-free-rows` will have merged first.

## 1. Domain: the new values and their rules

- [x] 1.1 `TabRegistrationShould`'s liveness facts read `Claim` (`new TabClaim.Claimed(4242, ProcessStart)` after `ClaimedBy`, `TabClaim.Pending` before) with every liveness answer unchanged: red, then `Domain\TabClaim.cs` and `TabRegistration.Claim` with `IsLive` switching on it green. Verify the facts pass and `TabRegistration.cs` declares no nullable field.
- [x] 1.2 `WorktreeHealthShould` builds observations with `WorktreeLock` and `GitPointer` (`Locked("maintenance")`, `Locked("")`, `RegisteredElsewhere(owner)`, `TargetGone`, `TargetExists`, `NoGitFile`) and expects the same kinds and reasons: red, then `Domain\WorktreeLock.cs`, `Domain\GitPointer.cs`, `WorktreeObservation` and `WorktreeHealth.From` green. Verify every fact passes with unchanged expectations.
- [x] 1.3 `ProcessTreeShould.KeepAChildItCouldNotMeasureAsUnmeasured` (and the existing facts reading `Measured`): red, then `Domain\ProcessMeasure.cs` and `ProcessTree.Usage` with `Build` translating `usageOf`'s answer green. Verify the facts pass with today's totals and counts.
- [x] 1.4 `ReleaseVersionShould`: a release's `Prerelease` is empty, a prerelease's is its text, and every comparison and `ToString` fact is unchanged: red, then `ReleaseVersion.Prerelease` as non-nullable text green. Verify the facts pass.
- [x] 1.5 `Domain\WaitingReason.cs` (`Nothing`, `Given(string Text)`) and `Domain\SessionMemory.cs` (`Measured(MemorySize Size)`, `NothingToMeasure`), first exercised by 3.2 and 4.1 (no fact of their own: they hold no rule). Verify the build.

## 2. Adapters

- [x] 2.1 `JsonFileStoreShould.ReadAPendingTabFileWrittenByTheCurrentVersion` (a file with `"processId": null` and `"processStartedAt": null` reads as `Pending`) and `WriteTheTabFileItAlwaysHas` (a written claimed tab's text has the same keys and values as `SessionTabJson`), with `RoundTripATabRegistration` and `ReadTabFilesWrittenByTheCurrentVersion` unchanged: red, then `Adapters\State\TabFile.cs` and `JsonFileStore` mapping through it, without `KeepTabOwnerAsSessionId`, green. Verify the four facts pass.
- [x] 2.2 `GitWorktreeListingShould`: a `locked` line gives `Locked` with git's reason or `""`, no line gives `Unlocked`, and a `prunable` line sets `IsPrunable`: red, then `GitWorktreeEntry`'s `Lock` and `IsPrunable` and `ParseBlock` green. Verify the facts pass and `GitWorktreeListing.cs` keeps no `string?`.
- [x] 2.3 `ClaudeAgentsListingShould`'s waiting facts expect `new WaitingReason.Given("permission prompt")` and `WaitingReason.Nothing`: red, then `Session.WaitingFor` as a `WaitingReason` and `ClaudeAgentsListing` translating the row green. Verify the facts pass.

## 3. Application: tabs and worktrees

- [ ] 3.1 `SessionTabsShould`, `OpenTabHandlerShould` (the claimed tab's `Claim` in place of `ProcessStartedAt`) and `OwnTabShould` (likewise) with their outcomes unchanged: red, then `SessionTabs`, `OpenTabHandler` and `OwnTab` on `TabClaim` green. Verify the facts pass and `SessionTestData.LiveTabAsync` still builds a live tab.
- [ ] 3.2 `WorktreeInventoryShould` (Infrastructure tests, real git) and `WorktreeInspectorShould`: a locked worktree, an orphan under `.claude\worktrees`, a broken folder and a worktree of a repository outside the roots keep their health and reasons: red on the compile, then `WorktreeInventory` (`PointerOf` in place of `GitDirOf`, `OwnerOf` and the `bool?`) and `WorktreeInspector` building the new observations green. Verify the facts pass and `WorktreeInventory.cs` keeps no `bool?` or `FolderPath?`.

## 4. Application: memory and waiting

- [ ] 4.1 `MemoryBreakdownShould`: an unmeasured child is labelled from an empty command line and adds nothing, as today: red if it fails, then `MemoryBreakdown.Of` switching on `ProcessMeasure` green. Verify the facts pass with today's figures.
- [ ] 4.2 `SessionRowEnricherShould`: a session whose process is measured has `SessionMemory.Measured`, one without a process or whose process has gone `NothingToMeasure`: red, then `SessionRow.Memory` as a `SessionMemory` and `MemoryOf` building it green. Verify the facts pass.
- [ ] 4.3 `WordingOfDetailShould`: "Memory" reads the figure with "(m for the breakdown)" or "nothing to measure", and "Waiting" reads the listing's reason in amber, "a first prompt" for a session needing input with no transcript, or "nothing" in plain: red, then `WordingOfDetail` on `SessionMemory` and `WaitingReason` green. Verify the facts pass with unchanged expected text and tones.

## 5. Version and the whole build

- [ ] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to one step above main (projected `0.19.0-preview.7`). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 5.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` pass; the smoke check (`dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>`) passes against a locally packed tool; `Get-ChildItem src\Source191.Vantage.Core\Domain -Filter *.cs | Select-String -Pattern 'string\? LockReason|GitFileTargetExists|OwnerRepository|ProcessUsage\? Usage|string\? WaitingFor|string\? Prerelease'` finds nothing; `openspec validate null-free-observations` passes.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
