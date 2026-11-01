# Design

## Context

See proposal.md for why. The facts that shape the approach, read on `main` ac7cdae0:

- `DiagnosticLine.Body` renders a crash as `failed Crash: <summary>` or `failed Crash <what>: <summary>` (`DiagnosticLine.cs:43`); `ProblemReports.cs:19, 52-53` keeps a crash line only when it contains `" failed Crash: "`. `CrashFileShould` (Infrastructure.Tests) writes the line as bytes to test the adapter; the Core.Tests fixtures spell it to test the report.
- `Attempt.RunAsync` (`Attempt.cs:11-27`) catches every `Exception`. The five catches it would replace are narrower: `GitException` in `WorktreeInventory`, `IOException or UnauthorizedAccessException` in `SetupFactsReader`, `not OperationCanceledException` in `DetailLoader`. The scan runs every 15 seconds while its tab is shown, serialised by `RunningLoads`; `DetailLoader` runs off the UI thread and two reads can overlap. `DiagnosticEventsShould` allows a `Failed` only `FailureSite`, `CodeName` and `ExceptionSummary`.
- `TabPalette.Default` is one ordered list; `TabColourSelection` takes it whole and a registration stores its colour, so a named member changes no state file.
- `GitHubPullRequests.cs:50-70` and `AzureDevOpsPullRequests.cs:57-77` are the same parse; a row missing a property throws `KeyNotFoundException`, caught as `PullRequestGap.Failed` for the whole listing. `openspec\specs\pull-requests\spec.md:114` already leaves out a row whose link is not https "as if the host had not listed it"; no requirement covers a malformed row.
- `tools\HeadlessTerminal` references Core only and `HeadlessTerminal.Tests` references it alone; `TabEscapes` is Infrastructure. `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors` are on, so a style rule at warning fails the build for `src`, `test` and `tools` alike; the multiline search found 8 double blank lines under `src`, about 17 under `test` and none under `tools`.
- `tidy-composition-root` (open) moves `Program.cs:144-158` into `ConsoleComposition`.

## Goals / Non-Goals

**Goals:**
- Each repeated rule or format has one home, and the tests that pinned a copy now pin the home.
- Every catch that answers a blank also leaves a trace a problem report can show, without flooding the log.
- Behaviour identical to today everywhere a screen, a key, a file or a command line can tell.

**Non-Goals:**
- Any new word on screen: the counts-unavailable marker, the reducer nits and `Wording`'s own repetition stay with their changes.
- Testing the tool project or the Tui dialogs beyond the build and the smoke check.

## Decisions

