# Proposal

## Why

No automated test runs the Vantage that people install. The unit and adapter suites run each test project's own build output, and the hand-off check runs `src\Source191.Vantage\bin\Release\net10.0\vantage.exe` and opens only the sessions table, attach and the wizard. A package that lost a DLL, or a screen or dialog that fails to open, passes every check today and is found only by retesting the whole UI by hand. The licence question over `TextMateSharp.Grammars.dll` (whether it could ever be left out of the package) raised this, but the gap matters for every release.

## What Changes

- **A blocking smoke test of the packed tool in `check`.** After `build-test-pack`, the job installs the Claude Code CLI (not signed in), seeds a throwaway repository with a linked worktree under the runner's default repository root, installs the freshly packed nupkg into a temporary `--tool-path` and runs a new `HeadlessTerminal smoke <exe>` command against it. The command opens the sessions view, the Keys help, Settings, the new-session wizard, the worktrees view, its help and the force-remove confirmation, cancels each one, quits, and passes only when every screen appeared and Vantage exited by itself.
- **The smoke script is built from `Wording`, not literals.** Keys come from `Wording.SessionsKeys` and `Wording.WorktreesKeys`, dialog markers from `Wording.TabTitleDuring(DialogName)`, and view markers from the column names in `Wording.SessionsColumns` and `Wording.WorktreesColumns`, so a renamed title or a moved key needs no test change. `HeadlessTerminal` gains a project reference to `Source191.Vantage.Core` for this.
- **The driver waits for text instead of a fixed time.** A smoke step waits until its marker appears in the capture (with a timeout) before it sends its keys, so a slow runner does not fail it. The existing `drive` and `check` commands keep their fixed-delay scripts.
- **Docs:** the HeadlessTerminal README and CLAUDE.md's build section describe `smoke`; the README's stale pointer to `test\Source191.Vantage.UnitTests\HeadlessTerminal` is corrected to `tools\HeadlessTerminal.Tests`.
- **Version:** no bump; nothing under `src\` changes.

Out of scope: leaving `TextMateSharp.Grammars.dll` out of the package (a later decision this change makes safe to take); the dialogs that need a live session or data a clean runner lacks (rename, delete session, resume, close empty, delete branch, open solution, update); any rule against Vantage using Terminal.Gui's Markdown, Code or other views that load `TextMateSharp.Grammars` (the product owner wants them available); visual or layout snapshots; signing the CLI in on `check`; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The change adds tests and CI only and no behaviour of Vantage changes; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **CI:** `.github\workflows\check.yml` gains the CLI install, the repository seed, the tool install and the smoke step, and uploads the smoke capture as an artefact when it fails; the required `check` job now depends on the Claude Code installer being reachable.
- **Tools:** `tools\HeadlessTerminal` (a `smoke` command, wait-for-text steps, a reference to `Source191.Vantage.Core`) and `tools\HeadlessTerminal.Tests`.
- **Docs:** `tools\HeadlessTerminal\README.md`, `CLAUDE.md`.