# Design

## Context

See proposal.md for motivation. This document records the technical decisions that shape the build.

Constraints that matter here: Windows only; sessions live under Claude Code's supervisor; the only stable interfaces are the documented CLI commands; Windows Terminal cannot list or focus tabs by name; worktrees must be siblings of the repository because Visual Studio cannot open solutions under .claude\worktrees; the coding and testing standards in CLAUDE.md apply in full.

## Goals / Non-Goals

**Goals:**
- One process, one tab, no daemon: everything is read live from `claude agents --json` and `git worktree list --porcelain`.
- A pure, fully tested domain with every external effect behind a small boundary interface.
- A screen model that can be driven by tests without a terminal.

**Non-Goals:**
- Reproducing the native agent view's peek panel, pinning, grouping modes or dispatch prompt. The `a` key opens the native view for those.
- Hosting the attached session inside Vantage. Attach is a child process that owns the terminal until it exits.
- Managing Windows Terminal tabs that Vantage did not open, beyond closing the tab of a terminal session that was never sent a message.

## Decisions

- **Supervisor-backed sessions over interactive tabs.** Chosen because Windows Terminal can only focus a tab by index, so a dashboard that "manages tabs" cannot find them again; the supervisor keeps sessions alive without a terminal and the console attaches on demand. Alternative considered: interactive `claude -n --session-id` in tabs with a manifest for resume. Rejected after the spike: the native agent view's screen was the complaint, not the session model beneath it.
- **Shell out for attach and for the native view.** `claude attach <id>` and `claude agents` take over the terminal; Vantage suspends its screen, waits for the child to exit, resets the console state the child leaves behind, then redraws. Alternative: a ConPTY host that proxies the session and intercepts keys. Rejected as a much larger build whose benefit is only rebinding the return key.
- **Vantage creates sibling worktrees itself with git.** `git worktree add ..\<repo>-<slug> -b <slug> <base>` plus a copy of `.worktreeinclude` matches. Alternative: a WorktreeCreate hook so that `claude --worktree` places siblings. Rejected because background sessions are started with `--bg` in a directory, not with `--worktree`, and the hook would run for every Claude-initiated worktree on the machine.
- **No manifest of sessions.** `claude agents --json --all` already returns name, ID, cwd, kind and state for current and completed background sessions, so Vantage stores nothing about sessions except the descriptions given at launch. It stores only what nothing else records: the attach-tab registry, recent repositories, descriptions, and the worktrees and branches it created.
- **.NET 10 global tool.** Matches the maintainer's stack and test tooling. PowerShell appears only in the encoded script that a new Windows Terminal tab runs.
- **TUI library: Terminal.Gui v2, with Spectre.Console as the fallback.** Terminal.Gui supplies list, table and dialog views with keyboard focus handling. Verified 2026-10-23 (task 1.1): Terminal.Gui 2.5.0 is the current stable release on NuGet and its docs describe the v2 instance-based model (`Application.Create()`, `IApplication.Run(window)`, TableView with arrow, Home, End and PageUp/PageDown bindings). Spectre.Console 0.57.2 is the fallback. Decision: Terminal.Gui 2.5.0.
- **Test packages (verified 2026-10-23).** xunit.v3 4.0.2 on the Microsoft.Testing.Platform runner, Shouldly 4.3.0, NSubstitute 6.2.0 with NSubstitute.Analyzers.CSharp 1.0.17, Microsoft.Extensions.TimeProvider.Testing 10.11.0, SonarAnalyzer.CSharp 10.35.0.4138 in Directory.Build.props; warnings are errors.
- **Look and feel.** A three-part screen: a header (context block, key hints with blue keys and white descriptions, a right-aligned VANTAGE wordmark), a bordered table titled with the view name, filter and count, a highlighted selection bar, state words coloured by state and finished rows dimmed, then a footer crumb and status line. Palette on a dark background: amber for needs input and for the crumb, cyan for working and titles, green for completed, red for failed, grey for stopped and unknown. Rendering is the only place colour appears; the reducer exposes a DisplayStateKind and the renderer maps it to a colour. Dialogs use the same palette through a shared Confirm helper rather than Terminal.Gui's defaults.
- **Screen model separated from rendering.** Each view is a state record plus a reducer that maps a key to the next state and a list of effects (run CLI, open tab, show dialog). Tests drive the reducer directly; the TUI layer only renders state and dispatches keys. This keeps the "no untested code" rule honest.
- **Boundaries as thin interfaces with real-process adapters.** `IClaudeCli`, `IGit`, `IWindowsTerminal`, `IProcessRegistry`, `ITranscripts`, `IFileSystem` and `TimeProvider`. Adapters wrap `System.Diagnostics.Process` and have a small number of integration tests that run only when the tool is on PATH.
- **Session state mapping.** Observed in the spike: `status` is busy, idle or waiting; `state` is working, blocked, done, failed or stopped; `waitingFor` names a permission prompt; a stopped session keeps its last `state` but loses `pid` and `status`. A single pure function maps process presence, status, waitingFor and state to the six display states and returns the raw text when it cannot.
- **Name resolution.** Every action takes the session ID from the listing row. Names are never passed to stop or rm.
- **Transcripts decide what can be resumed.** Claude writes a session's transcript only on its first message, so adopt, rename, attach and resume check for `~\.claude\projects\*\<sessionId>.jsonl` first; a session with no process and no transcript can only be deleted.
- **Rename and adopt by remove-and-resume.** The CLI has no rename verb and resuming a listed background job with flags starts a copy, so rename removes the job and resumes the transcript by session ID under the new name; adopting a terminal session ends its process and resumes its transcript under the supervisor. Both keep the session ID.
- **Closing a terminal tab from outside.** Windows Terminal closes a tab when its root shell exits with code 0 and keeps it after a kill, so closing the tab of a terminal session never sent a message ends claude and then ends the shell found by walking up the process tree to the WindowsTerminal child, with exit code 0.
- **Attention is the terminal bell.** Vantage writes the bell character when a background row starts waiting; Claude Code's `preferredNotifChannel` set to `terminal_bell` covers attached tabs. Windows toasts and a Notification hook were built first and removed: the bell needs no hook, no extra process and no window.
- **State folder.** %LOCALAPPDATA%\Vantage with JSON files or folders: tabs (one file per tab, liveness by process ID and start time), recent.json, created.json, sessions.json mapping session ID to description, and config.json for the repository roots and default worktree base. A missing or corrupt file is treated as empty, never as an error.
- **Children do not inherit a Claude Code session's environment.** NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_* are dropped from every process Vantage starts, because Windows Terminal hands the caller's environment to new tabs and everything in them would run monochrome.
- **The native agent view is an escape hatch, not the daily screen.** The `a` key stays for pinning and anything else Vantage lacks, but no workflow depends on it.
- **Tab colours.** Each tab Vantage opens gets a palette colour no live Vantage tab is using (session-attach spec). The palette is an ordered eleven: red, blue, yellow, purple, green, orange, teal, pink, brown, lime, grey.
- **Refresh.** A timer re-runs the listing every five seconds (fifteen for the worktree scan) while the console has the terminal; no refresh while a child process owns it.
- **A headless harness guards the hand-off.** `tools\HeadlessTerminal` drives the built exe inside a ConPTY pseudo-console with scripted keys and answers terminal queries as Windows Terminal would; its `check` command reproduces attach, Ctrl+Z, wizard, cancel, quit and fails when the UI thread is blocked, which is how the first hand-off deadlock was found.

