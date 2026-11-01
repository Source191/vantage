# Design

## Context

Keys are data in `Wording` (`SessionsKeys`, `WorktreesKeys`, `ConfigKeys`, the bar rows in `WordingOfConsole`) and cases in each view's reducer. Worktrees' Enter raises `JumpToSession(SessionId)`, an `ISwitchesTab` effect that `ConsoleView.Apply` turns into "show Sessions with this session selected". Sessions' Enter decides in `SessionsView.SessionAction` between `OpenTabForSession(BackgroundSession, LiveTab)`, `RunsInOwnTerminal` and `NeverSentAMessage`. A worktree row knows only `WorktreeSession.Running(SessionId, Name)`; the session's kind, process, transcript flag and live tab are on the Sessions tab's rows. The Config tab's `ConfigViewState.Shown` substitutes `StatusMessage.SettingHint(row)` whenever its message is blank; `StatusLayout.Closes` and `Wording.ToneOf` carry exceptions for it.

## Goals / Non-Goals

**Goals:**
- One decision for "what Enter does to this session", used by both tabs.
- Worktrees' Enter runs its effect and shows its outcome from the Worktrees tab, which stays shown.

**Non-Goals:**
- Changing what Sessions' Enter does, or the `OpenTabForSession` handler.
- Hidden aliases for e and v.

## Decisions

**Worktrees' Enter consults the Sessions tab's rows through the console.** `JumpToSession` is replaced by `OpenWorktreeSession(SessionId, string Name)`, carrying a new marker `IConsultsSessions`. `TableBody` routes that marker to `ITabHost`, and `ConsoleScreen` asks the pure `ConsoleView.OpenSession(ConsoleState, OpenWorktreeSession)`, which answers a `RowDecision` (act with `OpenTabForSession`, or explain with a `StatusMessage`). The worktrees body then dispatches the effect (so the handler's outcome lands on the Worktrees status line) or shows the message. `EffectRoutingShould` learns the marker, as it knows `ISwitchesTab`.
- Alternative: copy the session's kind and live tab into `WorktreeSession.Running` at scan time. Rejected: the scan runs every 15 s and only while shown, so the tab fact would be stale, and the Sessions rows are already the one home of it.
- Alternative: switch to Sessions, select, then reduce Enter there. Rejected: the user asked to stay on Worktrees, and the message would land on the hidden tab.

**The Enter decision moves into one pure function.** `SessionsView.Opening(SessionRow)` returns the `RowDecision` that today's `SessionAction(..., Attachment.InOwnTab)` computes; Sessions' Enter and `ConsoleView.OpenSession` both call it. A session id the Sessions rows do not hold is `StatusMessage.SessionNotListed(name)` (orange, refused), worded in `Wording` with its tone in `ToneOf`.

**SettingHint is deleted, not hidden.** `ConfigViewState.Shown` becomes `Message`, as in the other views; the `SettingHint` case, its `ToneOf` arm and its exception in `StatusLayout.Closes` go. `IScreenDefinition.RowTip` and `Wording.ConfigDescription` stay as the tooltip's source. `SetupAdvice` keeps its white, no-glyph tone and its ✕.

**Bar words.** f Open folder, s Open solution (Sessions, Worktrees), n New session and Enter Open session (Worktrees), matching the console-layout rule that the first row ends in New session. Worktrees' first bar row becomes d, D, P, n, o, Enter; both second rows c, f, s, p, /, ?, !, q; Config's c, f, /, ?, !, q. "Open solution" adds five cells to the second row, which the 120-column fact (`WordingOfKeyBarShould`) checks.

## Risks / Trade-offs

- [Muscle memory for e, v and s] → Release notes and a pre-1.0 minor version (0.20.0-preview.1); the help lists the new letters.
- [Sessions rows older than the worktree scan, so a new session is not yet listed] → `SessionNotListed` says so and the next listing (about 5 s) fixes it.
- [The smoke check finds keys by bar word] → `SmokeScript` steps move to "New session" and "Open solution" in the same task as the wording.
