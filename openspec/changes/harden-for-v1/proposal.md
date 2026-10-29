# Proposal

## Why

The security review of 29 October 2026 (branch v-sec-rev, re-checked against main at 0.17.0-preview.7) found two confirmed medium findings and a handful of lower ones that should not ship in 1.0. Every program Vantage starts by bare name (git, gh, wt.exe, pwsh.exe) is resolved by Windows against the folder Vantage was started from before the PATH, so a planted `git.exe` in a cloned repository runs on the first scan (verified by experiment). Three places hand text to a batch file, which Windows runs through cmd.exe, where a double quote, ampersand or percent sign is read as commands or expanded: `claude.cmd` when Claude Code is the npm shim (session names, prompts, the folder name `vantage here` uses as the name), `az.cmd` now that lookups pass `--source-branch` (git allows `&`, `|`, `%` and `"` in a branch name), and a custom editor typed as a bare name. The rest is a safety label, a process id check, CI supply-chain pins and scanning, and writing the trust model down.

## What Changes

- **Programs by full path:** git, gh, wt.exe and pwsh.exe are found once at start-up by searching the PATH folders for the `.exe` and are started by that path; the current directory is never searched. A missing git stops the console and `vantage here` with one sentence and exit 1; a missing gh reads as the CLI being absent; a missing wt.exe or pwsh.exe fails the key that needs it with a sentence.
- **BREAKING** **Claude Code must be an executable:** `~\.local\bin\claude.exe`, then `claude.exe` on the PATH. A `claude.cmd` or `claude.bat` alone is refused with a sentence naming the file and the native installer, as a custom editor's batch file already is. The npm package now installs the same native binary, so the shim is only what cmd.exe would parse.
- **BREAKING** **A custom editor's executable must be a full path:** a bare name is refused when saved and reads as VS Code from `config.json`, as a batch file already does.
- **A dash-led opening prompt is refused:** the CLI reads a prompt that starts with `-` as options and has no `--` to stop it (`claude attach -- --help` answers `unknown option '--'`, checked 2026-10-29), so the wizard and `vantage here` refuse it with a sentence; `vantage here` exits 2.
- **One rule for what a batch file may be given:** letters, digits, spaces, dots, underscores, hyphens and slashes. It serves the Azure DevOps remote names (as today) and now the branch name: any other branch is not given to az, and the pull request key says so.
- **Pull request links open only over https.**
- **Close-empty and adopt check the process is still the session's:** its start time must be no later than the session's listed start; a reused process id is left alone and named in the footer.
- **A worktree of a repository outside the roots is "elsewhere":** a folder under a root or the worktree root whose `.git` file points at a live registration of a repository Vantage did not scan is listed with that repository, never as "not registered with git", and is refused deletion as a leftover even when forced. Start session, shell and the folder keys still work on it.
- **The update tab installs from nuget.org only** (`--source https://api.nuget.org/v3/index.json`), the feed the check consulted.
- **CI:** every action pinned to a commit SHA with a version comment; a CodeQL workflow for C# and Actions; build provenance attestation on the release package; Dependabot security updates turned on; the repository setting that requires SHA pins, turned on after this merges.
- **Documentation:** a "What Vantage trusts" section in the README; notes on `VANTAGE_LOG` recording keystrokes, the npm shim, the prompt rule, the branch rule and the elsewhere health; the matching rules and CLI facts in CLAUDE.md.
- Version 0.17.0-preview.8.

## Capabilities

### New Capabilities
- `external-tools`: how Vantage finds and starts the programs it depends on (Claude Code, git, the GitHub and Azure CLIs, Windows Terminal, PowerShell): by full path from the PATH, never through the current directory, never a batch file given anything but plain text, and what happens when one is missing.

### Modified Capabilities
- `session-launch`: the session start refuses an opening prompt that begins with a dash; `vantage here` exits 2 for a refused prompt as for a refused name.
- `command-line`: the scenario for a prompt that starts with a dash now refuses it.
- `pull-requests`: branch names reach az only when plain; links are https only; the pull request key explains a branch that was not looked up.
- `session-attach`: adopting and closing an empty terminal session end its process only while it is still the session's.
- `worktree-management`: the "elsewhere" health, its refusal from deletion, and the keys it keeps.
- `settings`: a custom editor's executable must be a full path.
- `tool-updates`: the update runs against nuget.org only.

## Impact

- **Domain:** a batch-file argument rule, a process identity rule, a new launch problem, a new settings problem, a new health kind with its reason and removal reason.
- **Ports:** a Claude location record.
- **Application:** `LaunchPlanBuilder`, `Wording` and its outcome words, `TabScripts`, `WorktreeInventory`, `WorktreesView`, the close-empty and adopt handlers.
- **Adapters:** a program locator; `ClaudeExecutable`, `GitCli`, `GitHubPullRequests`, `AzureDevOpsPullRequests`, `WindowsTerminal` and `Desktop` take or answer full paths.
- **Tui:** the worktrees row colour and the settings caption.
- **Program.cs:** locates the five programs and prints the refusals.
- **Tests:** new domain and locator facts; adapter, handler, reducer and wording facts for each rule; the contract and integration tests take the located path.
- **CI and repository:** `check.yml`, `release.yml`, `publish.yml`, `claude-contract.yml`, a new `codeql.yml`; two repository settings through `gh api`.
- **Docs:** README and CLAUDE.md.
- **Out of scope:** multiple Claude accounts (a later change); running the npm shim through node instead of refusing it; counting or removing an "elsewhere" worktree through its own repository; replacing the `Interop.UIAutomationClient` package; author-signing the package; the diagnostic log's content; the `pwsh.exe` named in a tab's own command line, which Windows Terminal resolves from its own process, and the `dotnet` the update tab calls, which PowerShell resolves without the current directory.