## Risks / Trade-offs

- [Agent view is a research preview and its JSON shape may change] → depend only on fields seen today (pid, id, cwd, kind, startedAt, sessionId, name, status, state); tolerate missing fields; pin the minimum CLI version in the README.
- [Attaching twice to one session may be refused] → both Enter and the open-in-tab key refuse when a live Vantage tab exists, so Vantage never asks for a second attach.
- [agent_needs_input and agent_completed fire only while the native agent view is open] → Vantage rings its own bell from its refresh when a background row starts waiting.
- [A deliberate stop and a reboot both show as stopped] → resume-all restarts failed sessions automatically and offers stopped ones as an opt-in checklist.
- [Idle sessions stop after about an hour] → attaching restarts them; the listing shows stopped rather than hiding them; resume-all covers reboots.
- [A background session may be told to commit and push when it finishes] → settled in the spike on 2026-10-23: with bgIsolation none, a session in a hand-made sibling worktree left its edit untracked and did not commit or push. No appended system prompt is needed.
- [Windows Terminal argument quoting] → build arguments as an array and pass the tab's command through an encoded command; user text never reaches PowerShell source.
- [Terminal.Gui blocks on an awaited call] → nothing in the TUI layer waits on the UI thread; the harness check catches a regression.

## Migration Plan

1. Settings: `worktree.bgIsolation` none and `preferredNotifChannel` terminal_bell in the user's settings.json.
2. Install the tool with `dotnet tool install -g` from a local package source, add the `vantage` alias to the profile.
3. Rollback: uninstall the tool and delete the two settings; Vantage leaves nothing else behind except its state folder.

## Open Questions

- Whether `claude attach` can run in two terminals for one session at once. Does not change the specs: the open-in-tab key already refuses when a live tab exists.
- Whether `claude --bg --resume <id>` or `claude respawn --all` is the right resume after a reboot. The listing side is documented; resume-all uses the former.
- Whether a take-over key (stop, then a foreground `claude --resume` in a tab, leaving supervision) is wanted.
