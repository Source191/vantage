---
paths:
  - ".github/**"
  - "tools/HeadlessTerminal/**"
  - "tools/HeadlessTerminal.Tests/**"
  - "test/Source191.Vantage.Infrastructure.Tests/Contract/**"
---

# The checks and the workflows

What each check does and how CI runs it. The commands, and when each check is due, are in `CLAUDE.md`; `CONTRIBUTING.md` is the contributor's account of the same workflows.

## Workflows

- `.github\workflows\check.yml` runs on pull requests and pushes to `main` on a Windows runner and is required by branch protection; a pull request the owner did not open also needs the owner's approval, through `.github\CODEOWNERS` and the "Owner approves main" ruleset, which the owner bypasses for their own.
- It builds Release with the runner image's SDK (no `setup-dotnet`; `global.json` chooses it), tests, checks whitespace only (`dotnet format whitespace`, because the build already fails on style and analyser warnings), packs and uploads the nupkg. A pull request touching only `openspec\` or `.md` files outside `.github\` skips all of that after the version check, through `.github\scripts\Test-DocumentationOnly.ps1`; pushes to `main` run everything, and a new commit cancels a pull request's older run.
- Every action is pinned to the commit of its release with the version in a comment (Dependabot keeps the pins current; the repository setting requiring pins is turned on once every workflow on `main` has them).
- `.github\workflows\codeql.yml` runs CodeQL over the C# (build mode none) and the workflows on pull requests, pushes to `main` and weekly, never as a required check.
- `.github\workflows\release.yml` publishes a GitHub Release tagged `v<version>` when a push to `main` changes `Version` in the csproj: a `build` job with `contents: read` checks the version, builds, tests, packs and uploads the nupkg as an artefact, and a `release` job (`needs: build`, no checkout) holds `contents: write`, `id-token: write` and `attestations: write` to download it, attach a build provenance attestation and run `gh release create`. Immutable releases are on for the repository (`gh api repos/Source191/vantage/immutable-releases`). The first step of `check` (`.github\scripts\Assert-VersionBumped.ps1`) fails a pull request that changes the packaged tool without raising `Version` (the tool cache keeps a version for ever), so each merge of one is a release.
- Publishing to nuget.org is `.github\workflows\publish.yml`, run by hand on a tested release tag; it verifies that release's nupkg with `gh attestation verify --signer-workflow Source191/vantage/.github/workflows/release.yml --source-ref refs/heads/main` and pushes it without building, through Trusted Publishing (`NUGET_USER` secret) in the `nuget` environment (runs from `main` only, each approved by the owner, and the nuget.org policy names it), and never happens from `release.yml`, because nuget.org versions cannot be replaced.

## Hand-off check

- `dotnet run --project tools\HeadlessTerminal -- check` runs `vantage here` with a throwaway name and no prompt, which makes no model call, waits through `StepRunner` for Claude's `Claude Code v` banner, about 12 s, sends Ctrl+Z and waits for `here` to exit, then stops and removes the job by name; exit 1 means `here` did not exit after the detach, 3 that the banner never appeared (checked 31 October 2026, CLI 2.1.316).
- It is the one automated test of a real `claude attach` and detach; the console itself never hands over, so the blocked-UI-thread failure it was first built for can no longer happen.
- A [pseudo-console](https://learn.microsoft.com/windows/console/creating-a-pseudoconsole-session) child is started with [`STARTF_USESTDHANDLES`](https://learn.microsoft.com/windows/win32/api/processthreadsapi/ns-processthreadsapi-startupinfow) and no handles, or its standard output (and Claude's whole screen) would go to the driver's own stdout whenever that is piped, as in CI.
- The `handoff` job in `check.yml` runs the same check on the runner when the `CLAUDE_CODE_OAUTH_TOKEN` secret exists, and never blocks a merge.
- The same tool has `drive` (scripted keystrokes into a ConPTY pseudo-console, capture to drive-raw.txt), `grid` (replay a capture as a screen) and `count` (plain-text markers). Keys sent before Terminal.Gui has initialised (about 8 s on a cold start) are lost, so scripts start at 8000 ms.

## Smoke check of the packed tool

- `smoke <exe> <worktree-folder>` opens and cancels the Keys help, the wizard, the problem report, the first repository root's dialog on the Config tab and its folder picker (F3), the worktrees view, its help and force remove (on an idle linked worktree), then quits; exit 0 passes, 1 is a blocked UI thread, 2 no exe, 4 a step whose marker never appeared (named in the output).
- Its steps take their keys (by hint) and markers (dialog tab titles, a column only each view has, and the worktrees title's ", selected: " as the scan-finished signal, since the WORKTREE cell reads `repository › slug` and never the folder name) from `Wording`, so a new dialog is one step in `SmokeScript`.
- The `check` job installs the Claude Code CLI (signed out: `claude agents --json` prints `[]`), seeds `~\source\repos\Northwind.Smoke` with the linked worktree `Northwind.Smoke-smoke`, installs the packed nupkg into a temporary `--tool-path` and runs it, so it is the one test of the bits users install.

## Claude Code contract tests

- `test\...\Infrastructure.Tests\Contract` drives Vantage's own Claude adapters against the installed CLI (version, a `--bg -n` start with no prompt, the listing's fields and state, stop and rm) and compares the commands and options of `claude --help` and `claude agents --help` with `Contract\Snapshots`.
- `.github\workflows\claude-contract.yml` runs them nightly against the latest CLI, with the hand-off check, in a job with `contents: read`; when the install or a contract test fails, a `report` job (`needs: contract`, `issues: write`) reads the outcomes and the `contract-results` artefact and comments on the open `claude-cli` issue or opens one assigned to the `CLAUDE_CLI_WATCHER` repository variable.
- A snapshot difference leaves `<name>.received.txt` in the output folder (and the run's `contract-results` artefact).
- Automated runs, measured: a prompt-less `claude -p` with its standard input redirected and never written waits 3 s for input, fails on it without a model call and exits 1; on a clean machine (a fresh `CLAUDE_CONFIG_DIR`, as on the runner) it registers as an automated run at about 7 s and exits at about 8.5 s, before a listing can see it, and a user's own hooks lengthen that (the owner's SessionEnd hook adds about 10 s; measured 2026-10-31, CLI 2.1.316). The `ClaudeContract` fact checks the automated-run rule by holding the run open with a SessionEnd hook passed through `--settings` that sleeps for 20 s (listed at about 6 s, exit 1 at about 27 s on a clean machine; a user's SessionEnd hooks run as well and add their time). Start such a run after Vantage is up when checking the screen, since Vantage's first listing takes about 10 s.
