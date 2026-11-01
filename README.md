# Vantage

![Vantage: mission control for Claude Code sessions, every session in one tab, still there after a reboot. Beside it, the icon: two branches merging into one node, shaped as a V](https://raw.githubusercontent.com/Source191/vantage/main/assets/header/readme-banner.png)

[![Buy me a sliver of a Claude Max plan](https://img.shields.io/badge/Buy_me_a_sliver_of_a-Claude_Max_plan-FFDD00?logo=buymeacoffee&logoColor=black)](https://buymeacoffee.com/simonvane)

Vantage is mission control for Claude Code on Windows: one Windows Terminal tab that shows every session on the machine, so you no longer have to remember which tab is doing what or which one is waiting for you.

- **See every session at once:** each one is listed with its state, repository, branch and pull request.
- **Start in a fresh worktree:** a new session gets its own git worktree, so sessions never trip over each other's changes.
- **Open a session in its own tab:** Enter on any background session opens it in a tab of its own, named after the session and coloured so you can tell it apart at a glance, or switches to that tab when it is already open.
- **Hear when one needs you:** the terminal bell rings when a session finishes or pauses for you.
- **Keep working when the terminal closes:** sessions run under Claude Code's own background supervisor, so they carry on when no terminal shows them and are still listed after a reboot, ready to resume with one key.
- **Tidy up with one key:** when the work is done, delete a session and its worktree together, unless that would lose uncommitted or unpushed work.

I built Vantage because I wanted a tool that worked the way I work. Before starting it I tried [Orca](https://github.com/stablyai/orca) and [Kepler](https://gitkraken.com/kepler), and there is a lot I like about both of them. But I live in the terminal and I want to stay in the terminal, and both of them also do a great deal that I was not interested in. Perhaps my mind will change over time; for now I wanted something that fits how I use Claude Code and nothing more. By all means try the others, but give Vantage a go too 😊

> **To do:** add a screenshot of the sessions view here before the repository is made public.

## Contents

- [Prerequisites](#prerequisites)
- [Install](#install)
- [Start Vantage](#start-vantage)
- [The Sessions tab](#the-sessions-tab)
- [The new session wizard](#the-new-session-wizard)
- [Session tabs](#session-tabs)
- [The Worktrees tab](#the-worktrees-tab)
- [The Config tab](#the-config-tab)
- [The detail pane](#the-detail-pane)
- [Pull requests](#pull-requests)
- [vantage here](#vantage-here)
- [Notifications and Windows Terminal setup](#notifications-and-windows-terminal-setup)
- [Common scenarios](#common-scenarios)
- [Updates](#updates)
- [Reporting a problem](#reporting-a-problem)
- [Where Vantage keeps things](#where-vantage-keeps-things)
- [Security and privacy](#security-and-privacy)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [Support Vantage](#support-vantage)
- [Roadmap and known limits](#roadmap-and-known-limits)

## Prerequisites

Vantage runs on Windows 11. `winget`, which comes with Windows 11, installs everything it needs except Claude Code; skip a line for a tool you already have.

```powershell
winget install --id Microsoft.WindowsTerminal --exact --source winget   # already part of Windows 11 22H2 and later
winget install --id Microsoft.PowerShell --exact --source winget
winget install --id Microsoft.DotNet.SDK.10 --exact --source winget
winget install --id Git.Git --exact --source winget
irm https://claude.ai/install.ps1 | iex   # Claude Code
[Environment]::SetEnvironmentVariable('Path', [Environment]::GetEnvironmentVariable('Path', 'User') + ";$HOME\.local\bin", 'User')   # puts claude on the PATH
```

`--source winget` keeps winget away from the Microsoft Store source, which fails on some machines and networks.

- **Windows Terminal:** Vantage opens, titles and colours session tabs in it, and relies on its bell.
- **PowerShell 7:** every tab Vantage opens runs `pwsh`.
- **.NET 10 SDK:** `dotnet tool install` needs the SDK, not only the runtime.
- **git:** branches and worktrees.
- **Claude Code 2.1.309 or later:** the native installer above puts `claude.exe` in `~\.local\bin` and keeps it up to date; the line after it puts that folder on the PATH. WinGet's `Anthropic.ClaudeCode` does not update itself, so check its version if you use it. Run `claude` once to sign in. An npm install's `claude.cmd` on its own is refused, because Windows would run it through `cmd.exe`; install the native build instead.

Optional, for the PR column (see [Pull requests](#pull-requests)):

```powershell
winget install --id GitHub.cli --exact --source winget
winget install --id Microsoft.AzureCLI --exact --source winget
```

Open a new terminal so the PATH changes apply, then check:

```powershell
pwsh --version; dotnet --list-sdks; git --version; claude --version
```

## Install

1. Install the tool from nuget.org:

   ```powershell
   dotnet tool install -g Source191.Vantage
   ```

   Update it later with `U` inside Vantage (see [Updates](#updates)) or `dotnet tool update -g Source191.Vantage`. A prerelease (such as `0.20.0-preview.8`) installs only with `--prerelease` or `--version <version>`, and never checks for updates itself.

2. Set up the terminal bell (optional but recommended): run `vantage setup-terminal`; see [Notifications and Windows Terminal setup](#notifications-and-windows-terminal-setup).

3. Add profile shortcuts (optional). These two lines in `$PROFILE` give `v` for `vantage` and `vh` for `vantage here`:

   ```powershell
   Set-Alias v -Value vantage
   function vh { vantage here @args }
   ```

A shell that was open before the install needs restarting to find `vantage`.

## Start Vantage

Open a tab and run `vantage` (or `v`). The screen has four parts, top to bottom:

- **Settings row:** the versions, `Vantage 0.18.0 (35 MB) · Claude 2.1.310`, with Vantage's memory in brackets. When a newer release is out it reads `Vantage 0.18.0 (35 MB; 0.19.0 available, U to update)`, and a Claude Code older than Vantage needs reads `2.1.302 (below 2.1.309, update Claude Code)`.
- **Status line:** shown only while there is something to say, with a green ✓ for what a key did, an orange ! for a refusal or a partial result, and a red ✗ for a failure. A ✓ goes by itself after ten seconds; otherwise the message goes when you move the selection, change the filter or click the ✕ after it. A spinner shows while an action runs.
- **Tabs and table:** `Sessions`, `Worktrees` and `Config`, with counts for the tab shown (`◐ 2 need input · ● 1 working · ■ 1 stopped`) and the table's title (`7 sessions, selected: fix-123`). Under the Sessions and Worktrees tables, the [detail pane](#the-detail-pane) shows the selected row in full. Rest the mouse on a column header or a key to see what it means.
- **Key bar:** the last two rows name the keys that apply to the selected row; click a key to press it.

Keys that work on every tab:

| Key | Action |
| --- | --- |
| 1, 2, 3 | Show the Sessions, Worktrees or Config tab |
| Left, Right | Show the tab before or after this one |
| Up, Down | Move the selection |
| <, > | Scroll a table too wide for the terminal, one column at a time |
| / | Filter the rows; Enter keeps the filter, Esc clears it |
| ? | Help: every key, every column and the detail pane's lines for this tab |
| U | Update Vantage when a newer release is known (see [Updates](#updates)) |
| ! | Report a problem (see [Reporting a problem](#reporting-a-problem)) |
| q | Quit. Quitting Vantage never stops a session |

The sessions table refreshes every five seconds. Vantage names its own tab `Vantage` and colours it green, so it is easy to find among the session tabs, and puts the title and colour back when you quit (a profile that sets its own `tabColor` keeps it).

`vantage --help` lists the commands and options and `vantage --version` prints the version. `vantage` exits 0 when you quit, and 1 when Claude Code or git cannot be found or the command line cannot be read.

## The Sessions tab

Each row is a Claude Code session, ordered so that the ones needing attention come first:

| Column | Meaning |
| --- | --- |
| NAME | The session name given at launch, or the folder name for an unnamed one (cut at 28 characters) |
| TAB | A block in the colour of the Vantage tab showing the session; a grey `▯` for a session started as plain `claude` in a tab of its own; blank otherwise |
| STATE | `◐ needs input` (amber), `● working` (cyan), `● starting` (cyan, just started or resumed), `○ idle` (white), `✓ completed` (green), `✗ failed` (red), `■ stopped` (grey); for a state Vantage does not know, a grey `?` and Claude Code's own words |
| REPOSITORY | The repository the session runs in, such as `Northwind.Orders`, whether in its main checkout or one of its worktrees; for a folder outside any repository, a short label such as `~\scratch\notes` |
| PR | The branch's pull request: `#123 open` on GitHub, `!123 active` on Azure DevOps; blank when there is none or it cannot be looked up, and `p` says why |
| AGE | Time since the session started |

| Key | Action |
| --- | --- |
| Enter | Open the session in its own coloured Windows Terminal tab, or switch to that tab when it is open |
| n | New session wizard |
| r | Rename the session. It restarts under the new name with the same conversation, so it is refused while the session is working or before its first message; its tab is reopened under the new name in the same colour |
| x | Stop the session; it stays listed and can be resumed. Its Vantage tab closes |
| d | Delete the session after a confirmation, closing its tab, and remove the worktree Vantage created for it when nothing in it would be lost (asking before deleting an unmerged branch). On a plain `claude` session that has never been sent a message, close its tab instead |
| R | Resume every stopped or failed session the table shows, each in its own tab; filter with `/` first to resume fewer |
| C | Close every tab whose plain `claude` has never been sent a message, after a confirmation naming them |
| b | Move a plain `claude` session under Vantage's supervision; its old tab closes once the session has resumed |
| m | Show what the session's memory is made of: Claude Code itself and each program it started (MCP servers, shells), largest first |
| c | Copy the row's folder path |
| f | Open the row's folder with the program chosen under "Open folder with" on the [Config tab](#the-config-tab); when that program cannot be found or Windows will not start it, the status line says so and where to choose another |
| s | Open the folder's solution (`*.sln` or `*.slnx`, in the folder or its `src` folder) with Windows' default application; with several, the one named after the repository (`.slnx` before `.sln`), else ask which; when Windows has no application for the type, the status line says so and nothing opens |
| p | Open the row's pull request in your browser, or say why there is none |

The key bar shows what applies to the selected row: a running background session has `Enter Open  x Stop  d Delete  r Rename  m Memory`; a stopped or failed one `Enter Open  d Delete  r Rename`; a plain `claude` session `b Adopt  m Memory`, with `d Close tab` while it has never been sent a message. `R Resume`, `C Close empty`, `n New session` and `U Update` follow when they apply; the second row is `c Copy path  f Open folder  s Open solution  p Open PR  / Filter  ? Help  ! Report  q Quit`.

**Plain `claude` sessions.** A row with the `▯` swatch is a session someone started by typing `claude` in a tab. Vantage can see it but cannot open, stop or rename it; press `b` to bring it under supervision, or type `/bg` inside it. One that has never been sent a message has nothing to keep: `d` closes its tab and `C` closes all such tabs.

**Automated runs.** A `claude` started by a program rather than by you (a plugin's background review, a script using the Agent SDK, or `claude -p`) is not a row: it cannot be opened or adopted and ends by itself. While any run, the counts end with `◇`, how many there are and the memory they use. They do call the model, so they cost tokens.

## The new session wizard

Press `n`. Your repositories are listed first (the ten most recent, then everything under the repository roots); type to filter them, use the arrows to choose, and Enter moves to the form. Tab and Enter move down the form: name, Where, worktree base, prompt.

- **Session name (required):** shown in the NAME column; it also names the worktree folder and branch when you choose a worktree.
- **Where:** `New worktree` (the default, unless you change the Default place on the [Config tab](#the-config-tab)) creates `<repository>-<name>` under the worktree root on branch `<name>`, copying the git-ignored files named in the repository's `.worktreeinclude`; `Current branch` runs the session in the repository as it is. The arrows change the choice.
- **Worktree base:** what the new branch starts from: origin's default branch as of your last `git fetch` (Vantage does not fetch first), or the commit checked out now (HEAD).
- **Opening prompt:** sent as soon as the session starts. Leave it blank and the session waits at its prompt. A prompt that starts with `-` is refused, because Claude Code would read it as options.

A cyan line says exactly what will happen ("Worktree Northwind.Orders-fix-tests under C:\Users\you\source\worktrees, on branch fix-tests."). **F5** starts the session and opens it in its own tab; **Esc** cancels. Enter never starts a session, so there is nothing to undo if you press it too soon.

If Claude Code has not trusted the repository yet, the start is refused and Vantage opens a tab running `claude` there: accept the trust prompt, exit, and press `n` again. For a folder that is not a repository, run [`vantage here`](#vantage-here) there instead.

## Session tabs

Enter opens a new Windows Terminal tab titled with the session name, in a colour from a palette chosen so that neighbouring tabs stay distinguishable with red-green colour blindness too, and attaches the session there; Vantage stays in its own tab. The TAB column shows a block in that colour. While the tab is open, Enter switches to it instead of opening another, in whichever window it is.

Ctrl+Z in the tab detaches and closes it; the session keeps running. Stopping, renaming or deleting the session closes its tab too. Close a tab by hand and the swatch clears within a few seconds.

## The Worktrees tab

Press `2`. Vantage lists every linked worktree of the repositories under your roots, wherever its folder is, plus the leftover folders git has forgotten under the roots and the worktree root. The scan starts in the background as Vantage opens and refreshes while the tab is shown; a large roots folder can take a while the first time.

```text
Vantage 0.18.0 (35 MB) · Claude 2.1.310
╭────────╭─────────╮──────╮
│Sessions│Worktrees│Config│  3 ok · 1 broken · 1 with changes · 2 with unpushed commits  4 worktrees, selected: Northwind.Bi…
├────────╯         ╰──────┴────────────────────────────────────────────────────────────────────────────────────────────╮
││WORKTREE                                                                          │HEALTH     │LAST COMMIT│SESSION    ││
│ Northwind.Billing › invoice-pdf                                                    ✓ ok          3d4h       invoice-pdf │
│ Northwind.Billing › study                                                          ✓ ok          9d16h                  │
│ Northwind.Orders › fix-tests                                                       ✓ ok          1d0h       fix-tests   │
│ Northwind.Sales › republish                                                        ✗ broken      -                      │
```

| Column | Meaning |
| --- | --- |
| WORKTREE | The owning repository, then `›` and the worktree's slug (`Northwind.Billing › invoice-pdf` for the folder `Northwind.Billing-invoice-pdf`), or the folder's label when it is not named after the repository |
| HEALTH | `✓ ok`; `◆ locked` by another tool; `△ orphan`, a folder under `.claude\worktrees` git no longer lists; `✗ broken`, a registration whose folder or `.git` file has gone, or a folder whose registration or repository has gone; `□ empty`, a leftover folder with nothing in it; `↗ elsewhere`, a checkout of a repository outside your roots. The detail pane gives the reason in full |
| LAST COMMIT | Age of the last commit |
| SESSION | The session running in it, if any |

| Key | Action |
| --- | --- |
| d | Remove the worktree, or delete a leftover folder. Refused, with the reason, when it has changed files, unpushed commits, a lock or a live session, and always for an `elsewhere` worktree (add its repository's root on the Config tab, or remove it with git). A folder a program still has open is reported as in use; press `d` again once it is closed |
| D | Force remove: type the folder's name to confirm (Copy name, or F2, copies it for you). Never forces a locked worktree, one with a live session, or an `elsewhere` one |
| P | Prune stale registrations in the row's repository |
| n | Start a session in this worktree, on the branch checked out there: type a name, then Enter or F5 |
| o | Open a PowerShell tab in the worktree |
| Enter | Open the session running in the worktree in its own tab, or switch to that tab |
| Esc | Back to the Sessions tab (clears a filter first) |
| c, f, s, p | Copy the folder path, open the folder, open its solution, open its pull request, as on the Sessions tab |

When a removed worktree's branch was created by Vantage and is merged, the branch is deleted too; when it is not merged, or you created it, Vantage asks first. Junctions and links inside a leftover folder are removed as links and never followed, and a leftover with 1,000 files or more is refused.

## The Config tab

Press `3`. Each setting has a row; below them, under a Setup heading, is the setup Vantage reads but does not change. Rest the mouse on a row to see what it is for.

```text
Vantage 0.18.0 (35 MB) · Claude 2.1.310
╭────────╭─────────╭──────╮
│Sessions│Worktrees│Config│  7 settings · 1 to set up                         12 rows, selected: Repository roots
├────────┴─────────╯      ╰───────────────────────────────────────────────────────────────────────────────────╮
││SETTING                  │VALUE                                                                             ││
│ Repository roots          C:\Users\you\source\repos                                                        │
│                           D:\work                                                                          │
│ Worktree root             C:\Users\you\source\worktrees                                                    │
│ Open folder with          VS Code                                                                          │
│ Default base              origin's default branch, as last fetched                                         │
│ Default place             New worktree                                                                     │
│ Check for updates         on                                                                               │
│ Setup                                                                                                      │
│ Settings file             C:\Users\you\AppData\Local\Vantage\config.json                                   │
│ Windows Terminal profiles not set up, run vantage setup-terminal                                           │
│ Claude bell               on                                                                               │
│ Diagnostic log            off                                                                              │
│ Last update check         09:12 today, newest 0.18.0                                                       │
```

- **Repository roots:** the folders whose repositories the wizard offers and the Worktrees tab scans. Enter opens every root, one per line: add or delete lines, or Browse (F3) to pick folders, then F5 saves. Each must be a full path that exists, listed once, and at least one must stay.
- **Worktree root:** where new worktrees go, as `<worktree root>\<repository>-<name>`; `~\source\worktrees` unless you change it. Browse (F3) picks a folder.
- **Open folder with:** what `f` uses: VS Code, Visual Studio (the newest install), Rider (the newest install under Program Files or from JetBrains Toolbox, including Toolbox 2's folders under `%LOCALAPPDATA%\Programs`), Explorer, or Custom: the full path of an executable and its arguments, with `{folder}` where the folder goes. A bare name, a relative path, a `.cmd` or `.bat` file, or a file that does not exist is refused; while Custom is chosen, Browse (F3) picks the executable. Once the dialog has looked, it marks each preset it cannot find, such as `Rider (not found)`; you can still choose it, ready for when it is installed.
- **Default base:** where a new worktree's branch starts: origin's default branch as last fetched, or HEAD.
- **Default place:** where `n` starts a session: `New worktree` or `Current branch`. `n` on a worktree and `vantage here` always use the folder's current branch.
- **Check for updates:** whether Vantage asks nuget.org once a day for a newer release; Enter turns it on or off.
- **Setup rows:** where `config.json` is; whether `vantage setup-terminal` has added the Windows Terminal profiles (`installed` and their file) and set Claude's bell (or which channel it uses instead); the `VANTAGE_LOG` file, if set; and when nuget.org was last asked (`never`, or `off` while the check is off). Enter on one says how to change it.

Enter on a setting opens a small dialog for that one value: F5 checks and saves it, and the next scan, wizard and `f` use it at once; Esc changes nothing. `c` copies the selected path and `f` opens the selected folder, or the folder holding the selected file.

`config.json` can also be edited by hand while Vantage is closed; a property it leaves out reads as its default:

```json
{
  "repositoryRoots": ["C:\\Users\\you\\source\\repos", "D:\\work"],
  "worktreeRoot": "C:\\Users\\you\\source\\worktrees",
  "editor": { "preset": "Custom", "executable": "C:\\Tools\\subl.exe", "arguments": ["-n", "{folder}"] },
  "defaultBase": "RemoteDefault",
  "defaultPlace": "NewWorktree",
  "checkForUpdates": true
}
```

## The detail pane

Under the Sessions and Worktrees tables a pane shows what the selected row does not. It keeps a fixed height, so the table does not jump, and is read for the selected row only, so the table never waits for it; a fact that cannot be read says unavailable.

For a session:

```text
─┤ fix-123 ├──────────────────────────────────────────────────────────────────────────────────────────────────────────
 Waiting  permission prompt  Started 14:02 (5m ago)                         Prompted 14:03 (4m ago)
 Folder   C:\Users\me\source\worktrees\Northwind.Orders-fix-123            Session  81cdb0f9-0000-4000-8000-000000000001
 Worktree fix-123            Branch  fix-123                                Tab      open, yellow
 Changes  3 changed files · 0 unpushed commits · last commit 2h00m ago      Memory   903 MB (m for the breakdown)
```

- **Waiting:** what the session waits for (a permission prompt, a question, a first prompt), or `nothing`.
- **Started, Prompted:** when it started, and when Vantage last saw it go from waiting or idle to working.
- **Folder, Session:** the full folder and the session id.
- **Worktree, Branch, Tab:** the worktree's slug, `main checkout` or `not in a repository`; the branch, or `none` when detached; the Vantage tab and its colour, `own terminal` for a plain `claude` session, or `none`.
- **Changes, Memory:** changed files, unpushed commits and the last commit; the memory the session and everything it started use, as Task Manager counts it.

For a worktree:

```text
─┤ Northwind.Billing-invoice-pdf ├────────────────────────────────────────────────────────────────────────────────────
 Health  ok
 Branch  invoice-pdf, not merged into origin/main                          PR      !4521 draft (p opens it)
 Folder  C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf      Created by Vantage, 23 Oct 09:14
 Changes 0 changed files · 12 unpushed commits
 Unpushed 3f9c2a1  2h  Fix the invoice total
          8b1e04d  5h  Add the PDF renderer
          … 10 more
```

The health and its reason, the branch and whether it is merged into origin's default branch, the pull request, the folder, whether Vantage created it, and the changed files and unpushed commits; then up to five unpushed commits, or the changed files when nothing is unpushed.

## Pull requests

The Sessions tab's PR column, and the PR line of a worktree's detail pane, show the pull request for the branch, read from the repository's `origin` remote:

- **GitHub** (`github.com`): through the GitHub CLI. Run `gh auth login` once.
- **Azure DevOps** (`dev.azure.com` or `*.visualstudio.com`): through the Azure CLI. Run `az extension add --name azure-devops` and `az login` once.
- **Any other host:** left blank.

An open (or draft, or active) pull request is shown before a closed one; otherwise the most recent. Each listed session's and worktree's branch is looked up in the background, so the rows never wait and a change shows within a minute or two. Vantage never signs in for you and stores no credential. On Azure DevOps a branch whose name holds anything but letters, digits, dots, underscores, hyphens and slashes is not looked up, because the Azure CLI is a batch file that would read such characters as commands; `p` says so.

## vantage here

From any folder, `vantage here` starts a supervised session in that folder, on its current branch, and attaches in the same tab, so a session started from an ordinary terminal is managed by Vantage from the outset. The session is named after the folder unless you give a name.

```powershell
vantage here
vantage here fix-tests -p "Make the failing tests in OrdersShould pass"
vh fix-tests          # with the profile shortcut
```

`-p` (or `--prompt`) sends an opening prompt; without one the session waits at its prompt. Ctrl+Z detaches and returns to the shell; the session keeps running and is listed in Vantage. Exit codes: 0 after a detach; 1 when the start failed (an untrusted folder, for example), Claude Code or git was not found, or the command line could not be read; 2 when the name or the prompt is refused. A name that starts with `-` needs `--` before it: `vantage here -- -experiment`.

## Notifications and Windows Terminal setup

Two things ring the terminal bell, and Windows Terminal flags the tab, window or taskbar:

- **Claude Code**, with `"preferredNotifChannel": "terminal_bell"` in `~\.claude\settings.json`, rings in the tab a session is attached in when it finishes a task or pauses for a permission prompt while you appear to be away.
- **Vantage** rings in its own tab when a background session starts needing input, so a session with no tab of its own still gets your attention.

`vantage setup-terminal` makes the bells distinct and turns Claude's on:

- **Profiles:** it adds two Windows Terminal profiles: **Vantage**, which runs Vantage in a tab that closes when you quit, and **Vantage session**, which every tab Vantage opens then uses, so a session's bell sounds different from Vantage's own.
  - **Icon and alerts:** both carry the Vantage icon and flash the window and taskbar.
  - **Where they live:** they are written as a Windows Terminal fragment, a small JSON file of its own, and your `settings.json` is never touched.
  - **After running it:** restart Windows Terminal. Run it again after an update to refresh the icon.
- **Claude Code's bell:** it then offers to set `preferredNotifChannel` in `~\.claude\settings.json`, keeping every other setting.

```powershell
vantage setup-terminal                                   # show what will be written, then ask
vantage setup-terminal --yes                             # no questions
vantage setup-terminal --sound "C:\Windows\Media\chimes.wav" --session-sound "C:\Windows\Media\Ring01.wav"
vantage setup-terminal --remove                          # delete the profiles again
```

The default sounds are `Windows Notify Messaging.wav` and `Windows Proximity Notification.wav` from `C:\Windows\Media`; a sound you name must be an existing `.wav` file. Exit codes: 0 done, declined or nothing to remove; 1 a write failed or the command line could not be read; 2 a missing or non-wav sound, or `--remove` with a sound. `--remove` leaves Claude's bell alone; remove that line from `~\.claude\settings.json` yourself if you want it off.

To set a bell by hand instead: Windows Terminal, Settings, a profile, Advanced: "Bell notification style" (Audible, Window and Taskbar) and "Bell sound".

## Common scenarios

**Start work on a story in a clean worktree.** `n`, choose the repository, name the session after the story, type the opening prompt, F5. The session opens in its own coloured tab and Vantage stays where it is.

**Kick off several sessions and let them run.** `n` and F5 for each, with distinct names. Leave Vantage open: rows turn amber when one needs you and the Vantage tab rings even when another tab is in front. Enter on the amber row takes you to its tab; Ctrl+Z closes the tab when you are done.

**Start Claude from a folder you are already in.** `vh fix-tests -p "..."` instead of `claude`, so Enter, `x` and `d` work on it later.

**Bring a plain `claude` session under Vantage.** Select its row (the `▯` swatch) and press `b` while it is idle or waiting. Vantage ends the plain process and resumes the same conversation under the supervisor with the same name, and its old tab closes. A session never sent a message has nothing to resume: `d` closes its tab instead.

**After a reboot.** Sessions show as stopped or failed. `R` resumes every one the table shows, each in its own tab; filter with `/` first to resume fewer, press Enter on a row to resume just that one, and `d` deletes one you do not want back.

**Finish a story and clean up.** `d` on the session deletes it and removes the worktree Vantage created, with its branch if merged; anything with uncommitted or unpushed work is kept, with the reason.

**Find out what is cluttering the repositories folder.** `2` lists every worktree with its health. Empty leftovers go with `d`; a worktree with uncommitted work needs `D` and its name typed, on purpose.

**Something needs Claude Code's own screen.** Run `claude agents` in any shell for the native agent view.

## Updates

A dotnet global tool never updates itself, so Vantage looks for you: once a day at most, in the background as it opens, it asks nuget.org for the newest release of `Source191.Vantage`, and the settings row says when there is one. A slow or failed check shows nothing and delays nothing. Prerelease versions are not offered, and a prerelease build never checks.

`U` asks "Quit Vantage and update to 0.19.0?". On yes, Vantage opens a tab titled "Update Vantage" and quits; the tab waits for it to exit, runs `dotnet tool update -g Source191.Vantage` against nuget.org and starts Vantage again. If the update fails, usually because another Vantage is still open, the tab stays open with dotnet's message: quit every Vantage and try again. Background sessions keep running throughout.

The check is one anonymous HTTPS request for the public list of the package's versions, with a `Vantage/<version>` user agent and nothing else that identifies you. Turn it off with Check for updates on the [Config tab](#the-config-tab), or set the environment variable `VANTAGE_NO_UPDATE_CHECK` to any value.

## Reporting a problem

Press `!` on any tab. A dialog shows a title box and, read-only, exactly the diagnostics the report will carry: the Vantage, Claude Code, Windows and .NET versions, the terminal's size, a few facts about your settings (the editor preset, whether update checks are on, how many roots), Vantage's recent events (see [Security and privacy](#security-and-privacy)) and, after a crash, the crash. It holds no names, paths or code.

- **Open in browser** (or Enter) opens a "Problem report" issue on GitHub with the title and diagnostics filled in. Describe the problem there, change anything you like, and submit it from your own account. Vantage sends nothing itself, but the page's address carries the diagnostics to GitHub, so read the preview first; nothing is public until you submit. A very long log loses its oldest ordinary lines first.
- **Copy text** puts the same text on the clipboard instead.

## Where Vantage keeps things

Everything Vantage owns is under `%LOCALAPPDATA%\Vantage`:

| File or folder | Contents |
| --- | --- |
| `config.json` | Your settings (see [The Config tab](#the-config-tab)) |
| `update-check.json` | When Vantage last asked nuget.org, and the newest release it found |
| `recent\` | The repositories you used most recently, one file each, so the wizard can list them first |
| `created.json` | The worktrees Vantage created, which decides whether their branches are deleted with them |
| `tabs\` | One file per tab Vantage opened, so Enter switches to it instead of opening another |
| `last-crash.log` | Written only when an exception ends Vantage, and removed by the next problem report |

`vantage setup-terminal` also writes `%LOCALAPPDATA%\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json` and can set one line in `~\.claude\settings.json`. A missing or corrupt file is treated as empty. Sessions themselves belong to Claude Code, never to Vantage.

## Security and privacy

- **What runs:** Claude Code, `git`, `gh`, `az`, `wt.exe` and `pwsh.exe`, each found once as Vantage starts and run by its full path, so a program planted in the folder you started Vantage from is never run. A batch file is never given a session name, a prompt or a path.
- **What it trusts:** every repository under your roots. Vantage runs git in them, copies the files their `.worktreeinclude` names, and opens their solutions, so keep a repository you would not open in an editor out of the roots.
- **What it deletes:** a worktree only through git and only when nothing would be lost, unless you force it by typing its name; a leftover folder only inside your roots or the worktree root, and never one that is still a live checkout.
- **What it records:** a diagnostic log in memory (the latest 500 events: versions, which views, dialogs and keys were used and how they went, failures as exception types and Vantage's stack frames, and counts of the programs it ran). It never holds a path, a repository, branch or session name, a prompt, a file name, program output, your user or machine name, or anything you typed. Set `VANTAGE_LOG` to a file path to have the same lines written there; a crash writes them to `last-crash.log`.
- **What it sends:** nothing, except the daily update check (see [Updates](#updates)) and the report you choose to submit. It stores no credential and never edits Claude Code's trust records.

To report a vulnerability, see [SECURITY.md](https://github.com/Source191/vantage/blob/main/SECURITY.md).

## Troubleshooting

- **Something went wrong.** Press `!` to [report it](#reporting-a-problem) with the diagnostics filled in. To keep a log as it happens, set `$env:VANTAGE_LOG = "$env:TEMP\vantage.log"` before starting Vantage.
- **"not trusted yet".** Claude Code refuses to start in a folder you have not trusted. Vantage opens a tab running `claude` there; accept the prompt, exit, and start again. Vantage never edits the trust records.
- **No bell when a session needs you.** Run `vantage setup-terminal` (see [Notifications and Windows Terminal setup](#notifications-and-windows-terminal-setup)). Claude only rings when it thinks you are away from the terminal.
- **Tabs opened from Vantage have no colours.** Vantage was started from inside a Claude Code session, whose environment turns colour off; start Vantage from an ordinary tab.
- **The Worktrees tab is slow to fill.** The first scan runs git in every repository under the roots; narrow the repository roots if the folder is very large.
- **Claude Code was not found.** Vantage looks for `~\.local\bin\claude.exe`, then `claude.exe` on the PATH.
- **Claude Code was found only as a batch file.** Install the native build with `irm https://claude.ai/install.ps1 | iex`.
- **git was not found.** Vantage needs `git.exe` on the PATH (`winget install --id Git.Git --exact --source winget`, then a new terminal).
- **Windows Terminal or PowerShell 7 was not found.** The key that needs it says so; `wt.exe` and `pwsh.exe` must be on the PATH.

## Contributing

Vantage welcomes contributions. [CONTRIBUTING.md](https://github.com/Source191/vantage/blob/main/CONTRIBUTING.md) explains how to propose a change, build and test Vantage, install your own build, and take a change through OpenSpec to a release.

## Support Vantage

Vantage is built in the gaps between rate limits. Help close the gaps: [buy me a sliver of a Claude Max plan](https://buymeacoffee.com/simonvane).

![QR code for buymeacoffee.com/simonvane](https://raw.githubusercontent.com/Source191/vantage/main/assets/support/buy-me-a-coffee-qr.png)

## Roadmap and known limits

- Windows only, with Windows Terminal.
- Still to observe at the terminal: whether attaching to one session from two tabs is allowed, and the best way to bring sessions back after a reboot.
- A take-over key (stop a supervised session and continue it as a normal foreground `claude`) is undecided; `b` covers the other direction.
- Multiple Claude accounts, through Claude Code's `CLAUDE_CONFIG_DIR`.
- Installing with `winget install`, as a self-contained `vantage.exe` that needs no .NET SDK ([#93](https://github.com/Source191/vantage/issues/93)).
- A quicker worktree scan for a large roots folder.
- Pinning sessions, once Claude Code exposes it; a configurable palette and profile for the tabs Vantage opens.
