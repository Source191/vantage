# Design

## Context

See proposal.md for the motivation. Today the wizard returns a plan plus an "attach after" flag; both screens (sessions and worktrees) run the launch off the UI thread and, when the flag is set, stop the application with an attach effect. The open-in-tab path lives in `Actions\TabOpener`, which needs the session's full id for the tab registration, but `LaunchOutcome` carries only the eight-character job id. `LaunchService` already lists sessions after a start to record the description, but only when a description was given. The worktrees screen has no tab opener; `LaunchWorkflow` is the one door both wizards already share.

## Goals / Non-Goals

**Goals:**
- One tested place that turns "started" into "started and in a tab", shared by both wizards.
- The Terminal.Gui layer keeps only rendering, text and effect running.

**Non-Goals:**
- No change to how tabs are registered, coloured or tracked.
- No new screen effect or reducer key: the wizard is a dialog, not a reducer input.

## Decisions

- **The outcome carries the listed session.** `LaunchService` lists sessions after every successful start (one `claude agents --json` call it already makes when a description is given) and puts the matching `Session` on `LaunchOutcome`. Alternative: have the screen reload rows and search for the job id; rejected because the screen is untested and the row load is on a timer.
- **The tab opener accepts a session, not only a row.** `TabOpener.OpenAsync(Session)` builds the pending registration and the tab from the session; the existing row overload keeps the "already open" refusal and delegates. Alternative: build a synthetic `SessionRow` in the screen; rejected as untested glue.
- **The workflow owns the follow-up.** `LaunchWorkflow` gains the tab opener and `OpenTabAsync(LaunchOutcome)`, so the worktrees screen needs no new constructor argument and `Program.cs` only reorders construction. The screens compose the footer: "Started name (id). Opened name in a blue tab."
- **Not-listed is not a failure.** When the listing has no row for the job yet, the outcome is still Started with a null session and the footer says to press `o` when it appears, mirroring the existing silent skip in the description path.
- **The old F6 goes.** The request is to change F6, not add a key; Enter on the row still attaches in the same tab.

## Risks / Trade-offs

- [A second `claude agents --json` call after every start] → it is the call the description path already made; the listing is cheap and the start footer was already waiting on the CLI.
- [Opening a tab from a worktree-view start when the sessions view is not showing] → the tab opens regardless; the worktrees footer names the colour the same way.
