# Design

## Context

See proposal.md for why. The facts that shape the approach:

- **The build already enforces style:** `Directory.Build.props` sets `EnforceCodeStyleInBuild`, `AnalysisLevel 10.0-recommended` and `TreatWarningsAsErrors`, and `LangVersion latest` on net10.0 gives C# 14. A rule raised to `warning` in `.editorconfig` therefore fails the build wherever the code breaks it, in `src\`, `test\` and `tools\` alike (the two nested `.editorconfig` files only relax IDE0022 for tests).
- **What the rules will find, measured on 30 October 2026:** IDE0161, IDE0008, IDE0041 and IDE0083 find nothing (no block namespaces, no `var`, no `== null` or `!= null`). IDE0290 has two candidates: `CurrentSettings` and `NuGetPackageVersions`. Most of the other 36 explicit constructors are private constructors behind `From` and `TryParse`, which a primary constructor cannot express; the rest are xunit set-up constructors or Terminal.Gui views that build controls. IDE0090 and IDE0300 to IDE0306 find a few dozen sites at most.
- **Interop:** `ProcessRegistry` declares ten `[DllImport]`s and `ConsoleRecovery` four. `ProcessEntry` (the `PROCESSENTRY32W` structure) holds the executable name as `[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] string`, which makes it non-blittable. `tools\HeadlessTerminal\NativeMethods` already uses `[LibraryImport]`, with `AllowUnsafeBlocks` set in its project.
- **The guards:** `CurrentSettings` and `NuGetPackageVersions` check their parameters with `ArgumentNullException.ThrowIfNull`, and no test covers those checks today.

## Goals / Non-Goals

**Goals:**
- Each idiom is held by a rule at `warning`, so a later change cannot drift back without a red build.
- Each rule lands in a commit of its own (the rule and the fixes it forces), so a reviewer reads one idiom at a time and any commit can be reverted on its own.
- The packed tool behaves exactly as before: the same requests to nuget.org, the same process facts, the same console recovery.

**Non-Goals:**
- Raising every remaining `suggestion` in `.editorconfig`; only the rules named in the proposal move.
- Rewriting the private constructors of the text values, or anything listed as out of scope in the proposal.

## Decisions

### Rules are raised one at a time and fixed with the code fixer first

For each rule: set it to `warning`, run `dotnet format style --diagnostics <ID> --severity warn` to apply the IDE's own fix, then `dotnet build -c Debug` to list what the fixer could not handle, fix those by hand, and run the full test suite. Raising every rule at once would give one unreadable commit and make a regression hard to place. Fixing by hand from grep results would miss sites the analyser sees (target-typed `new` in property initialisers, for instance) and leave nothing to hold the result.

The four rules that find nothing (IDE0161, IDE0008, IDE0041, IDE0083) go in one commit, because there is nothing to review but the `.editorconfig` lines. `csharp_style_namespace_declarations` changes from `block_scoped:silent` to `file_scoped:warning`, which ends the contradiction with the code.

IDE0350 (implicitly typed lambda parameters) stays off: it would remove explicit types that the concrete-types rule wants. IDE0090 is configured through `csharp_style_implicit_object_creation_when_type_is_apparent = true:warning`, which matches the existing `Model model = new();` rule; it does not touch `new Derived()` assigned to a base type, so `Footer None = new Blank()` and the like stay as they are.

SYSLIB1054 (use `LibraryImport` instead of `DllImport`) is raised to `warning` with the interop commit, so a new `[DllImport]` fails the build.

### Primary-constructor guards are `?? throw` field initialisers

A primary constructor has no body, so `ArgumentNullException.ThrowIfNull` has nowhere to go. The parameter is checked where it is first used: `private readonly IVantageConfig store = store ?? throw new ArgumentNullException(nameof(store));`. It is the form Microsoft's IDE0016 documentation shows for exactly this assignment, and the code already uses throw expressions elsewhere (`ClaudeSettings`, `VantageIcon`). A parameter read only in an initialiser is not captured, so no hidden second field appears.

Alternatives considered: dropping the guards and relying on nullable reference types (rejected: `CurrentSettings` is public and built by `Program.cs` and tests, and a null would surface later as a `NullReferenceException` far from its cause); keeping the explicit constructors and suppressing IDE0290 for these two classes (rejected: a suppression is a standing exception for a style the rest of the code follows).

CA1510 (which asks for `ThrowIfNull`, and fails the build here) is documented as firing on an `if` block that throws `ArgumentNullException`, not on a throw expression, so the two should not conflict; the first build of task 4.2 confirms it, and if CA1510 does fire, the explicit constructor stays for that class and the conflict is recorded here. Because no test covers the guards today, a test for each guard is written first and passes before the move; it then holds the behaviour through the refactor.

For `CurrentSettings`, `value` stays a mutable field (it is replaced on a save through `Volatile.Write`) initialised from `initial ?? throw ...`; `store` becomes a readonly field initialised the same way.

### `NuGetPackageVersions` takes its handler from `Program.cs`

