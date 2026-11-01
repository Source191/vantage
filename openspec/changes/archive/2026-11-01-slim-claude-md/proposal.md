# Proposal

## Why

`CLAUDE.md` has grown to 55,685 characters in 87 lines, roughly 13,000 tokens loaded into every Claude Code session and every subagent, whatever the task. Claude Code's guidance (https://code.claude.com/docs/en/memory, read 1 November 2026) is under 200 lines per file, headings rather than dense paragraphs, and path-scoped rules in `.claude\rules` for what matters only in part of the code; longer files are followed less reliably. One bullet of the code rules alone runs to about 15,000 characters, so its security rules sit in the middle of a description of the console's layout. About half of the file (the Terminal.Gui, Windows Terminal and Windows process facts, the console screen's layout and the details of the headless and contract checks) is only needed when working on those parts.

## What Changes

- **`CLAUDE.md`** keeps what every session needs: the test loop, projects, the checks and when to run them, the release rules, the working rules, the code rules split under headings (layers, idioms, reducers and effects, one home per fact, values and null, wording, security, diagnostics), the Claude Code CLI facts and the open items. Every "never", "always" and "must" rule stays word for word or tighter; nothing is dropped.
- **`.claude\rules\`** gains four path-scoped rule files, each loaded when Claude reads or edits a matching file:
  - `console-screen.md`: the console's layout, tabs, status line, detail pane, Config tab and worktree scanning, for `Tui` and `Application\Views` and `Application\Detail`.
  - `terminal-gui.md`: the Terminal.Gui 2.5.0 facts, for `Tui`.
  - `windows.md`: the Windows Terminal and Windows process facts, for the terminal and process adapters and the domain types that follow them.
  - `checks.md`: the hand-off, smoke and Claude Code contract checks in detail, and the CI and release workflows, for `tools\HeadlessTerminal`, the contract tests and `.github`.
- **`CLAUDE.md`** names the four files at the top, so a session reasoning about one of those areas from elsewhere knows where to read.
- **`CONTRIBUTING.md`** and **`openspec\config.yaml`** say where the verified facts now live.
- The PowerShell quoting rule moves from the Terminal.Gui facts to the security rules, where it belongs; it stays in the always-loaded file.
- `skip_specs: true`: documentation only, no behaviour change, nothing under `src\`, so no `Version` raise.

Out of scope: renaming `CLAUDE.md` to `AGENTS.md` (only Claude Code works on this repository today; Claude Code reads `AGENTS.md` natively from v2.1.277, so a later change can move the body there and leave `@AGENTS.md` in `CLAUDE.md`), nested `CLAUDE.md` files inside `src\` (any file under `src\` requires a `Version` raise), changing any verified fact or its date, the section on other cultures that the open `locale-safety` change adds, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: documentation only, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Docs:** `CLAUDE.md`, new `.claude\rules\console-screen.md`, `terminal-gui.md`, `windows.md` and `checks.md`, one line each in `CONTRIBUTING.md` and `openspec\config.yaml`.
- **CI:** none. Every changed file is a `.md` outside `.github\` or under `openspec\`, so `Test-DocumentationOnly.ps1` skips the build, and nothing ships, so `Assert-VersionBumped.ps1` needs no raise. The workflow comments that cite `CLAUDE.md` sections ("Smoke check of the packed tool", "Hand-off check", the trust records) still find them, because those bullets keep their names in `CLAUDE.md`.
