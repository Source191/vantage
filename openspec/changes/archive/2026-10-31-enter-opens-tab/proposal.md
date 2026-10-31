# Proposal

## Why

The Sessions tab has two keys for getting into a session: Enter hands Vantage's own tab to `claude attach` until Ctrl+Z, and `o` opens the session in a tab of its own (or switches to it). The owner works with each session in its own tab, so Enter should do that, and one key is enough. The wizard has the same split: F5 starts a session and stays in Vantage, F6 starts it and opens its tab. Again only the second is wanted, and it belongs on F5, the key a .NET developer presses to start something in Visual Studio and the key that already saves a setting's dialog (the owner's decisions, 31 October 2026).

## What Changes

- **BREAKING: Enter opens the session's tab.** On a background session Enter does what `o` did: it opens the session in its own Windows Terminal tab, or switches to that tab when it is open. Its key bar word is Open. `o` is removed from the Sessions tab, its key bar, help and tooltips.
- **BREAKING: no attach in place from the console.** Nothing in the console hands its own tab to `claude attach` any more, so the "open in another tab" refusal, the console's hand-off loop (leave the screen, attach, recover, re-label the tab, redraw with the selection kept) and the two diagnostic events it recorded go.
- **BREAKING: F5 starts and opens the tab.** In the new-session wizard F5 and the one Start button start the session and open its tab, as F6 did; F6 and starting without a tab go. Enter in a fixed folder's name field does the same as F5.
- **`vantage here` keeps the hand-off,** and the line it prints before attaching reads "Ctrl+Z detaches; the session keeps running.", because Ctrl+Z returns to the shell there, never to Vantage.
- **The hand-off check runs `vantage here`:** it starts a throwaway session through `here`, checks Claude's own screen appears, detaches with Ctrl+Z and checks `here` exits by itself. The CI `handoff` job and the nightly Claude contract workflow keep running it. It remains the one automated test of a real `claude attach` and detach, which the tab Enter opens also relies on.
- **Words:** the README, `CONTRIBUTING.md`'s manual checks, CLAUDE.md, the headless terminal's README and every footer or outcome that names `o` or F6 follow.
- **Version:** a minor step (pre-1.0, an existing key changes behaviour), set when the pull request is opened, above whatever `main` has then.

Out of scope: the Worktrees tab's own `o` (open shell) and Enter (go to session), which stay; a key for interactive (terminal) sessions, which still only say they run in their own terminal; changing `vantage here` beyond its hint line; folding away `ConsoleExit` and `IConsoleScreen`'s state now that only Quit leaves the screen; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: "Drill into a session in the same tab" is removed; "Open a session in its own Windows Terminal tab", "Live tabs are tracked by process", "Sessions with nothing to resume are refused" and "Vantage's own tab is recognisable" name Enter instead of the open-in-tab key and lose the return from an attach.
- `session-launch`: the fixed-folder, start-and-open, `vantage here` and wizard-keys requirements: F5 starts and opens the tab, F6 and start-without-tab go, and `here` prints its own hint.
- `console-layout`: the key bar reads Enter Open with no `o`, and "One console keeps its state across hand-offs" becomes one console that only quit leaves.
- `session-listing`: interactive sessions refuse Enter rather than attach.
- `diagnostics`: no hand-off events; the attach scenarios become an open-in-tab effect.
- `problem-reports`: the preview scenario names the open-in-tab effect instead of the attach hand-off.

## Impact

- **Application:** `SessionsView` (Enter raises `OpenTabForSession`, `o` and `AttachToSession` from the console gone), `Footer.OpenInAnotherTab` removed, `WizardView` and `WizardInput.Start` lose the open-in-tab flag, `StartSession` and `StartSessionHandler` always open the tab (`StartSessionOutcome.Started` goes), `ScreenLoop` runs the screen once without `TerminalEffects`, the `HandingOver` and `ChildReturned` diagnostic events go, `Wording` (key help, key bar, wizard hints, footers, outcomes, the return hint).
- **Tui:** `LaunchWizardDialog` (one Start button on F5), `WizardResult`, `DialogEffects`.
- **Program.cs:** `ScreenLoop` no longer takes `TerminalEffects`; `vantage here` keeps it.
- **Tools:** `tools\HeadlessTerminal` (`HandoffCheck`, `HandoffCapture`, README), `.github\workflows\check.yml` and `claude-contract.yml` only where their wording describes the check.
- **Docs:** README, `CONTRIBUTING.md`, CLAUDE.md.
