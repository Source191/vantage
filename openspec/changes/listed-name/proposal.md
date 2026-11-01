# Proposal

## Why

The code review of 0.20.0-preview.7 (1 November 2026; D-07, A-10, section 4 item 3) found that `Session.Name` is a bare `string` whose blank means "the listing gave no name" (`src\Source191.Vantage.Core\Domain\Session.cs:8`, `src\Source191.Vantage.Infrastructure\Claude\ClaudeAgentsListing.cs:48`), against CLAUDE.md's "a session's name is a `SessionName`" and its rule that a named case stands where a null or sentinel would. That one sentinel forces two `ResumeBackgroundAsync` overloads on `IClaudeCli` (`src\Source191.Vantage.Core\Ports\IClaudeCli.cs:21-25`) and the same three-line `SessionName.TryParse` decision, comment included, in `AdoptSessionHandler.cs:34-37`, `RenameSessionHandler.cs:40-43` and `ResumeSessionsHandler.cs:30-33`; a fourth caller would copy it again. The name is also read in some fifty places as a string, so nothing says which of them would show a blank.

## What Changes

- **`ListedName` in Domain:** a closed two-case record, `Named(SessionName Name)` and `Unnamed`, that `Session`, `BackgroundSession` and `InteractiveSession` carry as `Name` instead of the string. The case is decided once, in `ClaudeAgentsListing.ToSession`: a listing row whose `name` is missing or blank is `Unnamed`, anything else `Named` through the existing `SessionName.TryParse`.
- **One resume on the port:** `IClaudeCli.ResumeBackgroundAsync(string sessionId, string directory, ListedName name, CancellationToken)` replaces the two overloads; `ClaudeCli` adds `-n <name>` only for `Named`. `StartBackgroundAsync` keeps its `SessionName`, because a start always has one.
- **The three handlers lose the branch:** adopt, rename and resume pass `session.Name` straight through; the comment "A listed name SessionName refuses is resumed unnamed" goes with it. Rename's own call passes `new ListedName.Named(newName)`.
- **The typed name travels to `Wording`:** the outcome records (`AdoptSessionOutcome`, `DeleteSessionOutcome`, `OpenTabOutcome`, `RenameSessionOutcome`, `StopSessionOutcome`, `ResumeNote`, `SkippedSession`, the three lists in `CloseEmptyTerminalsOutcome`), the nine `StatusMessage` cases that name a session, `WorktreeSession.Running`, `RemovalReason.LiveSession`, `OpenWorktreeSession` and `MemoryBreakdown` carry a `ListedName`; `RenameSessionOutcome.Renamed.NewName` becomes a `SessionName`. `Wording` gains one `Name(ListedName)` that every sentence, cell, title, dialog field and tab title reads: the `SessionName`'s value for `Named`, and for `Unnamed` the empty string every one of those places shows today. design.md says why not a word, and why not the folder name.
- **The tab title and its lookup agree:** `TabScripts.Attach` titles the tab and `OpenTabHandler` finds it by title; both read `Wording.Name`, with a fact that pins them together.
- **Staged so every step is green:** `Session` first gains a computed `ListedName Listed` beside the string, readers move to it one area at a time, then the string goes and `Listed` becomes `Name`.
- **CLAUDE.md:** the "ports keep strings and callers pass `.Value`" sentence in "Values, sessions and null" is restated for `IClaudeCli`'s name parameters only: a port takes the Domain value where one exists (`SessionName`, `ListedName`, as `OpeningPrompt` already does) and the adapter reads `.Value`; the string `sessionId`, `jobId` and `directory` parameters are left for the `folder-deleter-and-ports` change, which owns D-10's id and folder half. "A session's name is a `SessionName`" becomes "a session's listed name is a `ListedName`, `Named(SessionName)` or `Unnamed`". The Claude Code facts in CLAUDE.md and `.claude\rules\claude-code.md` record what the captures in `ClaudeAgentsListingShould` already show: the CLI itself names a session started without `-n` after its folder (`repos-93`, 2.1.309; `Northwind-9d` for `claude -p`, 2.1.314), so a session row with no name has not been seen; only the SDK's automated runs, which are never rows, lack one.
- **Version:** one preview step above `main` when implemented; a refactor, so patch-level under semantic versioning.
- `skip_specs: true`: no key, screen, message, tab title, state file or command line behaves differently; an unnamed session shows the same blank it shows today.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with tests and documentation, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `src\Source191.Vantage.Core\Domain\ListedName.cs` (new), `Session.cs`, `BackgroundSession.cs`, `InteractiveSession.cs`, `WorktreeSession.cs`, `RemovalReason.cs`; `Ports\IClaudeCli.cs`; `Application\Wording.cs` and its partials, `Actions\*Outcome.cs`, `Actions\ResumeNote.cs`, `Actions\SkippedSession.cs`, the three resume handlers, `OpenTabHandler`, `DeleteSessionHandler`, `StopSessionHandler`, `CloseEmptyTerminalsHandler`, `TabScripts`, `MemoryBreakdown`, `WorktreeInventory`, `Views\StatusMessage.cs`, `SessionsView`, `SessionsViewState`, `ConsoleView`, `WorktreesView`, `OpenWorktreeSession`, `FilterText`; `Infrastructure\Claude\ClaudeAgentsListing.cs`, `ClaudeCli.cs`; `Tui\DialogEffects.cs`, `SessionsScreenDefinition.cs`, `WorktreesScreenDefinition.cs`. About 36 record members and 50 interpolations change type, none their words.
- **Tests:** `ClaudeAgentsListingShould`, `ClaudeCliShould`, `ClaudeContractShould` (gated, but compiled), the three handler tests and `SessionTestData`, `WordingShould`, `WordingOfOutcomesShould`, `StatusTonesShould` (their `Placeholder` switches learn `ListedName`), `SessionsViewShould`, `OpenTabHandlerShould`, `WorktreeInventoryShould`, `WordingOfDetailShould`. No `FakeClaudeCli` exists: the handler tests run the real `ClaudeCli` over `FakeProcessRunner`, and keep doing so.
- **Docs:** `CLAUDE.md`, `.claude\rules\claude-code.md`, `src\Source191.Vantage\Source191.Vantage.csproj` for the version. The README's NAME column help ("or the folder name for an unnamed one") describes the CLI's own naming and stays.
- **Other open changes:** `fix-cli-contracts` edits `IClaudeCli.ResumeBackgroundAsync`'s return type (`ResumeResult` as a hierarchy, `ClaudeCliException`), `ClaudeCli.ResumeAsync` and the same lines of the three handlers; this change edits the method's parameters. `fix-cli-contracts` should land first and this change rebases onto it; whichever lands second rebases in any case.
- **Users:** none.

## Out of scope

- Giving an unnamed session words or its folder name on screen: a behaviour change for its own change, with a spec delta.
- `IClaudeCli`'s string `sessionId`, `jobId` and `directory` parameters, `ITranscripts.HasMessages` and the other ports D-10 lists: `folder-deleter-and-ports`.
- `StartSessionOutcome.Name` and `Wording.Here`'s name, which come from a `LaunchPlan`'s `SessionName`, never from the listing.
- The `ResumeResult` hierarchy and the adapter's failure convention: `fix-cli-contracts`.
- Multiple Claude accounts, which is a later change.
