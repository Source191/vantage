---
paths:
  - "**/*Claude*.cs"
  - "**/*Session*Handler*.cs"
  - "**/AutomatedRun*.cs"
  - "**/ITranscripts.cs"
  - "**/TabScripts*.cs"
  - "src/Source191.Vantage.Infrastructure/Claude/**"
  - "src/Source191.Vantage.Core/Application/Launch/**"
  - "test/Source191.Vantage.Infrastructure.Tests/Contract/**"
  - ".github/workflows/claude-contract.yml"
  - "openspec/changes/**"
  - "openspec/specs/session-*/**"
---

# Claude Code CLI facts (verified 2026-10-29, CLI 2.1.310)

Re-verify these, and move the date and version above, whenever a change accepts new help snapshots or raises `ClaudeCodeSupport.Minimum` (the settings row's below-minimum warning, stated as "2.1.309 or later" in the README and `openspec\config.yaml`, which `DocumentedMinimumShould` keeps in step).

- `claude --version` prints `2.1.310 (Claude Code)`; `IClaudeCli.VersionAsync` takes the leading dotted number (`NotRead` on failure) once per run for the settings row.
- `claude agents --json [--all]` is the scripting surface. Fields: pid, id, cwd, kind (interactive or background), startedAt, sessionId, name, status (busy, idle, waiting), state (working, blocked, done, failed, stopped), waitingFor. The job `id` is the first 8 characters of `sessionId`. A stopped session is a row without pid or status.
- An interactive listing row with a pid and no `status` is an automated run (`Domain\AutomatedRun`, sorted out in `ClaudeAgentsListing`), never a session row: the Agent SDK (`sdk-py`, `sdk-ts`; the security-guidance plugin's background review, run after a commit or push and when a turn ends, is one, about 20 s and a few thousand tokens) and `claude -p` (with a derived name such as `tmp-9d`) are listed that way for their whole lives, while a `claude` started in a terminal registers without a status and has `idle` about 130 ms later (measured 2026-10-30, CLI 2.1.314, through the headless terminal). `claude logs <id>` never registers. Their count and memory end the Sessions counts (`◇ 1 automated 216 MB`). How long a `claude -p` stays listed, and how the contract fact holds one open, are in `checks.md`.
- `claude --bg -n <name> [prompt]` starts a supervised session; with no prompt it waits at its own prompt. `attach` accepts a name, `stop` and `rm` need the id. Attaching to a session with no messages lands at that session's own prompt, and Ctrl+Z detaches from it as from any conversation. The CLI has no end-of-options marker: `claude attach -- --help` answers `unknown option '--'` (checked 2026-10-29), so a prompt that starts with `-` would be read as options and Vantage refuses it instead. The npm package now installs the same native binary as the installer; Vantage never runs a `claude.cmd`.
- A session's transcript `~\.claude\projects\<cwd-encoded>\<sessionId>.jsonl` is written on the first message. Resuming a session with no transcript fails with "source session not found", so adopt (`b`), rename (`r`), open-in-tab (Enter) and resume all check `ITranscripts` first.
- `claude --bg --resume <sessionId> -n <name>` continues a *non-background* transcript under the supervisor with the same id (how `b` adopts a terminal session after ending its process). For a *listed background* job the same command with flags starts a copy, as [the docs](https://code.claude.com/docs/en/agent-view) say ("keeps its own saved options"); `claude rm <id>` first (the transcript stays on disc) and the resume then continues the same id under the new name, which is how `r` renames. A listed stopped or failed job is copied the same way (verified 2026-11-01, CLI 2.1.316: the old row stayed listed as failed beside the new jobs), so `R` removes each job first too, like `r`; a job just started or resumed is listed with a pid and no status for a moment, which Vantage shows as starting. Never run `claude --bg --resume --help`: it starts a session.
- Interactive sessions register in `~\.claude\sessions\<pid>.json`; they cannot be attached, stopped or removed from outside. Windows Terminal closes a tab when its root shell exits with code 0 and keeps it after a kill, so `d` and `C` end claude and then end the tab's shell cleanly (`TerminalTab`, `IProcessRegistry.EndCleanly`).
- Workspace trust is per repository (linked worktrees inherit it), granted only interactively; `claude --bg` in an untrusted folder exits with "Workspace not trusted". Never script edits to the trust records on a developer machine; the exceptions are the CI `handoff` job and the `claude-contract.yml` workflow, which seed `projects.<path>.hasTrustDialogAccepted` in the throwaway runner's own `~\.claude.json` (keys are forward-slash paths with a capital drive letter, e.g. `D:/a/vantage/vantage`).
- Background sessions do not commit or push on their own (bgIsolation none, sibling worktrees). permission_prompt and idle_prompt notifications fire unattended; agent_needs_input and agent_completed fire only while the native agent view is open, which is why Vantage rings its own bell for background rows.
- `"preferredNotifChannel": "terminal_bell"` in settings.json ([terminal configuration](https://code.claude.com/docs/en/terminal-config)) makes Claude ring the terminal bell when it finishes a task or pauses for a permission prompt while the user appears away, which flags the tab showing a session. Claude Code writes `settings.json` with bare LF; anything that rewrites it must keep that.
- On detach Claude leaves focus reporting on and the cursor hidden and sends a late terminal-version query; `ConsoleRecovery` resets these after every child (only `vantage here` runs one now).
