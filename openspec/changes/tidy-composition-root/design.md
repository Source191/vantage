# Design

## Context

See proposal.md for why. `Program.cs` (218 lines at `main` b097e04a) is top-level statements with five local functions: `ClaimTabAsync`, `SetUpTerminalAsync`, `StartHereAsync`, `OpenConsoleAsync` (with `ReadConfigAsync`, `LoadAsync` and `ScanAsync` nested inside it), `LocateGitAsync`, `LocateClaudeAsync` and `RecordStart`. The tool project has no test project and is outside `LayeringShould`'s assemblies; it may reference Core, Infrastructure and Tui, and `Application` may reference neither of the last two. `ClaudeLocation` and `ProgramLocation` are already `Ports` types, `ClaudeExecutable.Locate` and `ProgramLocator` are Infrastructure, and the three sentences are `Wording.ClaudeIsABatchFile(path)`, `Wording.ClaudeNotFound` and `Wording.GitNotFound`. `Started` is `Application\Diagnostics`' start event, and its nullable `ClaudeCode` is on `NullFreeModelsShould`'s kept list. `ScreenContext.SettingsRows` is a `Func<int, IReadOnlyList<IReadOnlyList<TextPart>>>` given the width left after the icon, which today's lambda ignores; `ConsoleScreen` takes its loads as four delegates. Core.Tests already builds `SessionRowEnricher`, `WorktreeInventory`, `SetupFactsReader`, `LatestSessions` and `UpdateCheck` over the shared fakes.

## Goals / Non-Goals

**Goals:**
- Every decision in today's `Program.cs` sits in a tested `Application` class; what stays in the tool project only constructs objects and hands them on.
- `Program.cs` reads as the process's start: what every command shares, the crash hooks, the command line.
- Behaviour identical to today, including the order of the start-up work, the sentences, the exit codes and the overlap of `claude --version` with the rest of the console's start.

**Non-Goals:**
- Testing the tool project: its classes are wiring, as `Program.cs` is now.
- Shortening `Composition.Handlers`, `EffectPorts`, `DialogEffects` or `ConsoleScreen`'s constructors.

## Decisions

**Hand-written construction, no container.** Microsoft.Extensions.DependencyInjection was considered and rejected, as the `effect-handlers` change did for the handlers. Many constructor arguments cannot be resolved by type: three strings (the claude path, the vantage path, the tab title), four delegates on `ConsoleScreen`, a `Func<bool>` on `UpdateCheck`, three `ProgramLocation` values for different programs, and `JsonFileStore` serving several ports. Each would need a factory lambda, which is today's code moved into registrations. The start-up order is async (`CurrentSettings.LoadAsync`, and the version call awaited just before `Started`), and containers have no async factories. The locate step must print and exit before anything is built. And a missing registration would fail at run time in an untested project, where today it fails the build.

**`RequiredPrograms` is a closed two-case result built from values.** `public abstract record RequiredPrograms` with `Found(string Claude, string Git)` and `Missing(string Sentence)` nested inside it, and `static RequiredPrograms Check(ClaudeLocation claude, ProgramLocation git)` in `Application`. The composition passes `ClaudeExecutable.Locate()` and `programs.Locate("git")`, so the class needs no adapter and its facts need no fake. Claude Code's problem is reported before git's, as today. Both programs are now located before either is judged; locating git is a PATH search that starts no process and prints nothing, so this changes nothing a person sees. Paths stay strings, as `ClaudeLocation.Found` and every adapter that takes one already have them. A switch over it names both cases and its discard arm throws `UnreachableException`, the house rule for a two-case hierarchy. The sentence is taken from `Wording` inside `Check` because it is printed once, straight away, and never shown again; the alternative, a `MissingProgram` hierarchy that `Wording` renders, adds three types for one `WriteLine`. This supersedes the `null-at-the-boundary` decision to keep `string?` here.

