# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. Domain: the newest release without null

- [x] 1.1 `NewestReleaseShould.ReadARecordedVersionAsFound`, `ReadARecordedPrereleaseAsFound` and `ReadTextThatIsNotAVersionAsNone`: red, then `Domain\NewestRelease.cs` (`None`, `Found(ReleaseVersion Version)`, `Recorded(string text)` with non-nullable text) green. Verify the three facts pass and `NewestRelease.cs` declares no nullable reference type.
- [x] 1.2 `UpdateCheckRuleShould.OfferOnlyAHigherRelease` and `OfferNothingWhenTurnedOffOrForAPrerelease` pass `NewestRelease.Recorded(...)` (and `new NewestRelease.None()` where they passed `null`) with every expected answer unchanged: red on the missing overload, then `UpdateCheckRule.Offer(ToolVersion, bool, NewestRelease)` with the single `is NewestRelease.Found` pattern green. Verify both facts pass with the same expected `UpdateStatus` values as before.
- [x] 1.3 `UpdateCheckRuleShould`'s `Newest` facts expect `new NewestRelease.Found(...)` for `["0.7.2", "0.10.0", "0.9.0", "0.11.0-rc.1", "not a version"]` (the `!` goes) and `new NewestRelease.None()` for `["0.11.0-rc.1", "rubbish"]` and `[]` (the two `ShouldBeNull` go): red, then `UpdateCheckRule.Newest` answering a `NewestRelease` green. Verify the facts pass with the same versions chosen as before.

## 2. Application: the snapshot and its two users

- [x] 2.1 `SharedSnapshotShould.AnswerTheFirstValue`, `AnswerTheReplacement`, `RefuseANullFirstValue` and `RefuseANullReplacement`: red, then `Application\SharedSnapshot.cs` (sealed, `where T : class`, one `volatile` field, `Value`, `Replace`, the class comment citing the CoreCLR memory model) green. Verify the four facts pass and the build shows no analyser warning for the `volatile` field; if one appears, record the rule and the decision here.
- [x] 2.2 `CurrentSettings` holds a `SharedSnapshot<Settings>` in place of `Volatile.Read` and `Volatile.Write` (refactor step: no new fact). Verify `CurrentSettingsShould` passes unchanged and `Select-String -Path src\Source191.Vantage.Core\Application\CurrentSettings.cs -Pattern Volatile` finds nothing.
- [x] 2.3 `UpdateCheck` holds a `SharedSnapshot<NewestRelease>` starting at `None`; it replaces it with the record's newest (`record?.Newest is string text ? NewestRelease.Recorded(text) : new NewestRelease.None()`) and then with `UpdateCheckRule.Newest(versions)`, and writes the record's text through one private switch naming both cases with an `UnreachableException` discard arm (refactor step: `UpdateCheckRecord` keeps `string? Newest`, so the port's null is met only on those two lines). Verify `UpdateCheckShould` (including the record written as `"0.10.0"` and as `null`) and `ScreenLoopShould` pass unchanged, and `UpdateCheck.cs` has no `Volatile` and no `string? newest`.

## 3. Tui: the clipboard without null

- [ ] 3.1 `ScreenClipboard` holds a `SharedSnapshot<IClipboard>` starting at a private `NoClipboard`; `Attach` replaces it with a private `ApplicationClipboard(IApplication)`, whose `TrySetText` checks Terminal.Gui's nullable `Clipboard` on each copy (`app.Clipboard is { } clipboard && clipboard.TrySetClipboardData(text)`), and `Detach` with the `NoClipboard` instance (refactor step: `Tui` has no test project). Verify the build, `CopyFolderPathHandlerShould` and `CopyProblemReportHandlerShould` pass, `ScreenClipboard.cs` has no `Volatile` and no nullable declaration, and the smoke check passes: `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>` against a locally packed tool.

## 4. Docs, version and the whole build

- [ ] 4.1 `CLAUDE.md`'s code rules gain one sentence: a value one thread replaces and another reads is held in a `SharedSnapshot<T>` (never null, immutable value), never by a bare `Volatile` access or `volatile` field. Verify `Get-ChildItem src -Recurse -Filter *.cs | Select-String -Pattern 'Volatile\.|\bvolatile\b'` finds only `SharedSnapshot.cs`.
- [ ] 4.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to `0.19.0-preview.3` (one step above main's `0.19.0-preview.2`; raise again if another tool change merges first). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 4.3 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` all pass; `openspec validate shared-snapshot` passes.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