`public sealed class NuGetPackageVersions(ToolVersion version, HttpMessageHandler handler) : IPackageVersions, IDisposable`, with `private readonly HttpClient client = ClientFor(version ?? throw ..., handler);` and a private static `ClientFor` that sets the base address, the 3-second timeout and the `Vantage/<version>` user agent, as the constructor body does today. The one-parameter constructor goes. `Program.cs` passes `new HttpClientHandler()`, the handler that constructor created, so the request is unchanged. `HttpClient`'s own constructor already throws `ArgumentNullException` for a null handler, so the handler needs no guard of its own. `HttpClient(handler)` disposes the handler with the client, so ownership is as before and the class stays `IDisposable`.

The nuget.org facts (address, timeout, user agent) stay in the adapter, where `NuGetPackageVersionsShould` covers them through its `StubHandler`. Building the client in `Program.cs` would turn tested behaviour into untested wiring.

Alternatives considered: `IHttpClientFactory` (rejected in the proposal); `SocketsHttpHandler` in place of `HttpClientHandler` (on .NET 10 `HttpClientHandler` already runs on `SocketsHttpHandler`, and switching is a behaviour question that does not belong in a refactor).

### `[LibraryImport]` with an inline-array buffer for the executable name

Every `[DllImport]` becomes a `[LibraryImport]` on a `static partial` method in a `partial` class. `SetLastError` carries over unchanged. `[return: MarshalAs(UnmanagedType.Bool)]` and the `bool` parameter's `[MarshalAs(UnmanagedType.Bool)]` stay, because the generator supports them. `Process32FirstW` and `Process32NextW` are already named for their W entry points and take no string parameters, so they need no `StringMarshalling`.

`ProcessEntry` cannot pass by `ref` through the generator while it holds a `ByValTStr` string. Its last field becomes an `[InlineArray(260)]` struct of `ushort` (C# 12), read through `MemoryMarshal.Cast<ushort, char>` up to its first zero. That makes the structure blittable without `unsafe` code or a `fixed` buffer, and `CharSet.Unicode` comes off its `StructLayout` because no string remains. `ushort` rather than `char`: Microsoft's interop guidance counts `char` as blittable inside a struct only when the struct is marked `CharSet.Unicode`, and `bool` never, so a `char` buffer leaves the generator's verdict (or a SYSLIB1051 refusal) to chance. `[assembly: DisableRuntimeMarshalling]`, which would make `char` plain, is not open here: `UiAutomationTabs` relies on built-in COM interop, which needs runtime marshalling. The other structures (`ProcessBasicInformation`, `UnicodeString`, `ProcessMemoryCountersEx2`) are blittable already.

The generator requires `AllowUnsafeBlocks` even when the generated code needs no `unsafe` in the source, so the Infrastructure project sets it. Because a `.csproj` carries no comments, the reason goes in `src\Source191.Vantage.Infrastructure\README.md`.

Alternative considered: keeping `[DllImport]`. It works, but it marshals at run time through IL stubs and blocks trimming and native AOT. The tools project and SYSLIB1054 both point the same way, and this is the moment to move.

### `CapturedRun` locks on a dedicated `Lock`

`lock (output)` and `lock (replies)` become `lock (gate)` on one `private readonly Lock gate = new();`, as `DiagnosticLog`, `ProcessCounts` and the fakes already do. One gate is enough: the two collections are written from different reads of the same pseudo-console and never held together. IDE0330 does not catch this case (it only flags locks on an `object` field), so it is a hand fix.

### Switch statements become expressions only when they choose a value

Of the nine `switch` statements, those whose every arm assigns one variable or returns a value become switch expressions. Those whose arms run effects (`TableBody`, `TerminalEffects`, `ScreenReplay`, the `Program.cs` start-up) stay statements, because a switch expression of side-effecting lambdas reads worse. IDE0066 stays at `suggestion` for the same reason.

## Risks / Trade-offs

- **[The code fixer for IDE0090 or IDE0300 changes a type the code relied on]** For example, a collection expression assigned to `IEnumerable<T>` builds a compiler-chosen type rather than an array. → The full suite runs after each rule's commit; anything that compares types by reflection (`Architecture\*`, `WordingOfOutcomesShould`'s sample values) shows it at once.
- **[Interop structure layout changes]** An inline array of the wrong length, or reading past the NUL, would misreport process names, which feeds memory figures, `TerminalTab` and `ProcessTree`. → `ProcessRegistryIntegrationShould` reads the real process table (the test process's own name and parent chain) and keeps two hundred queries under a second; `ConsoleRecoveryShould` covers the console calls. The interop commit stands alone so it can be reverted without the style work.
- **[`AllowUnsafeBlocks` permits `unsafe` code anywhere in Infrastructure]** → Nothing in the change writes `unsafe`, and IDE0380 (raised in the same change) flags an unnecessary `unsafe` modifier.
- **[A raised rule fires on code written in another open change]** Six changes are in flight. → Whichever merges second rebases and runs the build, and the analyser lists exactly what to fix.
- **[Analysers flag the handler handed to `NuGetPackageVersions` as not disposed]** → The client owns and disposes the handler. If CA2000 or a Sonar rule objects, the construction moves into a single expression in `Program.cs`, without a suppression.
