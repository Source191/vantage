# Design

## Context

See proposal.md for why. Every `!` below sits after a check the compiler cannot follow: a `Where` that filtered nulls, an `All` over a nullable array, a `?? -1` that makes the index negative, a field checked in a different method. The fix in each case is to let the check and the use be one pattern, so the compiler sees the value is there. Shared-snapshot merges first and adds `NewestRelease.Recorded`, which calls `ReleaseVersion.TryParse`; this change alters that one line.

## Goals / Non-Goals

**Goals:**
- No null-forgiving operator in `src`.
- `TryParse` means one thing in `Domain`: `bool` with an `out` value.

**Non-Goals:**
- Removing the nullable declarations around the `!` sites where they come from outside (`IProcessRegistry`'s answers, Terminal.Gui's `SixelSupportResult`, the JSON fields `EditorChoice.FromFile` receives): the later changes in the Order own them.
- The null-forgiving operators in the test projects beyond those the `TryParse` change removes.

## Decisions

**`ProcessRegistry.WithProcess` takes the gone answer, and actions get their own overload.** `WithProcess<T>(int processId, Func<Process, T> query, T whenGone)` returns `whenGone` from its three `catch` arms; `StartTimeOf` and `ParentOf` pass `null` because `IProcessRegistry` declares those answers nullable today (null-at-the-boundary decides that). `WithProcess(int processId, Action<Process> action)` runs `Terminate` and `EndCleanly` and swallows the same three exceptions, so `WithProcess<object?>` and its dummy `return null` go. Alternative: `Func<Process, bool>` returning a dummy `true` (a different dummy).

**`TableBody` passes the checked detail parts on.** `AskForDetail` matches `detail is { } source && requests is { } asked && pane is { } shown` (with `isShown`) once, and hands `source`, `asked` and `shown` to `ReadDetail`, `ReadAsync` and `ShowDetail` as parameters; `ShowDetail`'s `requests?.Accept` becomes `asked.Accept`. The three fields stay nullable here. The brief for this change suggested a `NoDetail<TRow>` with no rows, but `DetailPane` always draws a one-row divider (`Rows = lines + 1`) and needs content to show, so it would change the Config tab's layout; modelling a tab without a pane is null-free-screens' work.

**`OwnerOf` matches the text it slices.** `gitDir is string dir && dir.IndexOf(WorktreesSegment, ...) is int at and >= 0 && FolderPath.TryParse(dir[..at], ...)`. `GitDirOf` keeps its nullable answer (null-free-rows models the `.git` pointer).

**`EditorChoice.FromFile` matches both values before using them.** `EditorPreset.Custom when executable is string path && arguments is { } list && Problem(path, list) is null => Custom(path, list)`. This answers as today: a missing `arguments` read as `[]`, which has no folder placeholder, so `Problem` refused it and the arm fell through to VS Code; the new arm falls through for the same input.

**`ReleaseVersion` parses into plain ints and answers through `out`.** `TryParse(string? text, [MaybeNullWhen(false)] out ReleaseVersion version)` matches `parts is [string major, string minor, string patch]` and `Number(major) is int a` (and so on) in one condition, so `int?[] numbers` and its `!` go; the private `Number` keeps its `int?` (a value type, local to the file, also used by `CompareIdentifier`). The class summary stops saying "answers null". `UpdateCheckRule.Newest` needs the texts that parse: a static `ReleaseVersion.Parsed(IEnumerable<string>)` keeps them, as `Parsed<T>()` does for text values. `ReleaseOf`, `ClaudeCodeSupport.Check` and `NewestRelease.Recorded` use `TryParse(..., out ...)` in their conditions.

**Tests build versions through one helper.** About twenty test lines write `ReleaseVersion.TryParse("0.10.0")!`. A `ReleaseVersions.Version(string text)` helper in `test\Source191.Vantage.Core.Tests` asserts the text parses (`ShouldBeTrue`) and returns it, so a typo fails at the helper with the text named. `ClaudeContractShould` checks `TryParse(version, out _)` with `ShouldBeTrue` and the same message.

**`RemoteRepository.TryParse(string? url, [MaybeNullWhen(false)] out RemoteRepository repository)`.** The private `Azure` helper answers through `out` too. `PullRequestCatalogue` reads `if (!RemoteRepository.TryParse(url, out RemoteRepository? repository))`. The class summary's "reads as null" becomes "is not read".

**`ExceptionSummary` keeps the method it filtered on.** `StackFrame.GetMethod()` can answer null (frames without metadata, trimmed or native code), which is why the code filters. `Frames` becomes an iterator that matches `frame.GetMethod() is MethodBase method` once and passes `method` to `IsAwaitMachinery(MethodBase)` and `Frame(StackFrame, MethodBase)`; the output is unchanged.

**`VantageIcon.Fit` matches the support it reads.** `support is { IsSupported: true, Resolution: System.Drawing.Size cell }` decides visibility and gives the cell size in one pattern. The parameter stays nullable until null-at-the-boundary passes a Vantage value from `ConsoleScreen`.

**A folder with no parent is not created.** In `WorktreeIncludeCopier` and `JsonFileStore`, `if (Path.GetDirectoryName(target) is string folder) { Directory.CreateDirectory(folder); }`. `GetDirectoryName` answers null only for a root or null path; neither caller passes one (both combine a file name onto a folder), and a root needs no creating, so skipping is correct where today `CreateDirectory(null)` would throw.

**`Architecture\ParsingShould` keeps the convention.** Over the types in `Source191.Vantage.Domain`: every declared static method named `TryParse` returns `bool`, and no type declares a static method named `Parse`. It reuses `TextValuesShould`'s reflection style. `FindEveryParser` lists the types it found (the seven text values, `JobId`'s second overload included, plus `ReleaseVersion` and `RemoteRepository`), so a new parser shows up in review. A wider rule (no static `Domain` member returns a nullable reference) is null-guard-rule's.

## Risks / Trade-offs

- [`WithProcess`'s action overload swallows a failure the old version also swallowed] → same three exception types, same comment; `ProcessRegistryIntegrationShould` covers a gone process.
- [`Tui` has no test project, so the `TableBody` and `VantageIcon` edits rest on the build and the headless checks] → the smoke check opens the Sessions, Worktrees and Config tabs of the packed tool, and the edits only move existing checks into patterns.
- [A `TryParse` caller missed] → the old signature is gone, so a missed caller fails the build.
