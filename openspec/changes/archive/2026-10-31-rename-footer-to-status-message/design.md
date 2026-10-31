# Design

## Context

`Footer` is the open record hierarchy in `Application\Views\Footer.cs` that reducers return and `Wording.Describe(Footer)` turns into words. Each view state (`SessionsViewState`, `WorktreesViewState`, `ConfigViewState`) holds one as `Footer`, `IListViewState<TRow>.Shown` answers the one shown, `IScreenDefinition.WithFooter` sets it, `IEffectContext.ShowFooter` lets a dialog flow set it, and `ITabBody.Status` hands it to the console. See proposal.md for why the name goes.

## Goals / Non-Goals

**Goals:**
- One name, `StatusMessage`, everywhere the code or the specs mean the status line's message, with no behaviour change.

**Non-Goals:**
- Renaming `IListViewState.Shown`, `ITabBody.Status`, `StatusTone` or `StatusLayout`, which already say what they are.
- Touching archived changes.

## Decisions

### The rename, by meaning rather than by text
| Before | After |
|---|---|
| `Footer` (type, file `Footer.cs`) | `StatusMessage` (`StatusMessage.cs`) |
| `Footer.None`, `Footer.Blank`, every case | `StatusMessage.None`, `StatusMessage.Blank`, the same case names |
| a view state's `Footer` property | `Message` |
| `WithFooter`, `ShowFooter` | `WithMessage`, `ShowMessage` |
| test names such as `HaveWordsForEveryFooterCase`, `FooterCases`, `FooterTones` | `HaveWordsForEveryStatusMessageCase`, `StatusMessageCases`, `StatusMessageTones` |
| a parameter or local named `footer` | `message` |

The type is renamed with the IDE's rename (or a whole-word replace of the identifier, which the compiler then checks), not a blind text replace: `footer` also appears in prose that should read "status line", and a local `message` can clash with an existing `message` in the same scope, which the build reports. `Wording.Describe(Footer)` and `Wording.ToneOf(Footer)` keep their names and take a `StatusMessage`.
- *Alternative:* `Status`. Rejected: "status" already means a Claude session's busy, idle or waiting in the listing code.
- *Alternative:* `Notice`. Rejected: it clashes with `StatusTone.Notice`, one kind of message.

### Specs reword through deltas
Each requirement that says "the footer" is copied whole into a MODIFIED delta with "the footer" read as "the status line", "filter and footer" as "filter and status message", and "beside the footer's working text" as "beside the status line's working text". The requirement "The screen is a header, a table and a footer" is RENAMED to "… and a detail pane", which is what the screen's last part is. The deltas were generated from the specs on `main` after `status-line-under-settings` was archived, so they carry no stale text.

## Risks / Trade-offs

- [A rename this wide could hide a real change in the diff] → It is its own pull request, the code diff is renames only, and the whole suite, the reflection facts and the headless smoke step in CI must pass unchanged.
- [Another open branch adds a `Footer` case] → That branch fails to compile after this merges, which points straight at the case to rename.
