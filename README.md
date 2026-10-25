# Vantage

A terminal console for Claude Code sessions and git worktrees on Windows.

Supervise your Claude Code sessions from one terminal tab: start, attach, rename, stop, and clean up the worktrees they leave behind. Vantage lists every Claude Code session on the machine on one screen, with its state, repository, branch and description, and lets you attach to a session, open it in its own coloured Windows Terminal tab, start new sessions in a repository or a fresh sibling worktree, and tidy up the worktrees that sessions leave behind. Sessions run under Claude Code's own background supervisor (`claude --bg`), so they keep working when no terminal shows them, and the terminal bell tells you when one needs you.

> **To do:** add a screenshot of the sessions view here before the repository is made public.

## Contents

- [Install](#install)
- [Start Vantage](#start-vantage)
- [The sessions view](#the-sessions-view)
- [The new session wizard](#the-new-session-wizard)
- [Attaching, tabs and getting back](#attaching-tabs-and-getting-back)
- [The worktrees view](#the-worktrees-view)
- [vantage here](#vantage-here)
- [Notifications](#notifications)
- [Common scenarios](#common-scenarios)
- [Where Vantage keeps things](#where-vantage-keeps-things)
- [Troubleshooting](#troubleshooting)
- [Developing Vantage](#developing-vantage)
- [Continuous integration](#continuous-integration)
- [Working with OpenSpec](#working-with-openspec)
- [Roadmap and known limits](#roadmap-and-known-limits)

## Install

Prerequisites: Windows 11, Windows Terminal, PowerShell 7, .NET 10 SDK, git, and Claude Code 2.1.309 or later installed at `~\.local\bin\claude.exe` or on the PATH.

1. Pack and install the tool from this repository:

   ```powershell
   .\tools\install-local.ps1
   ```

   The script stops a running `vantage.exe`, packs the tool as `<Version>-local.<timestamp>` and installs or updates the global tool. The suffix matters: the tool cache keeps the first package it saw for a version, so two plain builds of 0.1.18 would run the first one. `-Clean` uninstalls first.

   To test the package a GitHub Actions run built instead of building locally, download the run's `Source191.Vantage-<version>-ci.<run number>` artefact and install from the folder:

   ```powershell
   gh run download <run id> --name Source191.Vantage-0.1.18-ci.42 --dir .\downloaded
   .\tools\install-local.ps1 -FromArtifact .\downloaded
   ```

   Or by hand: `dotnet pack src\Source191.Vantage -c Release` then `dotnet tool install -g Source191.Vantage --add-source .\artifacts\packages`.

2. Let Claude Code ring the terminal bell (optional but recommended). Add this to `~\.claude\settings.json`:

   ```json
   { "preferredNotifChannel": "terminal_bell" }
   ```

   Claude then rings the bell when it finishes a task or pauses for a permission prompt while you appear to be away, and Windows Terminal flags the tab the session is in.

   Suggested Windows Terminal setting, so a bell in a background tab is hard to miss: in Windows Terminal's `settings.json` (Settings, then "Open JSON file"), under `profiles` > `defaults`:

   ```json
   "bellStyle": ["audible", "window", "taskbar"]
   ```

   That sounds the bell and flashes the window and the taskbar entry. The same option is in the settings UI under Defaults > Advanced > "Bell notification style"; drop `"audible"` if you want it silent.

3. Add the profile shortcuts (optional). These two lines in `$PROFILE` give `v` for `vantage` and `vh` for `vantage here`:

   ```powershell
   Set-Alias v -Value vantage
   function vh { vantage here @args }
   ```

New shells pick up `~\.dotnet\tools` on the PATH; a shell that was open before the install needs restarting.

## Start Vantage

Open a tab and run `vantage` (or `v`). The screen has three parts:

- **Header:** the machine, the Claude executable, the repository roots being scanned, counts of sessions by state, and the key hints.
- **Table:** one row per session, titled `sessions(all)[n]`, or `sessions(<filter>)[n]` when a filter is set. The selection bar is cyan.
- **Footer:** the current view as a crumb, then the last message: what a key did, why it was refused, or what the selected row needs. A spinner sits beside it while an action runs (starting a session, opening a tab, renaming, deleting), and one sits in the middle of the empty table until the first listing or worktree scan arrives.

The table refreshes every five seconds. `?` shows the keys for the current view and `q` quits. Quitting Vantage never stops a session.

Vantage names its own tab `Vantage` and colours it green (or the first free palette colour when a session tab is already green), so it is easy to find among the session tabs; they are restored when you return from an attached session (Claude names the tab itself while attached) and put back when you quit. A Windows Terminal profile that sets its own `tabColor` keeps it, because the terminal ignores colour changes from inside such a tab.

## The sessions view

Each row is a Claude Code session, ordered so that the ones needing attention come first:

| Column | Meaning |
| --- | --- |
| NAME | The session name given at launch (`-n`), or the folder name for an unnamed one |
| STATE | needs input (amber), working (cyan), idle (white), completed (green), failed (red), stopped (grey) |
| REPOSITORY, BRANCH | The folder the session runs in and its current branch |
| WT | `yes` when the session runs in a linked worktree rather than the repository itself |
| AGE | Time since the session started |
| TAB | `terminal` for a session started as plain `claude` in a tab, `open` while a Vantage tab shows the session |
| DESCRIPTION | The free-text description given in the wizard or with `vantage here -d` |

Keys (every key is dispatched by the tested reducer, so this table is the reference):

| Key | Action |
| --- | --- |
| Up, Down | Move the selection bar |
| Enter | Attach to the selected background session in this tab; Ctrl+Z returns |
| o | Open the session in its own coloured Windows Terminal tab; when one is already open for it, switch to that tab |
| n | New session wizard |
| r | Rename the session from Vantage: type the new name, Enter. The session restarts under the new name with the same id and conversation, so it is refused while the session is working or before its first message |
| x | Stop the session (it stays listed and can be resumed) |
| d | Delete the session, after a confirmation, and the worktree Vantage created for it when nothing in it would be lost; asks before deleting that worktree's branch when it is not merged. On a `terminal` row that has never been sent a message, closes that tab instead |
| C | Close every tab whose plain `claude` has never been sent a message, after a confirmation naming them |
| b | Move a plain terminal session under Vantage's supervision (its tab becomes an ordinary shell) |
| R | Resume sessions that stopped with a reboot |
| a | Open Claude Code's own agent view |
| w | Switch to the worktrees view |
| / | Filter by name, repository or branch; Enter keeps the filter, Esc clears it |
| ? | Help |
| q | Quit |

Rows marked `terminal` are sessions started by hand with `claude` in some tab. Vantage can see them but cannot attach to or stop them; press `b` to bring one under supervision, or type `/bg` inside it. One that has never been sent a message has nothing to keep: `d` closes its tab, `C` closes all such tabs, and the footer says so. Vantage ends the claude process, then ends the tab's shell with a clean exit code, which is what makes Windows Terminal close the tab; a session not inside a Windows Terminal tab is just ended.

## The new session wizard

Press `n`. The left column lists your repositories (recent ones first, then everything under the roots, then the roots themselves for a session with no repository); type to filter it and use the arrows to choose, then Enter confirms the repository and moves to the form. The right column is the form:

- **Session name (required):** the label shown in the NAME column. It also names the worktree folder and branch when you choose a worktree.
- **Description (optional):** shown in the DESCRIPTION column so sessions with similar names can be told apart.
- **Where:** `Current branch` runs the session in the repository as it is; `New sibling worktree` creates `<repository>-<name>` beside the repository on branch `<name>`, copying the git-ignored files named in the repository's `.worktreeinclude` so builds work there too. Use the arrows to change the choice.
- **Worktree base:** the branch the new worktree starts from, `origin` default branch or the current HEAD.
- **Opening prompt:** sent to the session as soon as it starts. It wraps as you type. Leave it blank and the session opens on Claude's agent list until you send it something.

A cyan line under the choices says exactly what will happen ("Worktree Northwind.Orders-fix-tests beside the repository, on branch fix-tests."). Then:

- **F5 Start:** start the session and stay in Vantage.
- **F6 Start and open in tab:** start it and open it in its own Windows Terminal tab straight away, as `o` does; Vantage stays on screen.
- **Esc:** cancel.

Enter never starts a session, so there is nothing to undo if you press it too soon. If a repository has not been trusted by Claude Code yet, the start is refused with an explanation and Vantage offers to open a plain tab there so you can run `claude` once and accept the trust prompt.

## Attaching, tabs and getting back

**Attach in this tab (Enter):** Vantage hands the terminal to `claude attach`. Press Ctrl+Z to detach and return to Vantage; the session keeps running. If the session has never had a message, Claude shows its agent list instead of a conversation: Space on the session sends it a prompt, Esc returns to Vantage. Vantage prints this reminder before every attach.

**Open in a tab (o):** Vantage opens a new Windows Terminal tab, titled with the session name and coloured from a palette so each session's tab is recognisable, and attaches there. The TAB column shows `open`. While that tab is open, `o` switches to it instead of opening a second one (Vantage finds the tab by its title through Windows UI Automation, so it works in whichever window holds the tab); close the tab (or detach in it) and the column clears within a few seconds.

**The native agent view (a):** Claude Code's own `claude agents` screen, for anything Vantage does not do, such as pinning. Esc returns.

## The worktrees view

Press `w`. Vantage lists every linked worktree under your repository roots, plus the leftovers git has forgotten. The first scan of a large roots folder takes a while (around forty seconds for fifty repositories); it refreshes every fifteen seconds after that.

```text
 Worktrees:  4 (3 ok, 1 broken)                               <s>       Start session     <?>     Help
 At risk:    1 with changes, 2 with unpushed commits          <o>       Open shell        <q>     Quit
                                                              <enter>   Go to session
  worktrees(all)[4]
 ╭──────────────────────────────────────────────────────────────────────────────────────────────────────────────────╮
 ││REPOSITORY          │FOLDER                        │BRANCH                   │DIRTY│UNPUSHED│LAST COMMIT│SESSION   │HEALTH │
 │ Northwind.Billing    Northwind.Billing-invoice-pdf   invoice-pdf                     12       3d4h       invoice-pdf ok    │
 │ Northwind.Billing    Northwind.Billing-study         study/reporting          5                9d16h                 ok    │
 │ Northwind.Orders     Northwind.Orders-fix-tests      fix-tests                        2        1d0h       fix-tests   ok    │
 │ Northwind.Sales      Northwind.Sales-republish       -                                         -                     broken│
```

| Column | Meaning |
| --- | --- |
| REPOSITORY, FOLDER, BRANCH | The owning repository, the worktree folder and its branch |
| DIRTY | Changed or untracked files in the worktree |
| UNPUSHED | Commits on its branch that no remote has |
| LAST COMMIT | Age of the last commit |
| SESSION | The session running inside it, if any |
| HEALTH | `ok`, `locked` (by another tool), `orphan` (a folder under `.claude\worktrees` git no longer lists), `broken` (a registration whose folder is gone, or a folder whose registration or repository is gone), `empty` (a leftover `.worktrees` folder with nothing in it) |

When nothing else needs saying, the footer explains the selected row's health in full.

| Key | Action |
| --- | --- |
| d | Remove the worktree, or delete a leftover folder. Refused, with the reason, when it has changed files, unpushed commits, a lock or a live session. A broken leftover also has its stale registration pruned from the repository. Junctions and links inside a leftover (pnpm's `node_modules`, for one) are removed as links and never followed, so nothing outside the folder is touched. A leftover's files are counted up to 1,000, so a large one is refused as "at least 1000 files" |
| D | Force removal: the folder name opens selected in a read-only box, so Ctrl+C copies it (F2 also copies, from anywhere in the dialog); Tab or Enter moves to the answer box, Ctrl+V pastes, Enter confirms. There is no Remove button; a wrong or empty answer keeps the dialog open and says what was typed, and Esc cancels. Never forces a locked worktree or one with a live session |
| p | Prune stale registrations in the row's repository |
| s | Start a session in this worktree (the wizard opens with the directory fixed) |
| o | Open a plain PowerShell tab in the worktree |
| Enter | Go to the session running in the worktree (selected by its id, so two sessions with the same name cannot be confused) |
| w, Esc | Back to the sessions view (Esc clears a filter first) |
| / | Filter by repository, folder, branch or health |

Branches: when a removed worktree's branch was created by Vantage and is merged, the branch is deleted too; when Vantage created it but it is not merged, or you created it by hand, Vantage asks before deleting it.

## vantage here

From any folder, `vantage here` starts a supervised session in that folder, on its current branch, and attaches in the same tab, so a session started from an ordinary terminal is Vantage-managed from the outset. The session is named after the folder unless you give a name.

```powershell
vantage here
vantage here fix-tests -p "Make the failing tests in OrdersShould pass" -d "Flaky orders suite"
vh fix-tests          # with the profile shortcut
```

Options: `-p`/`--prompt` sends an opening prompt (without one the session opens on the agent list), `-d`/`--description` sets the DESCRIPTION column. Ctrl+Z detaches and returns to the shell; the session keeps running and is listed in Vantage. Exit codes: 0 after a detach, 1 when the start failed (for example an untrusted folder), 2 for a usage error.

## Notifications

Two things ring the terminal bell, and Windows Terminal flags the tab (or the window or taskbar, depending on the profile's bell style):

- **Claude Code itself**, with `preferredNotifChannel` set to `terminal_bell` (see Install), rings the bell in the tab a session is attached in when it finishes a task or pauses for a permission prompt while you appear to be away.
- **Vantage** rings the bell in its own tab when a background row changes to needs input between two refreshes, so a session with no tab of its own still gets your attention.

There are no toasts; the header's Need input count and the amber rows are the overview.

## Common scenarios

**Start work on a story in a clean worktree.** `n`, choose the repository, name the session after the story, pick `New sibling worktree`, type the opening prompt, F6. The session opens in its own coloured tab and the overview stays where it is. The worktree is `<repository>-<name>` beside the repository, and the WT column marks the session.

**Kick off several sessions and let them run.** Use `n` with F5 for each, with a description so you can tell them apart. Leave Vantage open: rows turn amber when one needs you, and the Vantage tab rings the bell even when another tab is in front. Enter on the amber row, answer, Ctrl+Z.

**Keep one session in its own tab.** `o` opens it in a coloured tab you can leave open all day. The TAB column shows `open`; `o` again switches to that tab until it is closed.

**Start Claude from a repository folder you are already in.** `vh fix-tests -p "..."` instead of `claude`. It shows up in Vantage with a job id, so `o`, `x` and `d` work on it later.

**A session you started with plain `claude` needs the same treatment.** Select its `terminal` row and press `b` while it is idle or waiting. Vantage ends the plain process and resumes the same conversation under the supervisor with the same name; the old tab becomes an ordinary shell. A session you have not sent anything to yet is refused, because Claude writes no transcript until the first message and there would be nothing to resume: `d` closes that tab instead.

**After a reboot.** Sessions show as stopped or failed with no process. `R` resumes the failed ones and offers the stopped ones as a checklist.

**Finish a story and clean up.** `d` on the session deletes it and, if Vantage created the worktree and it is clean and pushed, offers to remove the worktree and its merged branch. Anything with uncommitted or unpushed work is refused with the reason.

**Find out what is cluttering the repositories folder.** `w` lists every worktree with dirty and unpushed counts and health. Empty leftover folders go with `d`; a worktree with uncommitted work needs `D` and the copied folder name, which is deliberate.

**Something needs Claude Code's own screen.** `a` opens the native agent view; Esc returns.

## Where Vantage keeps things

Everything Vantage owns is under `%LOCALAPPDATA%\Vantage`:

| File or folder | Contents |
| --- | --- |
| `config.json` | `repositoryRoots` (default `~\source\repos`) and `defaultBase` (`RemoteDefault` or `Head`) |
| `descriptions\` | One file per session description |
| `recent\` | One file per recently used repository, with when it was last used; the ten most recent are shown first in the wizard |
| `created.json` | Worktrees Vantage created, which decides the branch-deletion rules |
| `tabs\` | One file per tab Vantage opened, so a second `o` switches to the tab instead of opening another |

A missing or corrupt file is treated as empty. Descriptions and recent repositories are one file each so that `vantage here` and the console can record at the same time without losing a write; the `sessions.json` and `recent.json` files of versions before 0.3.0 are imported once and removed. Sessions themselves belong to Claude Code (`claude agents --json`), never to Vantage.

To scan more than one roots folder, edit `config.json`:

```json
{ "repositoryRoots": ["C:\\Users\\you\\source\\repos", "D:\\work"], "defaultBase": "RemoteDefault" }
```

## Troubleshooting

- **Keys stop working after returning from a session.** Set `$env:VANTAGE_LOG = "$env:TEMP\vantage.log"` before starting Vantage; the log records every hand-off and Terminal.Gui's own diagnostics. The known cause (blocking the UI thread after a hand-off) is fixed and guarded by the check in `tools\HeadlessTerminal`.
- **Attach shows a grey list, not a conversation.** The session has no messages yet; that is Claude's agent list. Space sends it a prompt, Esc returns to Vantage. Give sessions an opening prompt to skip this.
- **"not trusted yet".** Claude Code refuses to start in a folder you have not trusted. Open a tab there, run `claude`, accept the prompt, then start again. Vantage never edits the trust records.
- **No bell when a session needs you.** Check `preferredNotifChannel` in `~\.claude\settings.json` and the bell style of the Windows Terminal profile; Claude only rings when it thinks you are away from the terminal.
- **Tabs or sessions opened from Vantage have no colours.** Vantage was started from inside a Claude Code session, whose environment sets NO_COLOR; start Vantage from an ordinary tab.
- **The worktrees view is slow to appear.** The first scan runs `git worktree list` for every repository under the roots and three git commands per worktree. Narrow `repositoryRoots` if the folder is very large.
- **Claude Code was not found.** Vantage looks for `~\.local\bin\claude.exe`, then `claude` on the PATH.

## Developing Vantage

```powershell
dotnet build -c Debug                                              # warnings are errors
dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build  # the fast in-memory suite, about two seconds
dotnet test -c Debug --no-build                                    # every suite, including real git and processes
dotnet format --verify-no-changes
dotnet run --project tools\HeadlessTerminal -- check               # the hand-off check, from tools\HeadlessTerminal
```

- Four projects under `src\`: `Source191.Vantage.Core` (`Domain` rules, `Ports` boundary interfaces, `Application` with the `Views` reducers, `Actions` and `Launch` use cases, the screen loop and command line), `Source191.Vantage.Infrastructure` (git, Claude CLI, Windows Terminal, processes, files, state), `Source191.Vantage.Tui` (Terminal.Gui rendering and dialogs) and `Source191.Vantage` (`Program.cs`, the only composition root, packed as the tool with the other three inside). Project references and an architecture test keep the dependencies pointing one way.
- Tests are sociable: real collaborators, fakes only at the boundaries (`test\Source191.Vantage.TestDoubles`). `Core.Tests` is the fast in-memory suite; `Infrastructure.Tests` uses real git, processes and files. Every key in the tables above is dispatched by a reducer test. The domain rules are mutation tested with Stryker.
- `CLAUDE.md` records the verified Claude Code CLI and Terminal.Gui facts Vantage depends on, and the rules for working on it; `openspec\` holds the specifications and changes (below).
- `tools\HeadlessTerminal` drives the built exe inside a pseudo-console with scripted keys, replays the screen as text and runs the hand-off check; it is how the hand-off bug was reproduced and how the screen in this README was rendered.
- A Release build of `vantage.exe` cannot be rebuilt while it is running; use Debug for the test loop and stop the running instance before `dotnet pack` (`tools\install-local.ps1` does both).

## Continuous integration

`.github\workflows\check.yml` runs on every pull request and every push to `main`, on a `windows-latest` runner because the code uses P/Invoke, ConPTY and `cmd.exe` in its integration tests. Its `check` job builds in Release (warnings are errors), runs the whole test suite against that build, runs `dotnet format --verify-no-changes`, packs the tool as `<Version>-ci.<run number>` and uploads the `.nupkg` as a workflow artefact named `Source191.Vantage-<that version>`, so a working package is downloadable from any commit and each run's package is a version the tool cache has never seen (see Install). Branch protection on `main` requires `check`, so a red pull request cannot merge. Administrators can still push to `main` directly; do not, because the release check below only looks at the last commit of a push.

**Releases.** `.github\workflows\release.yml` runs on every push to `main` and is a no-op unless that push changed `Version` in `src\Source191.Vantage\Source191.Vantage.csproj`. When it did, it builds, tests and packs again and publishes a GitHub Release tagged `v<version>` with the `.nupkg` attached. Bumping `Version` is therefore the release action, and every pull request that changes the packaged tool makes it, so each merge of a tool change is a release. The tool is not on nuget.org yet (see Roadmap), so the release is provenance, an immutable package tied to a commit, rather than a distribution route.

**Version bumps.** The first step of the `check` job, on pull requests only, runs `.github\scripts\Assert-VersionBumped.ps1` against the two parents of the pull request's merge commit. It fails when the pull request changes anything under `src\`, or `Directory.Build.props`, `global.json` or `nuget.config`, without raising `Version` above `main`'s (compared as semantic versions, so 0.10.0 is above 0.9.0). Pull requests touching only docs, CI, `openspec\`, `test\` or `tools\` need no bump. Run it locally with `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD`. Versions follow [semantic versioning](https://semver.org/):

- **Patch:** fixes, refactors with no behaviour change, and changes to text.
- **Minor:** a new key or a new behaviour that keeps existing keys working.
- **Major:** a change that breaks how existing keys, state files or the `vantage here` command line behave. While the tool is `0.y.z`, a minor bump may break.

Branch protection on `main` has "Require branches to be up to date before merging" on. Without it a pull request's check can pass against an old `main`: two open pull requests that both bump to the same version merge without a conflict, and the second one is never released. With it, updating the branch reruns the check against the current `main`, and the second one fails until it bumps again.

**The hand-off job.** `check.yml` has a second job, `handoff`, that installs Claude Code on the runner and runs the hand-off check from `tools\HeadlessTerminal` against a real throwaway session. It needs a `CLAUDE_CODE_OAUTH_TOKEN` repository secret (from `claude setup-token`); without one it skips with a notice. It never blocks a merge, because its keystroke timings were tuned on a laptop and a slow runner can fail it for reasons that are not bugs; read its `handoff-capture` artefact when it does. The runner has no desktop, so it seeds workspace trust for the checkout in its own `~\.claude.json`, which is the one place that record is ever written by a script. One caveat until the first live run has been read: Vantage strips `CLAUDE_CODE_*` from the environment of the `claude` processes it starts (`InheritedEnvironment`), so the token reaches the throwaway session the harness starts but not Vantage's own `claude agents` and `claude attach`. If those turn out to need it, the attach never happens and the check still exits 0 because Vantage quits on `q`; the capture shows whether Claude's screen was actually reached.

### Manual checks for tab and attach changes

The runner drives Vantage inside a pseudo-console, which is the same plumbing Windows Terminal uses for a tab, so rendering, keys and the attach hand-off are covered. Anything that goes out through `wt.exe` is not: a runner has no desktop for a tab to appear on. Before merging a pull request that changes how Vantage drives `wt.exe`, opens, titles or colours tabs, or attaches to a session, install its package (`tools\install-local.ps1`) and run these in a real Windows Terminal:

1. `o` on a session opens a coloured tab titled with the session name, and `o` again switches to that tab while it is open.
2. `n`, pick `New sibling worktree`, F6: a new tab opens attached to the new session, and the WT column marks the row.
3. `d` on that session ends claude and the tab closes by itself.
4. A background session that pauses for input turns its row amber and rings the bell in the Vantage tab while another tab is in front.
5. Ctrl+Z from an attached session returns to the table with the keys still working.
6. The tab Vantage runs in is titled `Vantage` and green, and goes back to the profile's title and colour on `q`.

## Working with OpenSpec

The repository uses [OpenSpec](https://github.com/Fission-AI/OpenSpec) for spec-driven changes. `openspec\specs` describes what Vantage does today, one capability per folder (session listing, launch, attach, worktree management, needs-input notification). Each piece of work is a change under `openspec\changes\<name>` with a proposal, a design, the delta specs it adds or modifies, and a task list of red-green-refactor steps; `openspec\changes\archive` keeps finished changes.

- **Propose:** `/opsx:propose <name>` (or `openspec new change`) writes the proposal, design, delta specs and tasks for a feature or fix, in that order.
- **Apply:** `/opsx:apply <name>` works through the tasks test-first, ticking each one; `openspec status --change <name>` shows progress.
- **Archive:** `/opsx:archive <name>` when the tasks are done; the delta specs are merged into `openspec\specs` so they stay the single description of current behaviour.
- `openspec\config.yaml` holds the project context every change inherits (platform, CLI surface, worktree conventions, coding style); update it when a rule changes rather than repeating the rule in each proposal.

## Roadmap and known limits

- Windows only, with Windows Terminal; tab colours and titles use `wt.exe`.
- Still to observe at the terminal: whether `claude attach` to one session from two tabs is refused or allowed, and whether `claude --bg --resume <id>` or `claude respawn --all` is the right way to bring sessions back after a reboot (the listing side is documented: no pid, state failed or stopped).
- A take-over key (stop the supervised session and resume it as a normal foreground `claude`, at the cost of supervision) is undecided; `b` covers the other direction.
- Multiple Claude accounts: Claude Code's `CLAUDE_CONFIG_DIR` selects a configuration directory with its own login, settings, sessions and supervisor; Vantage would gain a profile column and set the variable when starting a session.
- A per-path cache for the worktree scan, so the first press of `w` is quick on a large roots folder.
- Pinning sessions, once the CLI exposes it outside the native agent view; configurable palette and Windows Terminal profile for the tabs Vantage opens.
- Publish to nuget.org: planned in the `public-release` change (`openspec\changes\public-release`), which also holds the order the open changes merge in; a hand-run `publish.yml` pushes a tested release through Trusted Publishing, and the Install section drops `--add-source`.
