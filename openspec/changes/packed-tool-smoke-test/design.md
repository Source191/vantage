# Design

## Context

See proposal.md for why. The pieces this builds on:

- `tools\HeadlessTerminal` already runs a program in ConPTY, answers Windows Terminal's queries, captures everything to `drive-raw.txt` and has a repeatable `check` command. Its scripts are fixed delays (`KeystrokeScript`, "delayMs:keys,..."), and keys sent before Terminal.Gui initialises are lost, which is why scripts start at 8000 ms. It has no reference to the Vantage projects.
- Every dialog calls `Tui\DialogTabTitle.Apply`, which writes `Wording.TabTitleDuring(DialogName)` (for example `Vantage (Keys)`) as the terminal title when the dialog becomes modal, and the main window writes `Wording.TabTitle` (`Vantage`) back when it closes. Checked on 2026-10-28 with `drive` against the installed tool: ConPTY passes these through as `ESC ] 0 ; Vantage (Keys) BEL` and `ESC ] 0 ; Vantage BEL`, so the capture holds a layout-free marker for every dialog opening and closing.
- Keys are data: `Wording.SessionsKeys` and `Wording.WorktreesKeys` are `KeyHelp(Key, Hint, Line, Column)` lists, and the columns are `Wording.SessionsColumns` and `Wording.WorktreesColumns`.
- With no `config.json`, the repository roots resolve to `<home>\source\repos` and the worktree root to `<home>\source\worktrees` (`Settings.Resolve`). On a runner that config never exists, so seeding a repository there needs no settings file.
- `build-test-pack` outputs the packed `version` and the `package` path; `tools\install-local.ps1` installs with `--add-source <folder> --version <version>`.
- In Terminal.Gui 2.5.0, only `Terminal.Gui.Drawing.TextMateSyntaxHighlighter` uses `TextMateSharp.Grammars`, and it is constructed only by `Views.Code`, `Views.CodeRoleLegendHighlighter`, `Views.WizardStep` and the `IDesignable.EnableForDesign` of `Views.Markdown`, `MarkdownCodeBlock` and `MarkdownTable` (found by scanning Terminal.Gui's IL on 2026-10-28).

## Goals / Non-Goals

**Goals:**

- One command that proves the installed package starts, opens each reachable screen and dialog and quits cleanly, on the same bits a user installs.
- A smoke script that survives renamed titles, moved keys and slow runners, and fails with the name of the step that did not happen.

**Non-Goals:**

- Forbidding Terminal.Gui's Markdown, Code or other views that load `TextMateSharp.Grammars`: the product owner wants them available, so no architecture rule guards them.
- Asserting layout, colours or wording beyond the markers; behaviour stays with the reducer and handler tests.
- Replacing `check` (attach needs a signed-in CLI and stays in the non-blocking `handoff` job).
- Running the smoke test in `dotnet test`: it needs an installed tool and a seeded machine, so it is a CI step and a command developers can run by hand.

## Decisions

**1. A `smoke` command in HeadlessTerminal, not an xunit test.** `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree folder>` sits beside `check` and shares the driver. The pure parts (building the steps from `Wording`, finding a marker in a capture after a position) are unit tested in `tools\HeadlessTerminal.Tests`. Alternative: a trait-gated test like the Claude contract tests. Not taken: it would still need the installed tool and the seed from outside, and an environment-gated test that skips everywhere but CI hides its own failures.

**2. Steps wait for a marker, then send keys.** A `SmokeStep(Marker, Keys)` waits until its marker appears in the capture after the previous step's match, then waits a short settle (500 ms) and sends its keys. A marker is either a title (matched in the raw capture as an OSC 0 sequence, exact text) or screen text (matched in the capture with escape sequences stripped). Each step carries its own timeout: 60 seconds for the cold start and for the worktree scan (about 25 seconds with five worktrees on a laptop, task 1.2), 20 for the rest; a step that times out ends the run with exit code 4 and prints the step's marker. Closing a dialog is a step of its own whose marker is the title `Vantage`, so the next key never lands on a dialog that is still closing. Alternative: longer fixed delays. Not taken: they are either slow or flaky on shared runners, and they say nothing about which screen failed. The `drive` and `check` commands keep `KeystrokeScript` unchanged; the shared ConPTY, capture and query code moves out of `Driver.RunAsync` so both run modes use it.

**3. Everything the script sends or waits for comes from `Wording`.**
- Dialog markers: `Wording.TabTitleDuring(DialogName.Keys)`, `Settings`, `NewSession` and `ForceRemove`; the closed marker is `Wording.TabTitle`.
- View markers: the first column name a view has that the other view lacks, computed from `SessionsColumns` and `WorktreesColumns` (today `NAME` for sessions, `DIRTY` for worktrees).
- Keys: looked up in `SessionsKeys` or `WorktreesKeys` by their `Hint` ("Help", "Settings", "New session", "Worktrees", "Filter", "Force remove", "Sessions", "Quit"), with `esc` and `enter` translated to their sequences. A rebound key needs no change; a renamed hint fails the unit test that builds the script, naming the hint it could not find.

`KeyHelp` has no stable id other than its hint. Adding one would change `src\` (and need a version bump) for a test's benefit, so the hint is the lookup key and the unit test makes its loss loud.

**4. The script.** Sessions view → `?` (Keys) → Esc → `,` (Settings) → Esc → `n` (New session) → Esc → `w` (worktrees view) → wait for the worktree's folder name in the table (the scan has finished) → `?` → Esc → `/` and the worktree folder name → Enter (filter to the seeded worktree) → `D` (Force remove) → Esc → `w` back to sessions → `q`. Enter and `D` have no marker of their own (the filter's echo is not `Wording`), so they wait only the settle; a lost key shows as the force-remove marker timing out. Task 1.2 found that keys typed into the filter while the scan runs are lost, and that `D` is refused with no dialog on a worktree with a live session, so the named worktree must be idle. The run passes when every step's marker appeared and Vantage exited by itself within six seconds of `q`, exit code 0. The force-remove dialog is only ever cancelled: the script never types an answer, so even against a developer's own repositories nothing is removed.

**5. The worktree is named on the command line and filtered to.** The worktrees view lists the main checkout too, and `RemovalDecision` may refuse it, so the script filters to the seeded linked worktree before `D`. CI passes the folder name it seeded; a developer passes one of their own.

**6. CI: in the required `check` job, after `build-test-pack`.** New steps, all `pwsh`:
- Install Claude Code with the same `irm https://claude.ai/install.ps1 | iex` as `handoff`, with `DISABLE_AUTOUPDATER=1`, not signed in. No workspace trust is seeded: the smoke never starts a session, and CLAUDE.md keeps scripted trust to `handoff` and `claude-contract.yml`.
- Seed `~\source\repos\Northwind.Smoke` (`git init`, a local identity, one commit) and `git worktree add -b smoke ~\source\worktrees\Northwind.Smoke-smoke`.
- Install the package: `dotnet tool install Source191.Vantage --tool-path $env:RUNNER_TEMP\vantage-tool --add-source <package folder> --version <version>`.
- Run `smoke` from `tools\HeadlessTerminal` against `$env:RUNNER_TEMP\vantage-tool\vantage.exe`.
- Upload `drive-raw.txt` as `smoke-capture` when the step fails.

Alternative: the non-blocking `handoff` job, which already has the CLI. Not taken (the product owner's decision): a smoke test that cannot block a merge does not stop a broken package.

## Risks / Trade-offs

- [The CLI signed out may make `claude agents --json` fail or prompt, and the sessions view could show a load failure instead of rows] → the sessions marker is a column name, which the table draws whatever the listing says; the first task confirms this locally with `CLAUDE_CONFIG_DIR` pointing at an empty folder before any CI work.
- [The required `check` job now depends on the Claude Code installer being reachable] → a failed install fails fast with its own step name; re-running the job is the remedy. Accepted by the product owner.
- [The worktrees view's filter may not take Enter, or the filtered row may not be selected] → the first task drives it by hand with `drive` and records the keys that work; if `D` is refused for the seeded worktree, the footer appears and the run fails at the force-remove step, not later.
- [Terminal.Gui settles after the title is written, so a key sent at once could be lost] → the 500 ms settle; a lost key shows as a timeout on the next step's marker, never as a false pass.
- [Hint text is the key lookup] → a hint rename breaks the script's unit test with a clear message, a deliberate coupling to the help text rather than to key letters.
- [A run on a developer machine uses their real config and repositories] → the script only opens and cancels; the README says so and that `smoke` needs the same environment clean-up as `check`.
- [Dialogs needing a live session or data stay uncovered] → listed as out of scope in proposal.md; `handoff` covers attach, and a later change can seed a stopped session if needed.

## Migration Plan

Merging adds the steps to `check`; nothing to roll out. If the smoke step proves flaky, `continue-on-error: true` on that step only is the fallback while it is fixed, and is removed in the fixing pull request.