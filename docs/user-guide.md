# Torni user guide

Everything Torni does, in more detail than the [README](../README.md). In the console itself, `?` on any tab lists every key, column and detail line, and every column header and key in the key bar has a tooltip.

## Contents

- [Installing](#installing)
- [The console](#the-console)
- [Sessions](#sessions)
- [Worktrees](#worktrees)
- [Configuration](#configuration)
- [Notifications and profiles](#notifications-and-profiles)
- [Every key](#every-key)
- [Privacy](#privacy)
- [Troubleshooting](#troubleshooting)

## Installing

It runs on Windows 10 or 11. The one program you install yourself is Claude Code 2.1.309 or later, with its native installer (winget's own Claude Code package can be older than Torni needs, and does not update itself):

```powershell
irm https://claude.ai/install.ps1 | iex   # Claude Code
[Environment]::SetEnvironmentVariable('Path', [Environment]::GetEnvironmentVariable('Path', 'User') + ";$HOME\.local\bin", 'User')   # puts claude on the PATH
winget install Source191.Torni
```

Open a new terminal, run `claude` once to sign in, then `torni`.

- **What winget brings:** Windows Terminal, PowerShell 7 and git, if any of them is missing. Torni carries its own .NET runtime.
- **Pull requests:** the GitHub CLI (`gh auth login`) or the Azure CLI (`az extension add --name azure-devops`, `az login`) fills in the PR column. Without them the column stays blank.
- **Upgrading:** it tells you when a newer release is out; `U` shows what changed and upgrades it with winget in a tab of its own. With it closed, `winget upgrade Source191.Torni` does the same.
- **Uninstalling:** `winget uninstall Source191.Torni` removes it and leaves your settings and your Claude Code sessions alone.
- **Without winget:** download `torni_<version>_win-x64.zip` from the [latest release](https://github.com/Source191/torni/releases/latest), check it with `gh attestation verify <zip> --repo Source191/torni`, extract it to a folder of your own and add that folder to your PATH. Windows Terminal, PowerShell 7 and git are then yours to install, and `U` can only show what changed and the release page.
- **Previews:** each preview is a [GitHub prerelease](https://github.com/Source191/torni/releases) with the same zip; extract it to any folder and run `torni.exe` from there. It never replaces the winget install, and previews never reach winget.

Start Torni from an ordinary Windows Terminal tab, not inside a Claude Code session, which turns colour off in every tab Torni opens.

## The console

One Windows Terminal tab with three tabs of its own; Left and Right step between them.

- **Settings row:** Torni's version and memory, and Claude Code's version, with a warning when it is below 2.1.309. A newer release shows here too.
- **Status line:** under the settings row while an action runs or has something to say; ✕ closes it, and a success clears itself after ten seconds.
- **Counts:** beside the tab titles, how many sessions are in each state, and the automated runs (Agent SDK and `claude -p`) with their memory.
- **Detail pane:** under the table, what the selected row does not show.
- **Key bar:** the keys for the selected row; click one to press it.

### The Sessions tab

Every Claude Code session on the machine, whether it was started here or not.

| Column | What it shows |
| --- | --- |
| NAME | The session name given at launch, or the folder name for an unnamed one |
| TAB | A block in the tab's colour while a Torni tab shows the session; ▯ grey for a plain `claude` in its own terminal |
| STATE | ◐ needs input (amber), ● working (cyan), ○ idle (white), ✓ completed (green), ✗ failed (red), ■ stopped (grey), ? unknown (grey) |
| REPOSITORY | The repository it runs in, or the folder's label outside a repository |
| PR | Its pull request: #123 on GitHub (open, draft, merged, closed), !123 on Azure DevOps (active, draft, completed, abandoned) |
| AGE | Time since the session started |

The detail pane shows what the session is waiting for, when it started and was last prompted, its folder, session id, worktree, branch, tab, changes (changed files, unpushed commits, the last commit) and memory (`m` for the breakdown).

### The Worktrees tab

Every git worktree under your repository roots, with or without a session.

| Column | What it shows |
| --- | --- |
| WORKTREE | The owning repository › the folder's slug, or its label when not named after it |
| HEALTH | ✓ ok, ◆ locked (by another tool), △ orphan (a folder git has forgotten), ✗ broken (registration or owner gone), □ empty (a leftover folder), ↗ elsewhere (a worktree of a repository outside the roots) |
| LAST COMMIT | Time since the last commit |
| SESSION | The session running in it, if any |

The detail pane gives the health's reason in full, the branch and whether it is merged into origin's default branch, the pull request, the folder and when Torni created it.

### The Config tab

The settings (see [Configuration](#configuration)), then a Setup heading with what the console reads but does not change: the settings file, the Windows Terminal profiles, Claude Code's terminal bell, the diagnostic log and the last update check. Enter on a setup row says how to change it.

## Sessions

- **Start one:** `n` opens the new-session wizard. Type to filter the repositories and press Enter, type a name, choose where (a new worktree, or the repository's current branch) and, for a worktree, what it branches from, then type an opening prompt if you like. Enter moves between the fields; F5 starts the session and opens it in a coloured tab named after it. With no prompt the session waits at its own prompt.
- **Where it runs:** a new worktree goes to `<worktree root>\<repository>-<name>` on a branch named after the session, so sessions never trip over each other's changes. The gitignored files a `.worktreeinclude` at the repository's root names are copied in, as Claude Code does.
- **Open it:** Enter opens the session in its own tab, or switches to that tab when it is already open.
- **Run several:** each `n` is a new session in its own worktree and tab. They are all in one list, with the ones waiting for you in amber.
- **Close the terminal, keep the sessions:** sessions Torni starts are background sessions under Claude Code's own supervisor, so closing a tab, Torni or Windows Terminal stops nothing. Start Torni again and every session is listed; Enter reattaches.
- **After a reboot:** sessions show as stopped; `R` resumes every stopped or failed session shown, each in its own tab. Filter first with `/` to resume only some.
- **Stop:** `x` stops a session; it stays listed and can be resumed.
- **Rename:** `r` gives a session a new name. It restarts under the new name and keeps its conversation.
- **Delete:** `d` deletes a session, and its worktree too when no work would be lost: never one with uncommitted changes, unpushed commits, a lock or another live session.
- **Adopt:** `b` moves a plain `claude` started in its own tab under Torni's supervision, so it survives the tab closing.
- **Close empty tabs:** `C` closes the tabs of terminal sessions that were never sent a message.
- **From a folder you are already in:** `torni here [name] [--prompt <text>]` starts a supervised session there, on the current branch, in the same tab, and lists it on the Sessions tab. Ctrl+Z detaches; the session keeps running.

## Worktrees

- **Remove:** `d` removes a worktree, refused when it holds work that would be lost. `D` removes it whatever it holds, after you type its name.
- **Prune:** `P` prunes the stale worktree registrations of the row's repository.
- **New session here:** `n` starts a session in the worktree, on the branch checked out there.
- **Shell:** `o` opens a PowerShell tab in the worktree's folder.
- **Open its session:** Enter opens the worktree's session in its own tab, or switches to it.

## Configuration

The Config tab edits the settings, which are kept in `%LOCALAPPDATA%\Torni\config.json` and read again on every scan, so a change applies at once.

- **Repository roots:** the folders searched for git repositories; `~\source\repos` until you change it. Enter edits every root, one per line, and F3 browses for a folder.
- **Worktree root:** where new worktrees go; a `worktrees` folder beside the first root until you change it.
- **Open folder with:** the program `f` opens a row's folder with: VS Code, Visual Studio, Rider or a program of your own (F3 browses for it).
- **Default base:** new worktrees branch from origin's default branch as of your last `git fetch` (Torni never fetches), or from HEAD.
- **Default place:** where the wizard starts a session: a new worktree, or the repository's current branch.
- **Check for updates:** whether to ask the winget community repository once a day for a newer release.

Every row also answers `c` (copy the path) and `f` (open its folder).

## Notifications and profiles

`torni setup-terminal` is the one step that sets up both bells and the profiles:

- **The Torni profile:** a Windows Terminal profile that starts Torni, with its own bell and icon. Choose it from the new-tab menu, or make it Windows Terminal's startup profile to have Torni open every time.
- **The Torni session profile:** the profile every session tab opens with, with a different bell, so you can tell Torni's bell from a session's.
- **Claude Code's terminal bell:** it sets `preferredNotifChannel` to `terminal_bell` in `~\.claude\settings.json`, so a session's tab rings when it finishes or pauses for permission while you are away.
- **Torni's own bell:** the console's tab rings when a background session starts waiting for you, and its row turns amber.

Restart Windows Terminal afterwards. It shows what it will write and asks first; `--sound` and `--session-sound` choose your own `.wav` files, `--yes` writes without asking and `--remove` removes the profiles. There are no toasts and no Claude Code hooks.

## Every key

The key bar shows the keys for the selected row. Left and Right switch tabs on every tab, `<` and `>` scroll a table too wide for the terminal, and `U` (update) and `!` (report a problem) work everywhere.

| Key | Sessions | Worktrees | Config |
| --- | --- | --- | --- |
| Enter | Open in its own tab, or switch to it | Open its session's tab | Edit the setting, or say how to set up |
| n | New session | New session in this worktree | |
| r | Rename (restarts the session) | | |
| x | Stop | | |
| d | Delete, and its worktree if nothing is lost | Remove (refused when work would be lost) | |
| D | | Force remove after typing its name | |
| R | Resume every stopped or failed session shown | | |
| P | | Prune stale registrations | |
| C | Close tabs of unused terminal sessions | | |
| b | Adopt a terminal session | | |
| o | | Open a PowerShell tab there | |
| m | What its memory is made of | | |
| c | Copy the folder path | Copy the folder path | Copy the path |
| f | Open the folder in your editor | Open the folder in your editor | Open the folder |
| s | Open its solution | Open its solution | |
| p | Open its pull request | Open its pull request | |
| / | Filter | Filter | Filter |
| Esc | Clear the filter | Clear the filter, or back to sessions | Clear the filter, or back to sessions |
| ? | Help | Help | Help |
| q | Quit; sessions keep running | Quit | Quit |

## Privacy

It sends nothing but a daily, anonymous check of the winget community repository on GitHub for a newer release, and of that release's changelog (turn it off with Check for updates), and the problem reports you choose to submit. It stores no credential and records nothing personal: its diagnostics are numbers, durations, versions and names of its own, never a path, a prompt or program output.

## Troubleshooting

- **Report a problem:** `!` previews the diagnostics it would send, then opens a GitHub issue with them filled in; nothing is sent until you submit it.
- **After a crash:** `%LOCALAPPDATA%\Torni\last-crash.log` is written; the next problem report includes it.
- **A diagnostic log:** set `TORNI_LOG` to a file path before you start it to keep one.
- **Monochrome tabs:** Torni was started inside a Claude Code session; start it from an ordinary tab.
- **"Workspace not trusted":** Claude Code asks for trust once per repository; Torni opens a tab in that folder so you can accept it, then start the session again.
- **Security:** to report a vulnerability, see [SECURITY.md](../SECURITY.md).
