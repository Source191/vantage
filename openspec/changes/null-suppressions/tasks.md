# Tasks

Each task is one red-green-refactor cycle or a refactor step, with a commit per step; the test is named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` unless it says otherwise.

## 1. Domain: the two parsers

- [x] 1.1 `Architecture\ParsingShould.ReturnBoolFromEveryTryParse`, `DeclareNoParse` and `FindEveryParser`: red on `ReleaseVersion` and `RemoteRepository`. Leave red; 1.2 to 1.4 turn it green. Verify the two failures name exactly those two types.
- [x] 1.2 `test\Source191.Vantage.Core.Tests\ReleaseVersions.cs` with `Version(string text)` (asserts the parse, returns the version), and every `ReleaseVersion.TryParse("...")!` in the Core tests uses it (refactor step against today's `TryParse`). Verify the suite passes and `Get-ChildItem test -Recurse -Filter *.cs | Select-String 'ReleaseVersion\.TryParse\(.*\)!'` finds nothing.
- [x] 1.3 `ReleaseVersionShould`'s parse facts call `TryParse(text, out ReleaseVersion? version)` (`ShouldBeTrue` and the version, `ShouldBeFalse` for the texts that are not versions), and `ReadOnlyTheTextsThatParse` covers `ReleaseVersion.Parsed`: red, then `TryParse(string?, [MaybeNullWhen(false)] out ReleaseVersion)` matching `[string major, string minor, string patch]` with no `int?[]` and no `!`, plus `Parsed(IEnumerable<string>)`, green; `UpdateCheckRule` (`Newest` through `Parsed`, `ReleaseOf`), `ClaudeCodeSupport.Check`, `NewestRelease.Recorded` and `ClaudeContractShould` follow. Verify `ReleaseVersionShould`, `UpdateCheckRuleShould`, `NewestReleaseShould` and `ClaudeCodeSupport`'s facts pass with their expected answers unchanged, and the Infrastructure tests build.
- [x] 1.4 `RemoteRepositoryShould`'s facts call `TryParse(url, out RemoteRepository? repository)` (`ShouldBeFalse` where they expected null): red, then `RemoteRepository.TryParse` (and the private `Azure` through `out`) green; `PullRequestCatalogue` follows. Verify `RemoteRepositoryShould`, `PullRequestCatalogueShould` and `ParsingShould` pass.
- [x] 1.5 `EditorChoiceShould.ReadACustomEditorWithNoArgumentsAsVsCode` (a full-path executable with `arguments: null`; none of the seven `FromFile` facts covers it), added first to pin today's answer, then `EditorChoice.FromFile`'s custom arm matches `executable is string path && arguments is { } list` (refactor step). Verify the new fact passes before and after the edit and the seven existing facts pass unchanged.

## 2. Application

- [x] 2.1 `WorktreeInventory.OwnerOf` matches `gitDir is string dir` before slicing (refactor step). Verify `WorktreeInventoryShould` passes unchanged and `WorktreeInventory.cs` has one `!` left, `row.Branch!`, for null-free-rows.
- [x] 2.2 `ExceptionSummary.Frames` matches `frame.GetMethod() is MethodBase method` once and passes it to `IsAwaitMachinery` and `Frame` (refactor step). Verify `ExceptionSummaryShould` and `DiagnosticsPrivacyShould` pass unchanged.

## 3. Adapters

- [x] 3.1 `ProcessRegistry.WithProcess<T>(processId, query, whenGone)` and `WithProcess(processId, Action<Process>)`; `Terminate` and `EndCleanly` use the action overload, `StartTimeOf` and `ParentOf` pass `whenGone: null` (refactor step). Verify `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` passes, including `ProcessRegistryIntegrationShould`, and `ProcessRegistry.cs` has no `default!` and no `object?`.
- [x] 3.2 `WorktreeIncludeCopier` and `JsonFileStore` create the parent folder only when `Path.GetDirectoryName(...) is string folder` (refactor step). Verify `WorktreeIncludeCopierShould` and `JsonFileStoreShould` pass unchanged.

## 4. Tui

- [ ] 4.1 `TableBody.AskForDetail` matches the source, requests and pane once and passes them to `ReadDetail`, `ReadAsync` and `ShowDetail` (refactor step: no Tui test project). Verify the build, `TableBody.cs` has no `!`, and `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>` passes against a locally packed tool.
- [ ] 4.2 `ConsoleScreen.ClearLater` (added by the status-line change, PR #129) removes its own timeout with `timer is { } added` in place of `timers.Remove(timer!)`; the `object?` from Terminal.Gui's `AddTimeout` stays, as the boundary (refactor step). Verify the build, `ConsoleScreen.cs` has no `!`, and a success message still clears after ten seconds in a headless `drive` capture (copy a folder path with `c`, wait 11000 ms, `count` finds the copied message once).
- [ ] 4.3 `VantageIcon.Fit` matches `support is { IsSupported: true, Resolution: System.Drawing.Size cell }` (refactor step). Verify the build and `VantageIcon.cs` has no `!`; the headless terminal reports no image support, so the hidden path is the one the smoke check exercises.

## 5. Version and the whole build

- [ ] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one step above main (projected `0.19.0-preview.4`). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 5.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` pass; `Get-ChildItem src -Recurse -Filter *.cs | Select-String '\w!\.|\w!\)|\w!;|\w!,|\w!\[|null!|default!'` finds only `WorktreeInventory.cs`'s `row.Branch!`; `openspec validate null-suppressions` passes.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
