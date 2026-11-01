# Tasks

Each task is one red-green-refactor cycle or smaller, with a commit per step; the fact is named before the fake or fixture it proves. Both test projects change, so verify each task with `dotnet build -c Debug` (warnings are errors) and `dotnet test -c Debug --no-build` at the solution unless it says otherwise. Fixtures stay Northwind.* and `C:\Users\me`.

## 1. Deadlines and no sleeping (T-04, T-05)

- [ ] 1.1 `FakePullRequestsShould.CompleteWhenCalledAsyncWhenTheCountIsReached` (two calls; `WhenCalledAsync(2)` completes, `WhenCalledAsync(3)` does not, and `WhenCalledAsync(1)` asked afterwards is already complete). Red, then green with the waiter list in `test\Source191.Vantage.TestDoubles\FakePullRequests.cs` completed under `gate`. Verify the facts pass.
- [ ] 1.2 `FakePullRequestsShould.HoldEachCallAndReleaseThemOneAtATime` (hold, two calls, `ReleaseOne()` completes the first only, `Release()` the second). Red, then green with per-call completion sources and `ReleaseOne()`. Verify the facts pass and `HoldACallOpenUntilReleased` still passes.
- [ ] 1.3 `HoldACallOpenUntilReleased` awaits `hosts.WhenCalledAsync(1).WaitAsync(TimeSpan.FromSeconds(5), ...)` in place of `Task.Delay(50)` (`FakePullRequestsShould.cs:19`). Verify the fact passes.
- [ ] 1.4 `PullRequestCatalogueShould`: delete `WaitForCallsAsync` (`:177-183`); `CallAgainAfterAMinuteKeepingTheOldAnswerMeanwhile` and `NotStartASecondCallWhileOneRuns` await `hosts.WhenCalledAsync(n).WaitAsync(5 s)`; `RunAtMostFourLookupsAtOnce` becomes the held-slot fact of design.md (4 recorded, `ReleaseOne`, 5 recorded, `Release`, 12) with no `Task.Delay`. Verify the class passes and `grep Task.Delay` finds nothing in it.
- [ ] 1.5 `SessionRowEnricherShould.LookUpEachRowsBranch` (`:59-65`) and `WorktreeInventoryShould.LookUpOnlyTheBranchesWithRows` (`:69-75`) await `WhenCalledAsync(1)` and `WhenCalledAsync(2)` with the 5 s deadline and assert the requested branches; the loops and the `Task.Delay(100)` go. Verify both pass and `grep "Task.Delay(10" test` finds no poll loop (`ClaudeContractShould`'s one-second deadline loop and `DesktopShould`'s file wait are not polls on a fake and stay).

## 2. `FakeFolders` answers as `Folders` does (T-06, T-13)

