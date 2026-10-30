# Tasks

Every group ends green: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes`. Each rule's commit holds the `.editorconfig` line and the fixes it forces, and nothing else. No behaviour changes, so the existing tests are the safety net; where a guard has no test yet, the test comes first and must pass before the code moves.

## 1. Lock in what already holds

- [x] 1.1 In `.editorconfig`, set `csharp_style_namespace_declarations = file_scoped:warning`, `csharp_style_var_for_built_in_types`, `csharp_style_var_when_type_is_apparent` and `csharp_style_var_elsewhere` to `false:warning`, and add `dotnet_diagnostic.IDE0161.severity`, `IDE0008`, `IDE0041` and `IDE0083` at `warning`. Verify the build passes with no code change (the measured count of violations is zero), then prove each rule bites by adding `var x = 1;` and a block-scoped namespace to a scratch file, seeing IDE0008 and IDE0161 fail the build, and deleting the file.

## 2. Collection expressions

- [x] 2.1 Set `dotnet_style_prefer_collection_expression = true:warning` and IDE0300 to IDE0306 to `warning`, run `dotnet format style --diagnostics IDE0300 IDE0301 IDE0302 IDE0303 IDE0304 IDE0305 IDE0306 --severity warn`, then fix by hand what the build still lists. Verify the build and every suite pass.
  - Recorded 30 October 2026: raising `dotnet_style_prefer_collection_expression` to `warning` also brought IDE0028 (collection initialisers) in. The fixer converted four `.ToArray()` sites (IDE0305 in `WorktreesViewShould` and `ScreenReplay`); the nine `TheoryData<T> => new(...)` members IDE0028 found were converted by hand to `[.. ...]`. No site in `src` was flagged.
- [x] 2.2 Convert the `new[] { ... }` sites the analyser leaves: `[executable, .. arguments]` in `PowerShellScript.cs:36` (guarded by `PowerShellQuotingShould`), `RemoteRepository.cs:33` (`RemoteRepositoryShould`), `WhereLabel.cs:66` (`WhereLabelShould`), the `foreach` sources in `ConsoleScreen.cs:359` and `WizardControls.cs:45`, and the `foreach (... in new[] { ... })` loops in the tests, and the `new List<T> { ... }` samples in `WordingOfOutcomesShould.cs:450-452`. Leave `[InlineData(new[] { ... })]` and the `Array.Empty<T>()` in `WordingOfConsole`'s conditional spreads. Verify `Select-String 'new\s*\w*\s*\[\]\s*\{'` over `src`, `test` and `tools` lists only `InlineData` lines, and that the named tests pass.
  - Recorded 30 October 2026: every `new[] { ... }` outside `InlineData` became a named local array built with a collection expression (a collection expression has no type of its own, so it cannot be the source of a `foreach` or the receiver of a call). The three `new List<T>` samples in `WordingOfOutcomesShould` stay: they are arms of a switch expression typed `object`, which gives a collection expression no target type, the same reason as `Array.Empty<T>()` in `WordingOfConsole`. The scan lists only `InlineData` lines; the full suite passed (1,700 passed, 7 skipped).

## 3. Target-typed new

- [x] 3.1 Add `csharp_style_implicit_object_creation_when_type_is_apparent = true:warning` and `dotnet_diagnostic.IDE0090.severity = warning`, run `dotnet format style --diagnostics IDE0090 --severity warn`, then fix what the build still lists. Verify the build and every suite pass and that `new Derived()` assignments to a base type (`Footer.None`, `TabOwner.Vantage`) are untouched.
  - Recorded 30 October 2026: the fixer changed 21 expression-bodied members in 16 files whose return type names the object (`=> new TabLaunch(...)` to `=> new(...)`), with nothing left for a hand fix; `Footer.None` and `TabOwner.Vantage` still read `new Blank()` and `new ForVantage()`; the full suite passed.

## 4. Primary constructors

- [x] 4.1 Write `RefuseNullInitialSettings` and `RefuseANullStore` in `CurrentSettingsShould`, each asserting `ArgumentNullException` with the parameter name, and `RefuseANullVersion` in `NuGetPackageVersionsShould`. Verify all three pass against the current constructors (they pin the behaviour the move must keep).
- [x] 4.2 Give `CurrentSettings` a primary constructor `(Settings initial, IVantageConfig store)` with `private readonly IVantageConfig store = store ?? throw new ArgumentNullException(nameof(store));` and `private Settings value = initial ?? throw new ArgumentNullException(nameof(initial));`. Verify the build raises no CA1510 (design.md records the outcome either way), and `CurrentSettingsShould` (including 4.1's tests) and `SettingsViewShould` pass.
  - Recorded 30 October 2026: the build raised no CA1510 on the `?? throw` initialisers, and no CS9124 (the parameters are read only in initialisers, so neither is captured); the in-memory suite passed (1,385), including `RefuseNullInitialSettings` and `RefuseANullStore`.
- [x] 4.3 Give `NuGetPackageVersions` the primary constructor `(ToolVersion version, HttpMessageHandler handler)`, build the client in `private readonly HttpClient client = ClientFor(version ?? throw new ArgumentNullException(nameof(version)), handler);` with a private static `ClientFor` holding the base address, timeout and user agent, delete the one-parameter constructor, and change `Program.cs:138` to `new(version, new HttpClientHandler())`. Verify `NuGetPackageVersionsShould` passes unchanged (the request URI and the `Vantage/0.9.0` user agent), and the build raises no CA2000 or Sonar disposal warning (design.md's risk names the fallback if it does).
  - Recorded 30 October 2026: the build raised no CA1510, CA2000 or Sonar warning; `NuGetPackageVersionsShould` passed unchanged (7, including `RefuseANullVersion`).
- [x] 4.4 Raise `csharp_style_prefer_primary_constructors` to `true:warning` and add `dotnet_diagnostic.IDE0290.severity = warning`. Verify the build passes with no further fixes. If it lists a class whose constructor is public, convert it in this task the same way and name it here; if the flagged constructor is `internal` or `private` (`ProblemReport`, `ChildCommand`), keep it, because a primary constructor would widen its access, and record it in design.md beside the `From` and `TryParse` exception.
  - Recorded 30 October 2026: the build listed no class (the analyser does not offer a primary constructor for a non-public constructor or one with logic); a scratch class with a public field-assigning constructor failed with IDE0290 and was deleted; the full suite passed (1,703 passed, 7 skipped).

## 5. Locks

- [x] 5.1 Add `csharp_prefer_system_threading_lock = true:warning` and `dotnet_diagnostic.IDE0330.severity = warning`. Verify the build passes (every existing gate is already a `Lock`).
- [x] 5.2 In `tools\HeadlessTerminal\CapturedRun.cs`, replace `lock (output)` and `lock (replies)` with `lock (gate)` on one `private readonly Lock gate = new();`. Verify `tools\HeadlessTerminal.Tests` passes and `dotnet run --project tools\HeadlessTerminal -- check` exits 0.
  - Recorded 30 October 2026: IDE0330 found nothing (5.1). The summary line's read of `replies` (`FinishAsync`), which took no lock before, now reads under the same gate through `RepliesSent()`. `tools\HeadlessTerminal.Tests` passed (40); the hand-off check against a Release build exited 0 ("Claude's own screen is in the capture", replies listed).

## 6. C# 14 clean-ups

- [x] 6.1 Add `csharp_style_prefer_unbound_generic_type_in_nameof = true:warning`, `csharp_style_prefer_simple_property_accessors = true:warning` and IDE0340, IDE0360, IDE0370 and IDE0380 at `warning`; fix whatever the build lists (the measured expectation is none or a handful of `!` suppressions for IDE0370). Verify the build and every suite pass and list here any site that changed.
  - Recorded 30 October 2026: no site changed; the build passed with all four rules at `warning`, and a scratch file with `nameof(List<int>)` and `"a"!` failed with IDE0340 and IDE0370 before it was deleted.

## 7. Switch expressions

- [ ] 7.1 Read the nine `switch` statements (`Program.cs:188`, `TerminalEffects.cs:14`, `HereCommandRunner.cs:27`, `TerminalSetupRunner.cs:143`, `CommandArguments.cs:20`, `GitWorktreeListing.cs:33`, `TableBody.cs:264`, `ScreenReplay.cs:66` and `:134`) and convert to a switch expression only those whose every arm returns or assigns one value, naming each converted one and the test that covers it here (`HereCommandRunnerShould`, `TerminalSetupRunnerShould`, `CommandArgumentsShould`, `GitWorktreeListingShould`, `ScreenReplayShould` as they apply). A switch over a closed enum names every member with no discard arm, as the code rules require. Verify the named tests pass.

## 8. Source-generated interop

- [ ] 8.1 Add `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` to `Source191.Vantage.Infrastructure.csproj` and a paragraph to `src\Source191.Vantage.Infrastructure\README.md` saying the `[LibraryImport]` generator requires it and the project writes no `unsafe` code. Verify the build passes.
- [ ] 8.2 Convert `ConsoleRecovery`'s four `[DllImport]`s to `[LibraryImport]` on `static partial` methods in a `partial` class, keeping the `bool` return marshalling. Verify `ConsoleRecoveryShould` and `TerminalConsoleShould` pass and the hand-off check (`dotnet run --project tools\HeadlessTerminal -- check`) exits 0.
- [ ] 8.3 Replace `ProcessEntry.ExeFile`'s `ByValTStr` string with an `[InlineArray(260)]` `ushort` buffer read through `MemoryMarshal.Cast<ushort, char>` up to its first zero, and drop `CharSet.Unicode` from its `StructLayout`; then convert `ProcessRegistry`'s ten `[DllImport]`s to `[LibraryImport]` with `SetLastError` and the `bool` marshalling kept. Verify `ProcessRegistryIntegrationShould` passes (the test process's own name and parent chain, the memory figures, two hundred queries under a second) along with `ProcessTreeShould` and `TerminalTabShould`.
- [ ] 8.4 Add `dotnet_diagnostic.SYSLIB1054.severity = warning` to `.editorconfig`. Verify the build passes and `Select-String 'DllImport\('` over `src` finds nothing.

## 9. Documentation and release

- [ ] 9.1 In `CLAUDE.md`'s code rules and `CONTRIBUTING.md`'s "C# style" bullet (which today names "the throw helpers for guard clauses"), say that `.editorconfig` holds the idioms at `warning` (file-scoped namespaces, explicit types, `is null` patterns, primary constructors with `?? throw` guards, target-typed `new`, collection expressions, `Lock`, `[LibraryImport]`), and that private constructors behind `From` and `TryParse` are the expected exception to IDE0290. Verify by reading it against design.md.
- [ ] 9.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one step with `.github\scripts\Step-Version.ps1` (from `0.18.0-preview.5`, or whatever `main` holds when the pull request opens). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.

## 10. Integration

- [ ] 10.1 Run the whole loop from a clean tree: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes`, `dotnet run --project tools\HeadlessTerminal -- check`, then `tools\install-local.ps1` and the packed tool's smoke run (`dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>`). Verify every command exits 0 and record the results here.

## Workflow follow-up

- Open the pull request; `check` must pass, including the version check and the smoke run.
- Archive the change in the same pull request; `skip_specs` means no spec sync.
