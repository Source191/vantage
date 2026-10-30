# Proposal

## Why

The code already follows most C# 14 and .NET 10 idioms (no `== null` or `!= null`, no `var`, file-scoped namespaces everywhere, 85 primary constructors), but only by habit: `.editorconfig` leaves the matching style rules at `silent` or `suggestion`, and one of them (`csharp_style_namespace_declarations = block_scoped`) contradicts the code. Raising those rules to `warning` makes the build, which already fails on warnings, hold the idioms in place, and the handful of places that still use older forms (array creation, `[DllImport]`, explicit constructors that could be primary ones, locking on a collection) move on at the same time.

## What Changes

- **`.editorconfig` holds the idioms:** file-scoped namespaces (IDE0161), explicit types rather than `var` (IDE0008), `is null` and `is not` patterns (IDE0041, IDE0083), primary constructors (IDE0290), target-typed `new` (IDE0090), collection expressions (IDE0300 to IDE0306), `System.Threading.Lock` (IDE0330) and the C# 14 clean-ups IDE0340, IDE0360, IDE0370 and IDE0380 all become `warning`. IDE0350 (implicitly typed lambda parameters) is left off, because it works against the concrete-types rule.
- **Collection expressions** replace the remaining `new[] { ... }` and `new List<T> { ... }` outside attribute arguments.
- **`CurrentSettings` and `NuGetPackageVersions` take primary constructors**, guarding their parameters with `?? throw new ArgumentNullException(nameof(...))` field initialisers. `NuGetPackageVersions` loses its second constructor: `Program.cs` hands it the message handler, and the configured `HttpClient` is built in a field initialiser.
- **`[LibraryImport]` replaces `[DllImport]`** in `ProcessRegistry` and `ConsoleRecovery`, so the interop is source-generated and trimming-safe, as `tools\HeadlessTerminal` already is.
- **`tools\HeadlessTerminal\CapturedRun`** locks on a dedicated `Lock` rather than on its `StringBuilder` and `List`.
- **Switch statements that only choose a value** become switch expressions; those that run side effects stay.
- **Version:** `Version` rises one step (the prerelease number while the version has one), because `src\` changes. `skip_specs: true`: no behaviour of the tool changes.

Out of scope:
- **`IHttpClientFactory`:** Vantage holds one long-lived client and asks nuget.org at most once a day with no dependency injection container; Microsoft's HttpClient guidelines accept a long-lived client or a limited number of instances.
- **C# 15 collection expression arguments** (`[with(comparer), ...]`) for the `ChildCommand` sets: not available on .NET 10.
- **C# 14 `extension` blocks** for the three `this`-parameter extension methods: little to gain.
- **Deliberate older forms:** `[InlineData(new[] { ... })]` (attributes cannot take collection expressions), `ConsoleScreen`'s re-captured fields (the nested `Frame` reads them), `Array.Empty<T>()` inside conditional spreads (no target type), the `GetAwaiter().GetResult()` UI-thread workaround in `LaunchWizardDialog`, `string.Empty`, and the one `ReferenceEquals` on `Token`.
- **The private constructors behind `From` and `TryParse`:** a primary constructor is always public, so the text values and validated records keep theirs.
- **Multiple Claude accounts:** a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: the change is a refactor with no behaviour change, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Build configuration:** `.editorconfig` (root); `src\Source191.Vantage.Infrastructure\Source191.Vantage.Infrastructure.csproj` gains `AllowUnsafeBlocks` (the `[LibraryImport]` generator requires it), with the reason recorded in that project's `README.md`.
- **Code:** `CurrentSettings`, `NuGetPackageVersions`, `Program.cs`, `ProcessRegistry`, `ConsoleRecovery`, `PowerShellScript`, `RemoteRepository`, `WhereLabel`, `ConsoleScreen`, `WizardControls`, `CapturedRun`, a few switch statements, and the tests that build arrays with `new[] { ... }` or construct `NuGetPackageVersions`; plus whatever the raised rules find.
- **Release:** `Version` rises from `0.18.0-preview.5` to the next free prerelease when the pull request merges.
- **Docs:** `CLAUDE.md`'s code rules mention that the idioms are enforced by `.editorconfig`.
