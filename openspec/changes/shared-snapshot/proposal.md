# Proposal

## Why

Three fields are written by one thread and read by another, and each access goes through `Volatile.Read` or `Volatile.Write`: `CurrentSettings.value` (a save runs off the UI thread, the UI and scans read), `UpdateCheck.newest` (the start-up check runs on a pool thread, the header reads) and `ScreenClipboard.current` (the UI thread attaches and detaches, copy handlers read from pool threads). The idiom is correct but unfamiliar to most .NET developers, it is repeated at every access with no comment saying why, and a later edit could add a plain access beside it without anyone noticing. The owner reviewed the options on 31 October 2026 (`Volatile` with comments, the `volatile` modifier, a lock, `Interlocked`, a wrapper type) and chose a wrapper that holds the explanation once and never holds null.

## What Changes

- **`SharedSnapshot<T>`** in `Application`: one immutable value replaced whole by one thread and read by others. It holds a `volatile` field, its comment says why (cites the CoreCLR memory model), and `T` is constrained to a non-nullable reference type, so it never holds null. `Value` reads it; `Replace` swaps it.
- **`CurrentSettings`** holds a `SharedSnapshot<Settings>`; `Value` and `SaveAsync` read and replace through it.
- **`UpdateCheck`** holds a `SharedSnapshot<NewestRelease>`, a new two-case Domain record (`None`, `Found(ReleaseVersion)`) standing where `string? newest` stood; `UpdateCheckRule.Offer` takes a `NewestRelease` instead of a `string?`, and `UpdateCheckRule.Newest` answers one instead of a `ReleaseVersion?`. The update-check port's nullable record and text are translated where `UpdateCheck` reads and writes them, so no null reaches Domain or the snapshot.
- **`ScreenClipboard`** holds a `SharedSnapshot<IClipboard>`: a private `NoClipboard` (answers false) while no application runs, and a private application-backed clipboard while one does, so its `IApplication?` field goes; the one null check left is on Terminal.Gui's own nullable `Clipboard`.
- **`CLAUDE.md`:** one sentence in the code rules saying that a value one thread replaces and another reads is a `SharedSnapshot<T>`, never a bare `Volatile` access.
- **Version:** `0.19.0-preview.3`, one step above main's `0.19.0-preview.2` (raised again if another tool change merges first).
- `skip_specs: true`: no behaviour changes; the update offer, the settings a save applies and the copy key answer exactly as before.

Out of scope: removing null from the rest of the code (`UpdateCheckRecord.Newest` stays `string?` in the port and the state file, `ReleaseVersion.TryParse` keeps its nullable answer, and every other nullable is untouched), which is a separate survey and change; any other cross-thread state that is not one of these three fields; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** new `src\Source191.Vantage.Core\Application\SharedSnapshot.cs` and `src\Source191.Vantage.Core\Domain\NewestRelease.cs`; `Application\CurrentSettings.cs`, `Application\UpdateCheck.cs`, `Domain\UpdateCheckRule.cs`, `src\Source191.Vantage.Tui\ScreenClipboard.cs`.
- **Tests:** new `SharedSnapshotShould` and `NewestReleaseShould`; `UpdateCheckRuleShould`'s `Offer` facts pass a `NewestRelease` and its `Newest` facts expect one (losing a `!` and two `ShouldBeNull`); `CurrentSettingsShould`, `UpdateCheckShould` and `ScreenLoopShould` unchanged and still green.
- **Docs:** `CLAUDE.md` (one sentence); `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

Change 1 of the null programme, which merges one change after another in this order: 1 shared-snapshot (this one, 0.19.0-preview.3), 2 null-suppressions (preview.4), 3 null-free-outcomes (preview.5), 4 null-free-rows (preview.6), 5 null-free-observations (preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (preview.9), 8 null-free-port-answers (preview.10), 9 null-free-sessions (preview.11), 10 null-guard-rule (no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between.