- [ ] 2.1 `test\Source191.Vantage.Core.Tests\TestDoubles\FakeFoldersShould.RefuseToDeleteARootOrARelativePath` (theory over `C:\`, `C:`, `\\server\share\`, `repos\Northwind.Orders`: `IOException`, and a declared folder under the root still exists). Red, then green in `FakeFolders.DeleteTree` with `Path.IsPathFullyQualified` and `Path.GetPathRoot`, before the sets are touched. Verify the facts and `RemoveWorktreeHandlerShould.RefuseToDeleteARootItself` pass.
- [ ] 2.2 `FakeFoldersShould.ThrowForAnUndeclaredFolder` (`Directories`, `Files`, `IsEmpty`: `DirectoryNotFoundException`; `FilesIn`: empty) and `ThrowForAMissingFile` (`Lines`: `FileNotFoundException`). Red, then green. Verify the facts pass.
- [ ] 2.3 Run the solution's tests; declare with `WithDirectory` the folders any fixture relied on enumerating undeclared (13 files build a `FakeFolders`), one commit, each fixture change explained in its commit. Verify `dotnet test -c Debug --no-build` at the solution is green.

## 3. The other fakes (T-12, T-13, T-14)

- [ ] 3.1 `OpenTabHandlerShould.LeaveThePendingRegistrationWhenTheTerminalRefusesTheTab`: `terminal.FailTheOpenWith = new TerminalException("wt refused")`, `Should.ThrowAsync<TerminalException>`, the store holds one pending registration, `Opened` is empty. Red, then green with `FailTheOpenWith` on `test\Source191.Vantage.TestDoubles\FakeWindowsTerminal.cs`. Verify the fact passes.
- [ ] 3.2 `StartSessionHandlerShould.PropagateARefusedTrustTab` (a `WorkspaceNotTrusted` launch whose trust tab is refused throws, and the fake CLI saw the start) and `ResumeSessionsHandlerShould.StopAtTheFirstRefusedTab` (two sessions, the first job removed and resumed, the exception thrown, the second never removed). Verify the facts pass; record the two half-done states in the pull request description for the owner.
- [ ] 3.3 A `FakeProcessRegistry` fact in `test\Source191.Vantage.Core.Tests\TestDoubles\FakeProcessRegistryShould.cs`: `EndTheTreeUnderATerminatedProcess` (a child declared `InTree` under the terminated id has no `StartTimeOf` and is gone from `Snapshot`; `Terminated` is the one id) and `ListARunningProcessNotInTheTree` (`Snapshot` holds it with parent 0 and an empty name). Red, then green. Verify the facts pass and `ProcessTreeShould`, `MemoryBreakdownShould`, `AdoptSessionHandlerShould` and `CloseEmptyTerminalsHandlerShould` still pass.
- [ ] 3.4 `InMemoryStateShould.SurviveTabsWrittenAndRemovedFromManyThreads` (`Parallel.For` writing and removing registrations, then a consistent read). Red if it corrupts (it may pass by luck; say so in the commit), then green with a `Lock` around the three collections. `FakeProcessRunner`: `InteractiveRequests` copied, and `RunInteractiveAsync`'s add made, under `gate`; `FakeProcessRunnerShould.CopyTheInteractiveRequests` pins that the list handed out is not the live one. Verify the facts pass.

## 4. `DesktopShould` runs alone (T-15)

- [ ] 4.1 `test\Source191.Vantage.Infrastructure.Tests\ProcessEnvironmentCollection.cs` with `[CollectionDefinition("Process environment", DisableParallelization = true)]`, and `[Collection("Process environment")]` on `DesktopShould`. Verify the build passes (the attribute property exists in xunit.v3 4.0.2) and `dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` reports `DesktopShould` passing.

## 5. One `Fixtures` (T-09)

- [ ] 5.1 `test\Source191.Vantage.Core.Tests\Fixtures.cs` with `Noon`, `Build(Type)` and `Placeholder(Type)` (the union switch, one message); `StatusTonesShould`, `WordingOfOutcomesShould` and `WordingShould` call them and lose their copies (`WordingShould.cs:35,490,562,648-669,679,712,761,775,888`; `WordingOfOutcomesShould.cs:25-27,472-494`; `StatusTonesShould.cs:244-283`). Verify the three classes pass with the same counts.
- [ ] 5.2 Replace the 26 `static readonly DateTimeOffset Noon` fields in Core.Tests (`SessionTestData.Noon` among them) with `Fixtures.Noon`, a few files per commit; a fact whose expected text spells its old date is updated in the same commit; `DetailSourcesShould.SummerNoon` stays; the two Infrastructure.Tests fields stay with a one-line comment. Verify `grep "DateTimeOffset Noon" test\Source191.Vantage.Core.Tests` finds nothing and the solution's tests pass.

## 6. Fixture names, clocks and `!` (T-10, T-11, T-18)

- [ ] 6.1 `SessionsViewShould.cs:14` and `ClaudeAgentsListingShould.cs:19` name `Northwind.Archive`. Verify `grep -rn "Source191.Vantage" test --include=*.cs` finds only namespaces, `LayeringShould.Root`, `UpdateCheckShould`'s and `NuGetPackageVersionsShould`'s package id and `IconAssetsShould`'s path.
- [ ] 6.2 `EffectDispatcherShould.cs:31,42` use `clock`; `WorktreeInventoryLeftoversShould.cs:22` and `WordingShould.cs:990` a `FakeTimeProvider(Fixtures.Noon)`; `ProcessRegistryIntegrationShould.cs:79,86` say `TimeProvider.System.GetUtcNow()` as `ClaudeContractShould` does. Verify `grep TimeProvider.System test\Source191.Vantage.Core.Tests` finds nothing and the facts pass.
- [ ] 6.3 `SessionsViewShould.cs:70`, `ConfigViewShould.cs:49,185` and `CrashRecorderShould.cs:35` become `ShouldNotBeNull()` chains. Verify `grep "!\." test --include=*Should.cs` finds no `Selected!.` or `Written!.` and the facts pass.

## 7. Blank lines, helper order and naming (T-16, T-19)

- [ ] 7.1 `test\.editorconfig` gains `dotnet_style_allow_multiple_blank_lines_experimental = false` and `dotnet_diagnostic.IDE2000.severity = warning` (unless `small-repetition` has added them first). Verify `dotnet build -c Debug` fails with IDE2000 at the 15 test sites and nowhere under `src` or `tools`.
- [ ] 7.2 Remove the 15 double blank lines (`DiagnosticLineShould.cs:58`, `EditorLocatorShould.cs:127`, `ConfigViewShould.cs:121`, `SessionsViewShould.cs:131,532`, `WizardViewShould.cs:232,243`, `WorktreesViewShould.cs:386`, `WordingOfOutcomesShould.cs:60,378,405`, `WordingOfScreensShould.cs:165,222`, `WordingShould.cs:109`, `SessionNameShould.cs:31`) and add the 6 missing ones before `[Fact]` (`ListSelectionShould.cs:142`, `SessionsViewShould.cs:549`, `WordingOfConfigShould.cs:93`, `WordingOfKeysAndColumnsShould.cs:85`, `WordingOfOutcomesShould.cs:364`, `WordingShould.cs:466`). Verify the build passes and `dotnet format --verify-no-changes` is clean.
- [ ] 7.3 Move the helpers that sit between facts to the end of their class, before `Dispose` where there is one: `SessionsViewShould.cs:563,586`, `WorktreesViewShould.cs:387`, `WordingShould.cs:648,865`, `WordingOfScreensShould.cs:228`, `CommandLineShould.cs:46`, `ScreenLoopShould.cs:23`, `TerminalSetupRunnerShould.cs:24`, `TabRegistrationShould.cs:75`, `WorktreeInventoryShould.cs:79`. Verify the build and tests pass; a diff of each file is moves only.
- [ ] 7.4 Root `.editorconfig`: `private_fields_camel_case` (field, private, camel_case), `private_static_fields_pascal_case` (field, private, `required_modifiers = static`, pascal_case) and `dotnet_diagnostic.IDE1006.severity = warning`, beside the three existing rules. Verify `dotnet build -c Debug` fails with IDE1006 at exactly `OpenTabHandlerShould.cs:22`, `OwnTabShould.cs:15` and `LaunchServiceShould.cs:26`.
- [ ] 7.5 Rename the three `Store` fields to `store` and their uses. Verify `dotnet build -c Debug` passes.

## 8. Whole-solution check

- [ ] 8.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate test-hygiene --strict` all pass; `grep -rn "Task.Delay" test --include=*.cs` shows only `ClaudeContractShould` and `DesktopShould`'s file wait. Record the totals here.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
- The pull request description lists the three half-done states the handler facts pin (design.md, `FailTheOpenWith`) for the owner to route to a `src` change.
- Push and open the implementation pull request only when the owner asks.
