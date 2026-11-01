# Proposal

## Why

The tests reviewer's report in the code review of 1 November 2026 (0.20.0-preview.7, `Vantage-0.20.0-preview.7-code-review-appendix.md`, T-04 to T-19) found three unbounded poll loops that hang the run instead of failing, four sleep-then-assert-absence facts that can pass falsely on a loaded runner, fakes that answer more kindly than the adapters they stand in for (`FakeFolders.DeleteTree` deletes `C:\`, `FakeWindowsTerminal` cannot refuse a tab, `FakeProcessRegistry.Terminate` leaves a tree's children running, `InMemoryState` has no lock though the enricher removes stale tabs from pool threads), three copies of one reflection builder and `Placeholder` switch, 28 copies of `Noon`, and small drifts (a real repository name in two fixtures, the real clock in three unit tests, blank-line slips, four `!`, a PascalCase field) that nothing mechanical catches. Each cited line was re-read on `main` ac7cdae0 before this was written; the lines below are current.

## What Changes

- **Deadlines (T-04):** `FakePullRequests` gains `Task WhenCalledAsync(int count)` backed by a `TaskCompletionSource`, awaited with `WaitAsync(TimeSpan.FromSeconds(5))`; the three `while (... .Count < n) await Task.Delay(10)` loops go (`PullRequestCatalogueShould.cs:177-183`, `SessionRowEnricherShould.cs:59-62`, `WorktreeInventoryShould.cs:69-72`).
- **No sleeping (T-05):** `FakePullRequests.Hold` holds each call on its own completion source and gains `ReleaseOne()`, so `RunAtMostFourLookupsAtOnce` (`PullRequestCatalogueShould.cs:108`) proves the fifth lookup waited by releasing one held call and awaiting the fifth; `LookUpEachRowsBranch` (`SessionRowEnricherShould.cs:64`), `LookUpOnlyTheBranchesWithRows` (`WorktreeInventoryShould.cs:74`) and `HoldACallOpenUntilReleased` (`FakePullRequestsShould.cs:19`) await `WhenCalledAsync` and assert on the branches requested. The four `Task.Delay` calls go.
- **`FakeFolders` fidelity (T-06, T-13):** `DeleteTree` refuses a drive root, share root or relative path with `IOException` as `FolderDeletion.RefuseRootsAndRelativePaths` does; `Directories`, `Files` and `IsEmpty` throw `DirectoryNotFoundException` for an undeclared folder as `Directory.Enumerate*` does (`FilesIn` stays lenient, as `Folders.FilesIn` is); `Lines` throws `FileNotFoundException` as `File.ReadLines` does. A new `FakeFoldersShould` beside `FakeProcessRunnerShould` pins each.
- **`FakeWindowsTerminal` failure switch (T-13):** `FailTheOpenWith`, in the shape of `FakeClaudeSettings.FailTheWriteWith`, and one fact each in `OpenTabHandlerShould`, `StartSessionHandlerShould` and `ResumeSessionsHandlerShould` pinning that a refused tab propagates to the dispatcher (design.md records the half-done states those facts expose).
- **`FakeProcessRegistry` (T-12):** `Terminate` also removes the tree's descendants from `running` and `tree` (`Terminated` still records only the id asked for); `Snapshot` includes ids declared only with `Running`, parent 0 and an empty name.
- **Thread safety in fakes (T-14):** a `Lock` around `InMemoryState`'s three collections; `FakeProcessRunner.InteractiveRequests` copied, and `RunInteractiveAsync`'s add made, under its existing gate.
- **Environment mutation (T-15):** `DesktopShould` joins a `Process environment` collection whose `[CollectionDefinition]` sets `DisableParallelization = true` (xunit.v3 4.0.2), so no other Infrastructure test class runs while it has the Claude markers set. `[Collection]` alone would change nothing: a class is already its own collection.
- **One `Fixtures` (T-09):** `Fixtures.Build(Type)`, `Fixtures.Placeholder(Type)` and `Fixtures.Noon` in Core.Tests replace the ten copies of the reflection builder and the three `Placeholder` switches (`WordingShould.cs:648`, `WordingOfOutcomesShould.cs:472`, `StatusTonesShould.cs:251`) and the 26 `Noon` fields in Core.Tests (`SessionTestData.Noon` among them); the two in Infrastructure.Tests (`WorktreeInventoryShould.cs:16`, `JsonFileStoreShould.cs:10`) stay, since that project does not reference Core.Tests. `DetailSourcesShould.SummerNoon` stays: a summer date is how a UK time is shown to differ from UTC (`.claude\rules\locale.md`).
- **Fixture names (T-10):** `SessionsViewShould.cs:14` and the `cwd` in `ClaudeAgentsListingShould.cs:19` name the real `Source191.Vantage` repository; both become `Northwind.Archive`.
- **Clocks (T-11):** `EffectDispatcherShould.cs:31,42`, `WorktreeInventoryLeftoversShould.cs:22` and `WordingShould.cs:990` use `TimeProvider.System` where a `FakeTimeProvider` is the house style; `ProcessRegistryIntegrationShould.cs:79,86` spell the real clock `DateTimeOffset.UtcNow` while `ClaudeContractShould` says `TimeProvider.System.GetUtcNow()`; one spelling.
- **Blank lines and helper order (T-16):** the 15 double blank lines in 11 test files and the 6 missing blank lines before `[Fact]` are fixed, and the helpers that sit between facts in 9 files move to the end of their class. `dotnet_style_allow_multiple_blank_lines_experimental = false` with IDE2000 at warning goes in `test\.editorconfig`, not the root: `src` holds nine double blank lines of its own (eight files) and a root rule would fail the build there and force a version bump. If `small-repetition` lands first with the rule, this change only reconciles.
- **`!` in assertions (T-18):** `SessionsViewShould.cs:70`, `ConfigViewShould.cs:49,185` and `CrashRecorderShould.cs:35` become `ShouldNotBeNull()` chains (Shouldly's returns the value).
- **Naming rule (T-19):** a `private_fields_camel_case` naming rule for private instance fields, a narrower pascal_case rule for private static fields (const is implicitly static), and `dotnet_diagnostic.IDE1006.severity = warning` in the root `.editorconfig`, which is what makes a naming rule fail the build; `Store` becomes `store` in `OpenTabHandlerShould.cs:22`, `OwnTabShould.cs:15` and `LaunchServiceShould.cs:26`. No private instance field under `src` or `tools` is PascalCase today.
- **Not in this change:** T-07 (`HeadlessTerminal`'s escape-sequence scanner) is `small-repetition`'s, T-08 (Tui definition tests) is `table-body-collaborators`' and T-17 (`LayeringShould`'s stale namespaces) is `convention-nets'`. T-19's `TerminalQueries` regex half is left with T-07 in `small-repetition`. Nothing cited was found already fixed on `main`; the review's T-16 line list was stale (five sites gone, four new), and the current scan replaces it.
- `skip_specs: true`: tests, fakes and two `.editorconfig` files only; no key, screen, message, state file or command line behaves differently.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: tests only, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Tests and fakes:** `test\Source191.Vantage.TestDoubles` (`FakeFolders`, `FakePullRequests`, `FakeProcessRegistry`, `FakeWindowsTerminal`, `InMemoryState`, `FakeProcessRunner`), `test\Source191.Vantage.Core.Tests` (new `Fixtures.cs` and `TestDoubles\FakeFoldersShould.cs`; about 30 existing files edited) and `test\Source191.Vantage.Infrastructure.Tests` (`DesktopShould`, a new collection definition, `WorktreeInventoryShould`, `ProcessRegistryIntegrationShould`, `ClaudeAgentsListingShould`).
- **Configuration:** the root `.editorconfig` (naming rule, IDE1006) and `test\.editorconfig` (IDE2000). Promoting IDE1006 also turns the three existing suggestion-level naming rules (interfaces, types, non-field members) into build warnings across the solution; a grep finds no violation, and the whole-solution build is the check.
- **Version:** no file under `src\`, `Directory.Build.props`, `global.json` or `nuget.config` changes, and `.github\scripts\Assert-VersionBumped.ps1` lists only those four paths, so `Version` does not rise. `.editorconfig` is not on its list.
- **Other open changes:** `small-repetition` (T-07, and T-16's rule if it adds it first), `table-body-collaborators` (T-08) and `convention-nets` (T-17) touch other test files; whichever lands second rebases. `tidy-composition-root` adds facts over the same fakes and is unaffected by their stricter answers unless it enumerates an undeclared folder.
- **Users:** none.

## Out of scope

- Production changes the handler facts might argue for (an `OpenTabOutcome` case for a refused tab, catching `TerminalException` in the three handlers): design.md lists them for a later change.
- A test seam on `PullRequestCatalogue` or the enricher to await the fire-and-forget refresh (a `src` change).
- Banned-symbols analysers for `TimeProvider.System` in tests, and the other convention nets of the review's conventions table (`convention-nets`).
- Multiple Claude accounts, which is a later change.
