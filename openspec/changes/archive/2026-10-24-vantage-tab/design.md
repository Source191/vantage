# Design

## Context

See proposal.md. Windows Terminal takes the tab title from OSC 2 and the tab background from palette entry 264 through OSC 4 (`ESC ] 4 ; 264 ; rgb:rr/gg/bb ESC \`), and resets the tab's colour indices with DECAC (`ESC [ 2 ; 263 ; 264 , |`), per the Windows Terminal maintainers in discussions 14903 and 16490. `ConsoleRecovery` already enables virtual terminal processing on standard output before writing sequences. Tab registrations are keyed by session id and live while their process exists with the same start time.

## Goals / Non-Goals

**Goals:** one escape write at start, one at quit; the colour choice reuses the existing registration and selection code so session tabs and the Vantage tab share one pool.

**Non-Goals:** restoring whatever title the shell had before; coping with a profile that fixes the tab colour.

## Decisions

- **OSC 4 on entry 264, not DECAC with a palette index:** the palette's hex values do not map onto the user's colour scheme indices, and OSC 4 takes the exact colour Vantage's palette names.
- **The Vantage tab is an ordinary registration** with session id "vantage", claimed by Vantage's own process id and start time from `IProcessRegistry`, so liveness, staleness and the colour pool need no new rules. No session has that id, so no row shows it as open.
- **`TabColourSelection.Choose(inUse, preferred)`** returns the preferred colour when free, otherwise the existing first-free rule, so a reused warning never applies to the Vantage tab.
- **Written from `Program.cs`** (wiring) before the first screen runs and after the loop ends, through `TabEscapes` constants that are tested as text.

## Risks / Trade-offs

- [The tab was launched with an explicit tabColor] → the sequence is ignored by the terminal and the title still applies; documented in the README.
- [Vantage is killed rather than quit] → the registration goes stale with the process; the tab keeps its colour until the shell exits, which is harmless.