**The crash line is recognised by `DiagnosticLine`, by the word and the site.** `public static bool IsCrashLine(string line)` matches `" failed Crash"` (a space, the `failed` word the `Failed` arm uses, a space, `FailureSite.Crash`), with no colon, so a crash with a `What` is kept too. The word and the site are shared constants with the arm, so renaming either moves both. The Core.Tests fixtures build their crash lines with `DiagnosticLine.Render(new TimedEvent(at, new Failed(FailureSite.Crash, null, ExceptionSummary.From(new InvalidOperationException()))))` (an exception never thrown has no frames, which is today's literal); `DiagnosticsPrivacyShould` and `ProblemReportShould` (whose line carries a stack frame) assert `IsCrashLine` instead of the literal; `CrashFileShould` keeps its literal, since it tests bytes round-tripping, not the format. The alternative, `CrashRecorder` writing the crash as the file's last line and `ProblemReports` keeping the last line, was rejected: it changes the crash file's shape for a problem that is a missing method.

**Swallowed failures are recorded through `Attempt`, with the catch shapes kept.** `Attempt.RunAsync` gains an overload taking `Func<Exception, bool> handles`, used as the catch's `when` filter; the existing overload delegates with `_ => true`. Routing the five catches through the unfiltered method would widen them: a cancelled scan would answer `[]` instead of propagating, and a `SetupFactsReader` bug would read as "unreadable". A new `Application\Diagnostics\OncePerFolder(DiagnosticLog log, FailureSite site, CodeName what)` holds the gate for the scan and the detail reads: `RunAsync<T>(FolderPath folder, Func<Task<T>> action, Func<Exception, bool> handles, Func<Exception, T> onFailure)` records the `Failed` the first time a folder fails in this process run and answers `onFailure` every time, keyed by a `ConcurrentDictionary<FolderPath, byte>` because `DetailLoader`'s reads overlap. The folder is a key in memory and never reaches the event, which stays `Failed(site, what, summary)` as `DiagnosticEventsShould` requires. `WorktreeInventory` and `DetailLoader` each take a `DiagnosticLog` and build one gate per catch site (`CodeName.For(typeof(WorktreeInventory))`, `CodeName.For(typeof(DetailLoader))`); `SetupFactsReader` reads rarely (each Config show and save) and uses the filtered `Attempt.RunAsync` with no gate. The alternative, a `log.Record` line inside each existing catch, keeps the shapes but copies the record-then-answer pattern `Attempt` exists for five more times.

**`TabRegistrations.For(SessionId)` is the fourth door on the first-class collection.** It answers `IReadOnlyList<TabRegistration>` whose owner equals `TabOwner.ForSession(id)`, built once rather than per element. `OpenTabHandler.OpenAsync` reads the store once: it removes `For(id)` from the store and computes `live` from the same read minus those registrations, where today it reads again (`OpenTabHandler.cs:36, 41`). `SessionTabs` and `RenameSessionHandler` call it; `SessionRowEnricher.LiveTabsAsync` keeps its loop, which groups by owner rather than filtering by one.

**The palette names its colours and builds `Default` from them.** `TabPalette` declares each colour as a static member (`Red`, `Blue`, `Green` and so on) and `Default` as the ordered list of them, so the order fact in `TabPaletteShould` still holds and no colour is spelled twice. `OwnTab` and `TabScripts.Trust` take `TabPalette.Green`; `TabScripts.Shell` takes `TabPalette.Last`, a property answering `Default[^1]`, because its intent is "never looks like a session", not "yellow". `TabScriptsShould` and the five tests that find green with `Single(colour => colour.Name == "Green")` use the member. The alternative, `TabPalette.Named("Green")`, keeps a string where a member will do. One question for the owner: the trust tab shares Vantage's own green today (`Default[2]` is Green); this change keeps that and makes it visible as `TabPalette.Green` on both.

**`FolderLabels` and `StartUpdateHandler` take the types the composition already holds.** `FolderLabels(CurrentSettings settings, FolderPath home)` computes `[.. settings.Value.RepositoryRoots, settings.Value.WorktreeRoot]` on each call, so a save still shows on the next redraw without a restart, and `FolderLabelsShould.FollowASavedConfiguration` saves through `CurrentSettings.SaveAsync`. `StartUpdateHandler(IWindowsTerminal, TabScripts, VantageProcess)` reads `ProcessId` and `Home` itself. `tidy-composition-root` lands first; the two call sites are then in `ConsoleComposition` and `Composition.Handlers`.

**The pull-request adapters keep their vocabularies and share the shell and the parse.** `CliListing.RunAsync(IProcessRunner, ProcessRequest, Func<ProcessResult, PullRequestListing>, CancellationToken)` holds the `Win32Exception` to `CliMissing` shell, and `PullRequestListingJson.Rows(string json, Func<JsonElement, BranchPullRequest?> row)` the `JsonDocument` walk and its catch; each adapter keeps its arguments, its `Row`, its `State` and its `Gap`. The shared `Rows` decides once what a row missing a property means: it is skipped, read with `TryGetProperty`, as `ClaudeAgentsListing` skips an odd row and as the spec already leaves out a non-https row; today it fails the whole listing. This is the one edge in the change that moves, in output the hosts never produce, so it stays under `skip_specs`; the owner may overturn it by keeping `GetProperty` in the shared parser, which keeps today's `Failed`.

**One ECMA-48 scanner in the headless terminal.** `EscapeSequences.EndOf(ReadOnlySpan<char> text, int start)` answers the index after the sequence that starts at `start`: `ESC [`, then parameter bytes `0x30` to `0x3F`, intermediates `0x20` to `0x2F` and a final `0x40` to `0x7E`; `ESC ]` and `ESC P` to BEL or `ESC \`; any other `ESC x` is two characters; an unterminated sequence runs to the end. `Strip`, `CaptureMarker.ScreenText.FindAfter` and `ScreenReplay.Escape` use it; `ScreenReplay.ControlSequence` reads its final byte at `end - 1` and strips the intermediates before splitting the numbers, so `,|` falls to its `default` arm as today's unknown finals do. `TitleSequence` stays a regex because it extracts a group. `TerminalQueries` becomes `partial` with six `[GeneratedRegex]` methods and no timeout (anchored fixed patterns cannot backtrack). `HeadlessTerminal.Tests` gains a reference to `Source191.Vantage.Infrastructure` so `EscapeSequencesShould` reads `TabEscapes.ResetBackground` rather than a copy, recorded in `tools\HeadlessTerminal.Tests\README.md`.

**`DialogButtons` is a Tui static of its own.** `Handled(Action)` and `Button(string, Action)` move to `Tui\DialogButtons.cs` (one type per file); `WizardControls.WhenEnterInSelectors` and the three dialogs call it. No test: the dialogs are covered by the smoke check, as today.

**Mechanical tidies need no decision:** `FolderKind` (`Repository`, `Candidate`, `EmptyLeftover`, `Other`) nested in `WorktreeInventory` with a switch statement, since each case adds to a different list; the `if` for `SessionRowEnricher.cs:35`; the `using`; `shown` into `Frame`; the comments; the blank lines, policed by IDE2000 once the rule is on.

## Risks / Trade-offs

- [IDE2000 at warning fails the build on every double blank line in `test` too] → one task sets the rule and runs `dotnet format style --diagnostics IDE2000` over the solution before anything else is committed; `test-hygiene` touching the same files rebases.
- [The scan now records a `Failed` per unreadable repository] → once per folder per process run; a machine with every root broken adds one event per repository, not one per 15 seconds.
- [`OncePerFolder` holds folders in memory for the process's life] → a set of paths already read from the disc every scan; nothing is written or logged.
- [A malformed pull-request row is skipped where it failed the listing] → flagged above; `GitHubPullRequestsShould` and `AzureDevOpsPullRequestsShould` each pin it, so the choice is explicit either way.
- [`EndOf` is wider than the regexes it replaces] → `EscapeSequencesShould` keeps today's cases (CSI with `?`, `<`, `>`, `=`, OSC and DCS to BEL or ST) beside the DECAC one, and `CaptureMarkerShould` and `ScreenReplayShould` stay green.
- [Dropping `default: break;` may raise Sonar S131] → the task's verify step is the build; if S131 fires, the line stays and the task records why.
- [Three constructors gain a `DiagnosticLog`] → `Program.cs` (or `ConsoleComposition`) already holds the one log; the test sites are named in tasks.md.

## Migration Plan

Nothing to migrate: no state file, key, setting or command line changes. Rollback is reverting the pull request.