**One class per extracted decision, each in `Application`:**
- `ConsoleLoads(LatestSessions sessions, SessionRowEnricher enricher, WorktreeInventory inventory, SetupFactsReader setup, CurrentSettings settings)` with `LoadSessionsAsync`, `ScanWorktreesAsync`, `ReadConfigAsync` (each a `Task<ViewInput>`) and `ConfigRowsWhileReading()`. The composition passes the method groups to `ConsoleScreen` (the scan still through `scans.Track<ViewInput>(...)`), so no Tui signature changes. One `LatestSessions` is shared by the load and the scan, as now.
- `Started.From(ToolVersion vantage, ClaudeVersionReport claude, Version windows, Version runtime, int columns, int rows)`: the factory reads the report (a `Printed` text that `ClaudeVersion.TryParse` accepts, otherwise no version). The composition supplies `Environment` and `Console` values and records the event.
- `UpdateCheckSwitch(CurrentSettings settings, string optOutVariable)` with `bool IsOn()`: the variable is judged once by `UpdateCheckOptOut.Applies`, as today, and the setting is read on every call, so a save applies without a restart.
- `HeaderSettingsRow(ToolVersion vantage, ClaudeCodeVersion claude, UpdateCheck updates, IProcessRegistry processes, int processId)` with `Rows(int width)`: one row from `Wording.SettingsRow`, the update status and memory read per call, `MemorySize.Zero` when `UsageOf` answers null. The width is accepted and unused, as today.

**The tool project's `Composition` folder** (namespace `Source191.Vantage.Composition`, every type `internal sealed` or `internal static`, one per file):
- `SharedParts`, a record of what more than one command uses: the store, the process registry, the tool version, the vantage executable, the terminal fragment, the program locator, the log setting, the log, the process counts, the recorded process runner, the crash file and the crash recorder. `SharedParts.ForCurrentUser()` builds it from the environment exactly as lines 26 to 45 do now, and `RecordStart(ClaudeVersionReport)` records `Started.From(...)` and returns it.
- `LaunchParts(RequiredPrograms.Found Programs, ClaudeCli Claude, GitCli Git, LaunchService Launches)` with `static Task<int> RunAsync(SharedParts parts, TextWriter error, Func<LaunchParts, Task<int>> run)`: checks the required programs, writes the sentence and answers 1 on `Missing`, otherwise builds the three objects once and runs. That is the console's and `vantage here`'s shared preamble.
- `ConsoleComposition.RunAsync(SharedParts, LaunchParts, CancellationToken)`: today's `OpenConsoleAsync` body, split into a few private static methods by concern (pull requests, the dispatcher and its ports, the dialogs, the screen), with every argument list over the line length broken into named locals. The version call is still started first and awaited just before `RecordStart`, and the `using` lifetimes (the version timeout, `PullRequestCatalogue`, `NuGetPackageVersions`) stay inside `RunAsync`.
- `HereComposition.RunAsync(SharedParts, LaunchParts, HereName, string, CancellationToken)` and `TerminalSetupComposition.RunAsync(SharedParts, TerminalSetupRequest, CancellationToken)`: today's `StartHereAsync` after its preamble, and `SetUpTerminalAsync`.
- `ClaimTabAsync` stays a two-line lambda in `Program.cs`; a class for it would be wiring around one call.

**`Program.cs` keeps the process-wide start:** build `SharedParts`, hand Terminal.Gui its logger, attach the two crash hooks, build the `CommandActions` (each wrapped in `crashes.GuardAsync` as today) and run `CommandLine.InvokeAsync`. The order is today's: the logger and the hooks are in place before any command runs.

**Move first, tidy second.** Each wiring step first moves code verbatim into its new home and builds, then breaks the long lines in a separate refactor commit, so a reviewer can see that nothing changed in the move.

## Risks / Trade-offs

- [The wiring move changes behaviour unseen, because the tool project has no tests] → verbatim moves before tidying; the solution's tests; the hand-off check (`dotnet run --project tools\HeadlessTerminal -- check`), which runs `vantage here` and a real attach; and CI's `check` job for the smoke check, which run locally would act on the owner's real repositories (as in `null-at-the-boundary` 2.7).
- [`RequiredPrograms` now locates git even when Claude Code is missing] → a PATH search with no process and no output; the sentence and exit code are unchanged, and `RequiredProgramsShould` pins Claude Code's sentence winning when both are missing.
- [Three open changes edit the same code (`performance-diagnostics`, `show-release-notes`, `standardise-maybe-null-when`)] → whichever merges second rebases; this change gives `performance-diagnostics`' timing a tested home in `ConsoleLoads`. If `standardise-maybe-null-when` lands first, `Started.From` calls its new `TryParse` signature, which changes no fact.
- [`SharedParts` has twelve members, against the object callisthenics guideline of two] → it is a bag of construction results in an untested project, not a domain object; splitting it further would add wiring without removing any.
- [`Missing` carries text rather than a value `Wording` renders later] → acceptable for a sentence printed once on start-up; `RequiredProgramsShould` compares it with `Wording`'s members, so the words keep one home.

## Migration Plan

Nothing to migrate: no state file, key, setting or command line changes. Rollback is reverting the pull request.
