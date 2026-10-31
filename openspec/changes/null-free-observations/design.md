# Design

## Context

See proposal.md for why. As of main at `356588b`: `TabRegistration(Id, Owner, Colour, CreatedAt, int? ProcessId, DateTimeOffset? ProcessStartedAt)` is written to `%LOCALAPPDATA%\Vantage\tabs\<id>.json` by `JsonFileStore` with System.Text.Json's defaults, `TabOwnerJsonConverter` and the `StateFileNames.KeepTabOwnerAsSessionId` modifier, and `JsonFileStoreShould.ReadTabFilesWrittenByTheCurrentVersion` pins the file text. `IsLive` reads the nulls as pending (a 60-second grace), `SessionTabs` ends a claimed shell with `tab is { ProcessId: int shell }`, and `OwningTerminal` walks up from a claimed shell. `WorktreeHealth.From` matches `WorktreeObservation`'s `LockReason: not null`, `GitFileTargetExists: true` with an `OwnerRepository`, and `GitFileTargetExists: false`; `WorktreeInventory` builds observations in four places and `WorktreeInspector` in one. `ProcessTree.Build` keeps a child whose `usageOf` answered null with a null `Usage`, and `MemoryBreakdown` reads `Usage?.CommandLine ?? ""` and `Usage?.PrivateMemory ?? MemorySize.Zero`.

## Goals / Non-Goals

**Goals:**
- No nullable reference or value type left on these Domain records; null from System.Text.Json, git, the CLI listing and the process registry met on the one line that reads it.
- Tab files byte-compatible in both directions.

**Non-Goals:**
- Changing `IProcessRegistry` or the tab file format.

## Decisions

**`TabClaim` is `Pending` or `Claimed(int ProcessId, DateTimeOffset StartedAt)`.** `TabRegistration.Pending(...)` starts with `Pending`, `ClaimedBy(processId, startedAt)` replaces it, `IsLive` switches on it (grace period for `Pending`, `processes.StartTimeOf(ProcessId) == StartedAt` for `Claimed`), and `SessionTabs` matches `Claimed { ProcessId: int shell }`. `OwningTerminal` keeps its `int?` answer: it comes from `TerminalTab`'s walk of the operating system's parent chain, which answers null when the chain does not reach Windows Terminal, and feeds `IWindowsTerminal.FocusTabAsync(int?)`, both left for `null-at-the-boundary`. Alternative: a hierarchy of registrations (rejected: `Id`, `Owner`, `Colour` and `CreatedAt` are shared, and only the claim varies).

**Tab files go through a `TabFile` record.** `TabFile(Guid Id, TabOwner SessionId, TabColour Colour, DateTimeOffset CreatedAt, int? ProcessId, DateTimeOffset? ProcessStartedAt)` in `Adapters\State` is what `JsonFileStore` serialises, with `From(TabRegistration)` and `ToRegistration()`; the nullables live only there, the accepted shape of a JSON data object. Its `SessionId` property keeps the file's key, so the `KeepTabOwnerAsSessionId` modifier goes. A file holding only one of the two reads as `Pending`. No version has written one (`ClaimedBy` sets both), so the only difference from today, where such a file's liveness depends on whether the process has gone, is in a file someone edited by hand. Alternative: a `JsonConverter<TabRegistration>` (rejected: hand-written reading and writing of six properties, where a record and a mapping say the same in less).

**`WorktreeLock` is `Unlocked` or `Locked(string Reason)`.** The reason is empty for a lock with no reason, which `HealthReason.Locked` and `Wording` already show as today. `GitWorktreeListing` gives `Locked(value)` for a `locked` line and `Unlocked` otherwise; `WorktreeObservation` and `GitWorktreeEntry` both hold it.

**`GitPointer` is `NoGitFile`, `TargetGone`, `TargetExists` or `RegisteredElsewhere(FolderPath Owner)`.** It replaces `GitFileTargetExists` and `OwnerRepository`, which only ever meant one of these four together. `WorktreeHealth.From` maps `RegisteredElsewhere` to `Elsewhere` with `RepositoryOutsideRoots(owner)`, `TargetGone` to `Broken` with `RegistrationGone`, and `NoGitFile` and `TargetExists` fall through to the orphan, empty and not-registered arms, as `true` without an owner and null do today. `WorktreeInventory`'s private `PointerOf` reads the `.git` file once (its `FirstOrDefault` line is the boundary) for both `OrphanRows`, which never names an owner, as today, and `BrokenWorktree`, which names one only when the target exists and the owner still has its `.git` folder.

**`GitWorktreeEntry.IsPrunable` replaces `PrunableReason`.** Vantage only tests whether the reason is present; the text is read nowhere, and `GitWorktreeListingShould.ReportThePrunableReason` becomes a fact about the flag.

**`ProcessMeasure` is `Measured(ProcessUsage Usage)` or `Unmeasured`.** `Build` translates `usageOf`'s answer on the line that calls it; `Total` sums the measured, `MemoryBreakdown` gives `""` as the command line and `MemorySize.Zero` as Claude Code's own figure for `Unmeasured`, as today. The root of a tree is always measured (`Under` refuses otherwise).

**`SessionMemory` is `Measured(MemorySize Size)` or `NothingToMeasure`.** `SessionRowEnricher.MemoryOf` builds it from whatever `ProcessTree.Under` answers after `null-free-outcomes`; `BackgroundSession.ProcessId`'s `int?` stays until `null-free-sessions`. `MemorySize.Zero` is not a stand-in: the pane would read "0 MB" where it reads "nothing to measure".

**`WaitingReason` is `Nothing` or `Given(string Text)`.** `ClaudeAgentsListing` translates the listing row's `string?` on the line that builds each session. `WordingOfDetail`'s helper answers a `WaitingReason` too, adding "a first prompt" as `Given` when the session needs input and has no transcript, and the pane's switch gives "nothing" in plain or the text in amber, as today.

**`ReleaseVersion.Prerelease` is non-nullable, empty for a release.** `IsPrerelease` is `Prerelease.Length > 0`, `ToString` and `ComparePrerelease` test emptiness. A named case would be ceremony here: the grammar has no empty prerelease, so empty cannot be confused with a real one.

## Risks / Trade-offs

- [A tab file stops reading] → `JsonFileStoreShould` keeps its two current-version files, gains a pending one, and checks a written file's text, so a format change fails a fact.
- [A health arm changes meaning] → `WorktreeHealthShould` covers every arm today; its facts keep their expected kinds and reasons.
- [Two changes touch `GitWorktreeEntry` and `SessionRow`] → `null-free-rows` merges first; this change re-reads both before task 2.1 and 4.2.
