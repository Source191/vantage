# Vantage

![Vantage: mission control for Claude Code sessions, every session in one tab, still there after a reboot. Beside it, the icon: two branches merging into one node, shaped as a V](https://raw.githubusercontent.com/Source191/vantage/main/assets/header/readme-banner.png)

[![Buy me a sliver of a Claude Max plan](https://img.shields.io/badge/Buy_me_a_sliver_of_a-Claude_Max_plan-FFDD00?logo=buymeacoffee&logoColor=black)](https://buymeacoffee.com/simonvane)

Vantage is mission control for Claude Code on Windows: one Windows Terminal tab that shows every session on the machine, so you no longer have to remember which tab is doing what or which one is waiting for you.

- **See every session at once:** each one is listed with its state, repository, branch and pull request.
- **Start in a fresh worktree:** a new session gets its own git worktree, so sessions never trip over each other's changes.
- **Open a session in its own tab:** Enter on any background session in the list opens it in a tab of its own, named after the session and coloured so you can tell it apart at a glance, or switches to that tab when it is already open.
- **Hear when one needs you:** the terminal bell rings when a session finishes or pauses for you.
- **Keep working when the terminal closes:** sessions run under Claude Code's own background supervisor, so they carry on when no terminal shows them and are still listed after a reboot, ready to resume with one key.
- **Tidy up with one key:** when the work is done, delete a session and its worktree together, unless that would lose uncommitted or unpushed work.

I built Vantage because I wanted a tool that worked the way I work. Before starting it I tried [Orca](https://github.com/stablyai/orca) and [Kepler](https://gitkraken.com/kepler), and there is a lot I like about both of them. But I live in the terminal and I want to stay in the terminal, and both of them also do a great deal that I was not interested in. Perhaps my mind will change over time; for now I wanted something that fits how I use Claude Code and nothing more. By all means try the others, but give Vantage a go too 😊

> **To do:** add a screenshot of the sessions view here before the repository is made public.

## Contents

- [Prerequisites](#prerequisites)
- [Install](#install)
- [Start Vantage](#start-vantage)
- [The sessions view](#the-sessions-view)
- [The new session wizard](#the-new-session-wizard)
- [Session tabs](#session-tabs)
- [The worktrees view](#the-worktrees-view)
- [The detail pane](#the-detail-pane)
- [Pull requests](#pull-requests)
- [vantage here](#vantage-here)
- [Notifications](#notifications)
- [Windows Terminal setup](#windows-terminal-setup)
- [Common scenarios](#common-scenarios)
- [Where Vantage keeps things](#where-vantage-keeps-things)
- [What Vantage trusts](#what-vantage-trusts)
- [The Config tab](#the-config-tab)
- [Updates](#updates)
- [What Vantage records](#what-vantage-records)
- [Reporting a problem](#reporting-a-problem)
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

`--source winget` keeps winget away from the Microsoft Store source, which fails on some machines and networks and then stops winget choosing a package at all.

- **Windows Terminal:** Vantage opens, titles and colours session tabs in it, and relies on its bell.
- **PowerShell 7:** every tab Vantage opens runs `pwsh`.
- **.NET 10 SDK:** `dotnet tool install` needs the SDK, not only the runtime.
- **git:** branches and worktrees.
- **Claude Code 2.1.309 or later:** the native installer above puts `claude.exe` in `~\.local\bin`, one of the two places Vantage looks (the other is the PATH), and keeps it up to date in the background. It does not add that folder to the PATH, which the line after it does, so `claude` works in a new terminal. WinGet has it too (`Anthropic.ClaudeCode`), but that package does not update itself and can lag behind the version Vantage needs; if you use it, run `winget upgrade Anthropic.ClaudeCode` and check the version. Run `claude` once to sign in. Vantage runs Claude Code only as an executable: an npm install's `claude.cmd` shim on its own is refused and named, because Windows would run it through `cmd.exe`, which reads session names and prompts as commands (the npm package now installs the same native binary, so `~\.local\bin\claude.exe` is usually there too).

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

Install the [prerequisites](#prerequisites) first.

1. Install the tool from nuget.org:

   ```powershell
   dotnet tool install -g Source191.Vantage
   ```

   Update it later with `dotnet tool update -g Source191.Vantage`. A prerelease (a version such as `0.16.2-preview.1`) installs only when asked for, with `--prerelease` or `--version <version>`; it never checks for updates itself (see [Updates](#updates)), so update it with `dotnet tool update -g Source191.Vantage --prerelease`. To try a build that is not on nuget.org yet, install a GitHub Release's `.nupkg` instead: download it into a folder, then `dotnet tool install -g Source191.Vantage --add-source <folder> --version <version>`.

2. Set up the terminal bell (optional but recommended): run `vantage setup-terminal`. It adds two Windows Terminal profiles with their own bell sounds and can turn on Claude Code's terminal bell; see [Windows Terminal setup](#windows-terminal-setup).

3. Add the profile shortcuts (optional). These two lines in `$PROFILE` give `v` for `vantage` and `vh` for `vantage here`:

   ```powershell
   Set-Alias v -Value vantage
   function vh { vantage here @args }
   ```

New shells pick up `~\.dotnet\tools` on the PATH; a shell that was open before the install needs restarting.

## Start Vantage

Open a tab and run `vantage` (or `v`). The screen has four parts, top to bottom:

- **Settings:** one row, labels in orange and values in white: `Vantage 0.18.0 (35 MB) · Claude 2.1.310`. In a terminal that draws images, such as Windows Terminal, the Vantage icon sits to its left; elsewhere the row starts at the left edge. The Claude Code version reads `unknown` when `claude --version` cannot be read and `2.1.302 (below 2.1.309, update Claude Code)` when it is older than the minimum Vantage supports; a local install shows its prerelease label without the build timestamp (`0.6.0-local`); a newer release adds `0.19.0 available, U to update`. The repository roots and the worktree root are on the [Config tab](#the-config-tab).
- **Tabs:** `Sessions`, `Worktrees` and `Config`, the title of the one shown in bold cyan and the others in grey, their frames grey; beside them the counts for the tab shown, such as `◐ 2 need input · ● 1 working · ■ 1 stopped` in the states' colours, ending with `◇ 1 automated 216 MB` in grey while any automated run is listed (see below). Left and Right move to the tab before or after (stopping at the ends), `1`, `2` and `3` choose one, and a click on a title selects it. Each tab keeps its selection, filter and message while another is shown, and switching is instant; the worktrees scan starts the first time its tab is shown and refreshes only while it is.
- **Table:** in the tab's frame under a header box in the same grey with white column names, its title at the right end of the tabs' row: what it counts and which row is selected, `7 sessions, selected: fix-123`, or `2 of 7 sessions match "templates", selected: templates-sync` while a filter is set, with the filter being typed shown first (a failed listing shows there in red; a title too long for the row is cut at its end). The selection bar is bright cyan. Rest the mouse on a column header to see what the column shows; the tooltip appears on the rows just under the header. Under the table on the Sessions and Worktrees tabs the [detail pane](#the-detail-pane) shows the selected row in full; under that, while there is something to say, a status line with the last message: what a key did, why it was refused, or what the selected row needs. It goes when you move the selection or change the filter, and the table takes its row back. A spinner sits beside the message while an action runs (starting a session, opening a tab, renaming, deleting), and one sits in the middle of the empty table until the first listing or worktree scan arrives.
- **Key bar:** the last two rows name the keys for the selected row: a background session's `Enter Open  x Stop  d Delete  r Rename  m Memory`, a plain `claude` session's `b Adopt` (and `d Close tab` when it has never been sent a message), then `R Resume` while a stopped or failed session is listed, `C Close empty` while an empty terminal session is, `n New session`, and `U Update` when a newer release is known; the second row has the folder keys and the view's own. Click a key or its word to press it, and rest the mouse on it to see what it does; the tooltip is black on amber. The help lists every key whether the bar shows it or not.

The sessions table refreshes every five seconds. `?` shows the keys and the columns of the current view (and, in the sessions view, the path of the Claude Code executable Vantage drives) and `q` quits. Quitting Vantage never stops a session.

Vantage names its own tab `Vantage` (the version is in the settings row) and colours it green (or the first free palette colour when a session tab is already green), so it is easy to find among the session tabs, and puts them back when you quit. A Windows Terminal profile that sets its own `tabColor` keeps it, because the terminal ignores colour changes from inside such a tab. While a dialog is open the tab says which, such as `Vantage (Keys)` for the help or `Vantage (Settings)` while a setting is edited, and goes back to `Vantage` when it closes.

**Help and version.** `vantage --help` (also `-h`, `/h`, `-?` or `/?`) lists the commands and options, and the same option after a command, anywhere on the line, shows that command's own (`vantage here --help`, `vantage setup-terminal -h`). `vantage --version` prints the installed version. Both exit 0 and neither needs Claude Code. Anything Vantage cannot parse is refused with exit code 1: the message names what was not recognised, suggests close matches (`vantage hre` suggests `here`) and shows the help. Options take their value after a space, `=` or `:` (`--prompt="Fix the tests"`), and `--` ends the options. Only `vantage` on its own opens the console.

## The sessions view

Each row is a Claude Code session, ordered so that the ones needing attention come first:

| Column | Meaning |
| --- | --- |
| NAME | The session name given at launch (`-n`), or the folder name for an unnamed one |
| TAB | While a Vantage tab shows the session, a two-cell block in that tab's colour, faded as Windows Terminal draws the tab while Vantage's own tab is selected so that it matches the strip, on the highlighted row too (the tab closes when its attach ends, so the swatch clears with it), `▯` in grey for a session started as plain `claude` in a tab of its own, blank otherwise |
| STATE | `◐ needs input` (amber), `● working` (cyan), `○ idle` (white), `✓ completed` (green), `✗ failed` (red), `■ stopped` (grey) |
| REPOSITORY | The repository the session runs in, such as `Northwind.Orders`, whether in its main checkout, a folder inside it or one of its worktrees; for a folder outside any repository its label, relative to the repository root or worktree root that holds it, else to your home folder (`~\scratch\notes`), else in full. The worktree, branch and full folder are in the [detail pane](#the-detail-pane) |
| PR | The branch's pull request: `#123 open` on GitHub (open, draft, merged, closed), `!123 active` on Azure DevOps (active, draft, completed, abandoned); an open one wins, else the most recent. Blank when there is none or it cannot be looked up; `p` says why. See [Pull requests](#pull-requests) |
| AGE | Time since the session started, right-aligned |

REPOSITORY takes the width the other columns leave, so every column fits a 120-column terminal; a repository name too long for it loses its middle to an ellipsis. NAME is cut at 28 characters with an ellipsis. `<` and `>` scroll the table on a narrower terminal, and the title, the filter and every key still use the full values.

Keys (every key is dispatched by the tested reducer, so this table is the reference):

| Key | Action |
| --- | --- |
| Up, Down | Move the selection bar |
| Left, Right | Show the tab before or after this one |
| <, > | Scroll the table sideways, one column at a time, when its columns do not all fit |
| Enter | Open the session in its own coloured Windows Terminal tab; when one is already open for it, switch to that tab |
| n | New session wizard |
| r | Rename the session from Vantage: type the new name, Enter. The session restarts under the new name with the same id and conversation, so it is refused while the session is working or before its first message. A Vantage tab showing it closes and a tab under the new name opens in the same colour |
| x | Stop the session (it stays listed and can be resumed); a Vantage tab showing it closes |
| d | Delete the session, after a confirmation, closing a Vantage tab showing it first, and the worktree Vantage created for it when nothing in it would be lost; asks before deleting that worktree's branch when it is not merged. A worktree whose `.git` has gone has its folder deleted and its registration pruned; one whose folder a tab or program still has open is reported as in use, by name, and a remove from the worktrees view finishes it once that is closed. On a row with the hollow `▯` swatch (a plain `claude` in its own terminal) that has never been sent a message, closes that tab instead |
| C | Close every tab whose plain `claude` has never been sent a message, after a confirmation naming them |
| b | Move a plain terminal session under Vantage's supervision; its old tab closes once the session has resumed (a failed resume leaves it at its prompt) |
| R | Resume sessions that stopped with a reboot |
| c | Copy the selected row's full folder path to the clipboard |
| e | Open folder: open the row's folder with the program chosen under "Open folder with" on the [Config tab](#the-config-tab) (VS Code unless you chose another on the [Config tab](#the-config-tab)); works on terminal and stopped sessions too |
| v | Open the folder's solution with Windows' default application: `*.sln` or `*.slnx` in the folder, else in its `src` folder; several open the one named after the repository, else ask which from a list of their paths; none says so in the footer |
| p | Open the row's pull request in your default browser; with none, the footer says why (no origin remote, a host Vantage does not know, `gh` or `az` missing or signed out, no pull request for the branch, or the lookup still running) |
| m | Show what the session's memory is made of: Claude Code itself, then one line per program it started (an MCP server named from the package or executable that launched it, a shell running a command, anything else) with that program's whole tree, largest first, and the total with the process count. The [detail pane](#the-detail-pane)'s Memory line shows the total, the private working set (the figure Task Manager's Memory column shows), so pages shared between the MCP servers' node processes count once and pages Windows has paged out do not count. A snapshot, taken when the dialog opens; Esc or Close closes it. A stopped session has nothing to measure. Programs are named from their command lines, which are never shown or written anywhere |
| U | Update Vantage when the settings row names a newer release: after a confirmation, a new tab waits for this Vantage to quit, updates it and starts it again (see [Updates](#updates)) |
| ! | Report a problem: shows the diagnostics a GitHub issue would carry, then opens the issue form in your browser or copies the text (see [Reporting a problem](#reporting-a-problem)) |
| 1, 2, 3 | Show the Sessions, Worktrees or Config tab, from any tab |
| / | Filter by name, repository or branch; Enter keeps the filter, Esc clears it |
| ? | Help: every key and what each column shows; on a short terminal the arrow keys and PageUp and PageDown scroll it |
| q | Quit |

Rows with the hollow `▯` swatch in TAB are sessions started by hand with `claude` in some tab. Vantage can see them but cannot attach to or stop them; press `b` to bring one under supervision, or type `/bg` inside it. One that has never been sent a message has nothing to keep: `d` closes its tab, `C` closes all such tabs, and the footer says so. Vantage ends the claude process, then ends the tab's shell with a clean exit code, which is what makes Windows Terminal close the tab; a session not inside a Windows Terminal tab is just ended.

An automated run is a `claude` started by a program rather than by you: a plugin's background review (the security-guidance plugin reviews changes through the Agent SDK after a commit or push and when a turn ends), a script using the Agent SDK, or `claude -p`. Claude Code lists it as an interactive session with no status, but it cannot be attached to, adopted or renamed and it ends by itself, so it is not a row. While any run, the counts end with `◇`, how many there are and the memory their processes use together, measured as a session's Memory line in the detail pane is; these runs do call the model, so they cost tokens.

## The new session wizard

Press `n`. The wizard is one column, read from the top: your repositories first (recent ones, then everything under the roots), taking the rows the form leaves; type to filter them and use the arrows to choose, then Enter confirms the repository and moves to the form beneath. For a folder that is not a repository, run `vantage here` there instead. Tab and Enter both move down the form in order: name, Where, worktree base, prompt (Enter on Where or the base only moves on; the arrows change them):

- **Session name (required):** the label shown in the NAME column. It also names the worktree folder and branch when you choose a worktree.
- **Where:** `New worktree`, the default unless you choose `Current branch` as the Default place on the [Config tab](#the-config-tab), creates `<repository>-<name>` under the worktree root (`~\source\worktrees` unless you change it on the [Config tab](#the-config-tab)) on branch `<name>`, creating the worktree root the first time, copying the git-ignored files named in the repository's `.worktreeinclude` so builds work there too; `Current branch` runs the session in the repository as it is. Use the arrows to change the choice. A folder that is not a repository needs `Current branch`: Start refuses a worktree there.
- **Worktree base:** what the new worktree's branch starts from: origin's default branch as of your last `git fetch` (Vantage does not fetch first, so fetch if you want the latest), or the commit checked out now (HEAD).
- **Opening prompt:** sent to the session as soon as it starts. It wraps as you type. Leave it blank and the session waits at its prompt until you send it something.

A cyan line under the choices says exactly what will happen ("Worktree Northwind.Orders-fix-tests under C:\Users\you\source\worktrees, on branch fix-tests."). Then:

- **F5 Start:** start the session and open it in its own Windows Terminal tab straight away, as Enter on its row does; Vantage stays on screen in its own tab.
- **Esc:** cancel.

Enter never starts a session here, so there is nothing to undo if you press it too soon. If a repository has not been trusted by Claude Code yet, the start is refused with an explanation and Vantage offers to open a plain tab there so you can run `claude` once and accept the trust prompt.

## Session tabs

**Open (Enter):** Vantage opens a new Windows Terminal tab, titled with the session name and coloured from a palette so each session's tab is recognisable, and attaches there; Vantage itself stays on screen in its own tab. A session that has never had a message opens at its own prompt. The TAB column shows a block in that tab's colour, in the faded shade the tab has while you are looking at Vantage's own tab. While that tab is open, Enter switches to it instead of opening a second one (Vantage finds the tab by its title through Windows UI Automation, so it works in whichever window holds the tab); detaching in it (Ctrl+Z), stopping, renaming or deleting the session closes the tab, because its script exits when `claude attach` ends cleanly; a failed attach keeps the tab open to show the error. Close a tab by hand and the swatch clears within a few seconds; Enter checks the tab's shell is still running before switching, and opens a new tab when it is not. Vantage never attaches in its own tab; `vantage here` is the way to attach in the tab you are in.

## The worktrees view

Press `2` or Right (or click the Worktrees tab). Vantage lists every linked worktree of the repositories under your roots, wherever its folder is (under the worktree root, or beside the repository as versions before 0.8.0 placed them), plus the leftovers git has forgotten under the roots and the worktree root. The first scan of a large roots folder takes a while (around forty seconds for fifty repositories); it refreshes every fifteen seconds while the tab is shown.

```text
Vantage 0.18.0 (35 MB) · Claude 2.1.310
╭────────╭─────────╮──────╮
│Sessions│Worktrees│Config│  3 ok · 1 broken · 1 with changes · 2 with unpushed commits  4 worktrees, selected: Northwind.Bi…
├────────╯         ╰──────┴────────────────────────────────────────────────────────────────────────────────────────────╮
││WORKTREE                                                                                      │LAST COMMIT│SESSION    ││
│ Northwind.Billing › invoice-pdf                                                                  3d4h       invoice-pdf │
│ Northwind.Billing › study                                                                        9d16h                  │
│ Northwind.Orders › fix-tests                                                                     1d0h       fix-tests   │
│ Northwind.Sales › republish                                                                      -                      │
```

| Column | Meaning |
| --- | --- |
| WORKTREE | The owning repository, then `›` and the worktree folder's slug (`Northwind.Billing › invoice-pdf` for the folder `Northwind.Billing-invoice-pdf`), or the folder's label when it is not named after the repository. The column takes the width the others leave and elides the middle of a long repository name. The row's colour is its health: white ok, amber locked, red broken or orphan, grey empty |
| LAST COMMIT | Age of the last commit |
| SESSION | The session running inside it, if any |

The branch, the pull request, the changed files, the unpushed commits and the health are in the [detail pane](#the-detail-pane). The health words: `ok`, `locked` (by another tool), `orphan` (a folder under `.claude\worktrees` git no longer lists), `broken` (a registration whose folder is gone, or a folder under a root or the worktree root whose registration or repository is gone), `empty` (a leftover `.worktrees` folder with nothing in it), `elsewhere` (a live worktree of a repository that is not under the roots, so Vantage never scanned it; the row names that repository, and the footer says where it is).

The selected row's health and its reason are in the detail pane, and its colour in the table. A long repository name is elided in the middle of the WORKTREE cell, and `<` and `>` scroll the table on a narrow terminal.

| Key | Action |
| --- | --- |
| d | Remove the worktree, or delete a leftover folder. Refused, with the reason, when it has changed files, unpushed commits, a lock or a live session, and always for an `elsewhere` worktree, which is not a leftover: add its repository's root on the Config tab, or remove it with git there. A broken leftover also has its stale registration pruned from the repository. A folder a tab or program still has open is reported as in use, by name; what git could delete is gone, and a second `d` once it is closed finishes the job. Junctions and links inside a leftover (pnpm's `node_modules`, for one) are removed as links and never followed, so nothing outside the folder is touched. A leftover's files are counted up to 1,000, so a large one is refused as "at least 1000 files" |
| D | Force removal: the folder name opens selected in a read-only box, so Ctrl+C copies it (F2 also copies, from anywhere in the dialog); Tab or Enter moves to the answer box, Ctrl+V pastes, Enter confirms. There is no Remove button; a wrong or empty answer keeps the dialog open and says what was typed, and Esc cancels. Never forces a locked worktree, one with a live session, or an `elsewhere` one |
| P | Prune stale registrations in the row's repository (`p` before 0.11.0) |
| p | Open the row's pull request in your default browser, as in the sessions view |
| s | Start a session in this worktree: a compact wizard with only the name and the prompt; type a name and Enter (or F5) starts the session and opens it in its own tab |
| o | Open a plain PowerShell tab in the worktree |
| Enter | Go to the session running in the worktree (selected by its id, so two sessions with the same name cannot be confused) |
| 1, Esc | Back to the Sessions tab (Esc clears a filter first); `3` shows the Config tab |
| Left, Right | The tab before or after, as in the sessions view |
| <, > | Scroll the table sideways, as in the sessions view |
| / | Filter by repository, folder, branch or health |
| c, e, v | Copy the folder path, open the folder, open its solution (choosing from a list when there are several), as in the sessions view; refused for a leftover whose folder is gone |
| U | Update Vantage, as in the sessions view |
| ! | Report a problem, as in the sessions view |

Branches: when a removed worktree's branch was created by Vantage and is merged, the branch is deleted too; when Vantage created it but it is not merged, or you created it by hand, Vantage asks before deleting it.

## The detail pane

Under the Sessions and Worktrees tables a pane shows what the selected row does not, under a grey divider titled with its name. It is always as tall as its tab's facts can be (five rows on Sessions, eleven on Worktrees), so the table does not jump as you move between rows. The Config tab has no pane. It is read for the selected row only, off to one side, so the table never waits for it; a fact that cannot be read says unavailable and the rest still show.

For a session:

```text
─┤ fix-123 ├──────────────────────────────────────────────────────────────────────────────────────────────────────────
 Waiting  permission prompt  Started 14:02 (5m ago)                         Prompted 14:03 (4m ago)
 Folder   C:\Users\me\source\worktrees\Northwind.Orders-fix-123            Session  81cdb0f9-0000-4000-8000-000000000001
 Worktree fix-123            Branch  fix-123                                Tab      open, yellow
 Changes  3 changed files · 0 unpushed commits · last commit 2h00m ago      Memory   903 MB (m for the breakdown)
```

- **Waiting:** what the session waits for, in amber: the reason Claude Code lists (a permission prompt, a question), or `a first prompt` for a session that has never had one; `nothing` otherwise.
- **Started, Prompted:** when it started, and the last time this Vantage saw it go from needs input or idle to working, which is when a prompt or an answer reached it; `not seen since Vantage started` otherwise. Nothing is read from the session's transcript for it.
- **Folder, Session:** the full folder and the session id.
- **Worktree, Branch, Tab:** the worktree's slug, the path inside the repository, `main checkout` or `not in a repository`; the branch; and the tab, open with its colour, or its own terminal for a plain `claude` session.
- **Changes, Memory:** the folder's changed files, unpushed commits and last commit, and the memory the session and everything it started use (`m` for the breakdown); `nothing to measure` for a stopped session.

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

The health with its reason in full (in the row's colour), the branch and whether it is merged into origin's default branch, the pull request, the full folder, whether and when Vantage created it, and the changed files and unpushed commits (amber when there are any); then up to five unpushed commits, newest first with hash, age and subject, or the changed files when nothing is unpushed, and how many more.

## Pull requests

The PR column in both views shows the pull request for each row's branch, read from the repository's `origin` remote: a `github.com` remote is asked through the GitHub CLI, an Azure DevOps remote (`dev.azure.com`, `*.visualstudio.com`, HTTPS or SSH) through the Azure CLI. Any other host leaves the column blank.

- **Signing in:** run `gh auth login` once for GitHub; for Azure DevOps install the Azure CLI, run `az extension add --name azure-devops` and `az login`. Vantage never signs in for you and stores no credential.
- **Refreshing:** each row's branch is looked up on its own (`gh pr list --head`, `az repos pr list --source-branch`, twenty pull requests at most), only for rows on screen, never for every repository under the roots. An answer is kept for a minute, so a change shows within about 75 seconds; a signed-out or missing CLI is asked again only after three minutes. At most four lookups run at once, and the rows never wait for them: the column fills on the refresh after an answer lands. Each lookup is one short `gh` or `az` process returning a few kilobytes.
- **Which one:** a branch with an open (or draft, or active) pull request shows that one; otherwise its most recent.
- **Links:** an Azure DevOps pull request opens at `https://dev.azure.com/<organisation>/<project>/_git/<repository>/pullrequest/<number>`, built from the `origin` remote. Only an `https` link is shown or opened.
- **Branch names on Azure DevOps:** the Azure CLI is a batch file (`az.cmd`), which Windows runs through `cmd.exe`, and `cmd.exe` reads a double quote, `&`, `|`, `%`, `<`, `>`, `^` and `!` as commands or expansions. So a branch whose name holds anything but letters, digits, dots, underscores, hyphens and slashes is never given to it (nor is an organisation, project or repository name): the row's PR column stays blank and `p` says why. The GitHub CLI is an executable and takes any branch name.

## vantage here

From any folder, `vantage here` starts a supervised session in that folder, on its current branch, and attaches in the same tab, so a session started from an ordinary terminal is Vantage-managed from the outset. The session is named after the folder unless you give a name.

```powershell
vantage here
vantage here fix-tests -p "Make the failing tests in OrdersShould pass"
vantage here --help   # the options
vh fix-tests          # with the profile shortcut
```

Options: `-p`/`--prompt` sends an opening prompt (without one the session waits at its prompt). Ctrl+Z detaches and returns to the shell; the session keeps running and is listed in Vantage. Exit codes: 0 after a detach, 1 when the start failed (for example an untrusted folder) or the command line could not be parsed, 2 when the name or the opening prompt is refused. An opening prompt that starts with `-` is refused, in the wizard too: Claude Code would read it as options and has no `--` to stop it, so start the prompt with a word. A name that starts with `-` is refused as a likely mistyped option (`vantage here --promt` names `--promt`, shows the help and exits 1); to use such a name, or one that starts with `@` (which otherwise reads a response file), put `--` before it: `vantage here -- -experiment`.

## Notifications

Two things ring the terminal bell, and Windows Terminal flags the tab (or the window or taskbar, depending on the profile's bell style):

- **Claude Code itself**, with `preferredNotifChannel` set to `terminal_bell` (see [Windows Terminal setup](#windows-terminal-setup)), rings the bell in the tab a session is attached in when it finishes a task or pauses for a permission prompt while you appear to be away.
- **Vantage** rings the bell in its own tab when a background row changes to needs input between two refreshes, so a session with no tab of its own still gets your attention.

There are no toasts; the need input count beside the tabs and the amber rows are the overview. Bell styles and sounds are set in [Windows Terminal setup](#windows-terminal-setup).

## Windows Terminal setup

Windows Terminal plays the bell with whatever sound the profile has, so a bell from Vantage sounds like every other notification until it has a profile of its own. `vantage setup-terminal` gives it two:

- **Vantage:** runs the installed `vantage`; open it from the new-tab menu to get Vantage in a tab that closes when you quit. It has no tab colour, so Vantage still colours its own tab.
- **Vantage session:** PowerShell. Every tab Vantage opens (Enter) uses it once it exists, so a bell from an attached session sounds different from Vantage's own.

Both profiles carry the Vantage icon as their tab icon, so it shows on Vantage's own tab and on every tab Vantage opens, but only on tabs opened from these profiles: a `vantage` typed into an ordinary PowerShell tab keeps that tab's icon. The icon is copied next to the fragment, so run `vantage setup-terminal` again after an update to refresh it. Both profiles sound the bell and flash the window and the taskbar. The profiles are added as a Windows Terminal fragment, a small JSON file that Windows Terminal reads at start-up; your own `settings.json` is never touched. Restart Windows Terminal after running the command, and the profiles appear in the new-tab menu.

```powershell
vantage setup-terminal                                   # show what will be written, then ask
vantage setup-terminal --yes                             # no questions
vantage setup-terminal --sound "C:\Windows\Media\chimes.wav" --session-sound "C:\Windows\Media\Ring01.wav"
vantage setup-terminal --remove                          # delete the fragment
```

Without options the Vantage profile plays `Windows Notify Messaging.wav` and the session profile `Windows Proximity Notification.wav`, both from `C:\Windows\Media`; a default that is missing on your machine is left out and that profile uses Windows Terminal's own bell sound. A sound you name must be an existing `.wav` file. The command first shows the file it will write and asks; any answer but yes, or no answer, writes nothing. Run it again with other sounds to replace the fragment. Exit codes: 0 done, declined or nothing to remove, 1 a write failed or the command line could not be parsed, 2 a missing or non-wav sound or `--remove` with a sound.

**Claude Code's bell.** The command then offers to set `"preferredNotifChannel": "terminal_bell"` in `~\.claude\settings.json`, keeping every other setting. Claude then rings the bell when it finishes a task or pauses for a permission prompt while you appear to be away, and the tab it is attached in is flagged. It applies to sessions started from now on. You can add the line by hand instead:

```json
{ "preferredNotifChannel": "terminal_bell" }
```

**By hand.** In Windows Terminal, open Settings, choose a profile, then Advanced: "Bell notification style" (tick Audible, Window and Taskbar) and "Bell sound" (a `.wav` path). To make every profile flash, set the same under Defaults.

**Suggested sounds** in `C:\Windows\Media`: `Windows Notify Messaging.wav`, `Windows Notify Calendar.wav`, `Windows Proximity Notification.wav`, `Windows Message Nudge.wav`, `chimes.wav`, `Alarm01.wav` to `Alarm10.wav` and `Ring01.wav` to `Ring10.wav`. Not every edition of Windows ships all of them.

**Undo.** `vantage setup-terminal --remove` deletes the fragment; restart Windows Terminal and the profiles are gone, and Enter opens tabs in your default profile again. Remove the `preferredNotifChannel` line from `~\.claude\settings.json` yourself if you want Claude's bell off.

## Common scenarios

**Start work on a story in a clean worktree.** `n`, choose the repository, name the session after the story (`New worktree` is already chosen), type the opening prompt, F5. The session opens in its own coloured tab and the overview stays where it is. The worktree is `<repository>-<name>` under the worktree root, its REPOSITORY cell reads the repository, and the detail pane names the worktree's slug.

**Kick off several sessions and let them run.** Use `n` with F5 for each, giving each a distinct name so you can tell them apart; each opens in its own tab, and you can switch back to the Vantage tab straight away. Leave Vantage open: rows turn amber when one needs you, and the Vantage tab rings the bell even when another tab is in front. Enter on the amber row opens or switches to its tab; answer there, and Ctrl+Z closes the tab when you are done.

**Keep one session in its own tab.** Enter opens it in a coloured tab you can leave open all day. The TAB column shows a block in the tab's colour; Enter again switches to that tab until it is closed.

**Start Claude from a repository folder you are already in.** `vh fix-tests -p "..."` instead of `claude`. It shows up in Vantage with a job id, so Enter, `x` and `d` work on it later.

**A session you started with plain `claude` needs the same treatment.** Select its row (the hollow `▯` swatch) and press `b` while it is idle or waiting. Vantage ends the plain process and resumes the same conversation under the supervisor with the same name; the old tab becomes an ordinary shell. A session you have not sent anything to yet is refused, because Claude writes no transcript until the first message and there would be nothing to resume: `d` closes that tab instead.

**After a reboot.** Sessions show as stopped or failed with no process. `R` resumes the failed ones and offers the stopped ones as a checklist.

**Finish a story and clean up.** `d` on the session deletes it and, if Vantage created the worktree and it is clean and pushed, offers to remove the worktree and its merged branch. Anything with uncommitted or unpushed work is refused with the reason.

**Find out what is cluttering the repositories folder.** `2` lists every worktree with dirty and unpushed counts and health. Empty leftover folders go with `d`; a worktree with uncommitted work needs `D` and the copied folder name, which is deliberate.

**Something needs Claude Code's own screen.** Run `claude agents` in any shell for the native agent view; Esc returns.

## Where Vantage keeps things

Everything Vantage owns is under `%LOCALAPPDATA%\Vantage`:

| File or folder | Contents |
| --- | --- |
| `config.json` | Your settings: `repositoryRoots` (default `~\source\repos`), `worktreeRoot` (default `worktrees` beside the first root, so `~\source\worktrees`), `editor` (default VS Code), `defaultBase` (`RemoteDefault` or `Head`), `checkForUpdates` (default `true`) and `defaultPlace` (`NewWorktree`, the default, or `CurrentBranch`); see [Settings](#the-config-tab) |
| `update-check.json` | When Vantage last asked nuget.org for a newer release of itself and the newest release it found; see [Updates](#updates) |
| `recent\` | One file per recently used repository, with when it was last used; the ten most recent are shown first in the wizard |
| `created.json` | Worktrees Vantage created, which decides the branch-deletion rules |
| `tabs\` | One file per tab Vantage opened, so a second Enter switches to the tab instead of opening another |
| `last-crash.log` | Written only when an exception ends Vantage: its diagnostic log, the child program counts and the crash; replaced by the next crash; see [What Vantage records](#what-vantage-records) |

`vantage setup-terminal` also writes `%LOCALAPPDATA%\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json`, which belongs to Windows Terminal's fragment mechanism rather than to Vantage's own folder, and can set one line in `~\.claude\settings.json` (see [Windows Terminal setup](#windows-terminal-setup)).

A missing or corrupt file is treated as empty. Recent repositories are one file each so that `vantage here` and the console can record at the same time without losing a write; the `recent.json` file of versions before 0.3.0 is imported once and removed. The `descriptions\` folder and `sessions.json` left by earlier versions are no longer used and can be deleted. Sessions themselves belong to Claude Code (`claude agents --json`), never to Vantage.

## What Vantage trusts

Vantage trusts every repository under its roots: it runs git in each of them on every scan, copies the gitignored files a repository's `.worktreeinclude` names into new worktrees, and opens solutions with the application Windows associates with them, so a repository you would not open in an editor should not sit under a root.

It runs the Claude Code it finds at `~\.local\bin\claude.exe` or as `claude.exe` on the PATH, `git`, `gh` and `az` for repositories and pull requests, and `wt.exe` and `pwsh.exe` for tabs. Each is found in the PATH folders once, as Vantage starts, and run by its full path, so a program planted in the folder you started Vantage from is never run (Windows would otherwise look there first for a bare name). A `.cmd` or `.bat` is never run with a session name, a prompt or a path: Claude Code must be an executable, a custom editor's executable must be a full path to one, and the Azure CLI, which is a batch file, is given plain text only (see [Pull requests](#pull-requests)). An opening prompt that starts with a dash is refused rather than passed to Claude Code as options.

Its own state lives in `%LOCALAPPDATA%\Vantage`; it never edits Claude Code's trust records and stores no credential. Once a day it asks nuget.org for its newest version, which the setting or `VANTAGE_NO_UPDATE_CHECK` turns off, and the update tab installs from nuget.org alone. The diagnostic log and a problem report hold only what [What Vantage records](#what-vantage-records) lists: never a path, a name, a prompt or anything typed.

## The Config tab

Press `3`, or Right until it shows (or click the Config tab). Each setting has a row, and below them, in grey, the setup Vantage reads but does not change:

```text
Vantage 0.18.0 (35 MB) · Claude 2.1.310
╭────────╭─────────╭──────╮
│Sessions│Worktrees│Config│  7 settings · 1 to set up                         12 rows, selected: Repository root
├────────┴─────────╯      ╰───────────────────────────────────────────────────────────────────────────────────╮
││SETTING                  │VALUE                                                                             ││
│ Repository roots          C:\Users\you\source\repos                                                        │
│                           D:\work                                                                          │
│ Worktree root             C:\Users\you\source\worktrees                                                    │
│ Open folder with          VS Code                                                                          │
│ Default base              origin's default branch, as last fetched                                         │
│ Default place             New worktree                                                                     │
│ Check for updates         on                                                                               │
│ Settings file             C:\Users\you\AppData\Local\Vantage\config.json                                   │
│ Windows Terminal profiles not set up, run vantage setup-terminal                                           │
│ Claude bell               on                                                                               │
│ Diagnostic log            off                                                                              │
│ Last update check         09:12 today, newest 0.18.0                                                       │
```

- **Repository roots:** one row per root, the first labelled; the wizard offers every repository directly under each, and the worktrees view scans them. Enter on any of them opens one dialog with every root, one per line: add a line to add a root, delete one to remove it, and F5 saves. Browse (F3) opens a folder picker inside the terminal instead of typing: Tab into its list, choose a folder with the arrows (Shift+Down adds the next one too) and press Enter, and each folder chosen is added as a line after the roots already there, to be checked and saved with F5 like a typed one; Esc in the picker adds nothing. Each must be a full path that exists and be listed once, and at least one must stay.
- **Worktree root:** where new worktrees go, as `<worktree root>\<repository>-<name>`. It must be a full path; it is created when the first worktree goes there. Browse (F3) opens the same folder picker, starting beside the current worktree root, and the folder chosen replaces the path in the box, saved with F5. Worktrees made beside their repository by earlier versions stay where they are and are still listed, removed and opened as before.
- **Open folder with:** VS Code, Visual Studio (the newest install, which opens the folder in its folder view), Rider, Explorer, or Custom: the full path of an executable and its arguments, with `{folder}` where the folder goes (double quotes keep an argument with spaces together). A bare name or a relative path is refused, because Windows would look for it in the folder Vantage was started from before the PATH; a `.cmd` or `.bat` file is refused, because Windows runs it through `cmd.exe`, which would read the folder path as commands. The `e` key opens the selected row's folder with it. Cursor is no longer offered; a `config.json` that still names `Cursor` reads as VS Code, the other settings kept.
- **Default base:** where a new worktree's branch starts by default: origin's default branch as of your last `git fetch` (no fetch is made first), or the commit checked out now (HEAD).
- **Default place:** where the wizard opened with `n` starts a session, `New worktree` (the default) or `Current branch`; Where still offers the other. `s` on a worktree and `vantage here` always start on the folder's current branch.
- **Check for updates:** whether Vantage asks nuget.org once a day for a newer release of itself (see [Updates](#updates)); on unless you turn it off. Enter turns it on or off and saves at once.
- **Settings file, Windows Terminal profiles, Claude bell, Diagnostic log, Last update check:** where `config.json` is, whether `vantage setup-terminal` has added the profiles and set Claude's bell (each says to run it when it has not), the `VANTAGE_LOG` file if one is set, and when nuget.org was last asked. They are read each time the tab is shown and after each save; Enter on one says how it is changed. The counts beside the tabs name how many settings there are and how much setup is left to do.

The line under the table says what the selected row's setting does, or what its setup is for, and resting the mouse on a row shows the same words as a tooltip.

Enter on a setting opens a small dialog for that one value. F5 saves: the dialog checks the value first, names any problem and stays open; once saved, `config.json` is written and the next scan, wizard and editor launch use the new value at once, without a restart. Esc closes the dialog without changing anything. `c` copies the selected path and `e` opens the selected folder, or the folder holding the selected file.

`config.json` can also be edited by hand while Vantage is closed (a hand edit made while it runs is overwritten by the next save from the tab, and otherwise applies on the next start). A file from an earlier version keeps working: every property it leaves out reads as its default, and a custom editor without `{folder}`, or whose executable is not a full path to something other than a batch file, reads as VS Code.

| Key | Action |
| --- | --- |
| Up, Down | Move the selection bar |
| Enter | Edit the selected setting (on any repository root, every root together, one per line); on Check for updates, turn it on or off; on a setup row, say how that setup is changed |
| c, e | Copy the selected path; open the selected folder, or the folder holding the selected file |
| Left, Right, 1, 2, Esc | Other tabs, as in the sessions view (Esc clears a filter first) |
| / | Filter by setting or path |
| U, !, ?, q | Update, report a problem, help and quit, as in the sessions view |

```json
{
  "repositoryRoots": ["C:\\Users\\you\\source\\repos", "D:\\work"],
  "defaultBase": "RemoteDefault",
  "worktreeRoot": "C:\\Users\\you\\source\\worktrees",
  "editor": { "preset": "Custom", "executable": "C:\\Tools\\subl.exe", "arguments": ["-n", "{folder}"] },
  "checkForUpdates": true,
  "defaultPlace": "NewWorktree"
}
```

## Updates

A dotnet global tool never updates itself, so Vantage looks for you. Once a day at most, in the background as Vantage opens, it asks nuget.org for the versions of `Source191.Vantage`; when a higher release exists the Vantage settings row says so, for example `Vantage: 0.9.0 (0.10.0 available, U to update)`. The check runs in the background with a 3 second timeout: a slow or failed request shows nothing, delays nothing and is tried again on the next start. Prerelease versions on nuget.org are ignored, and a local or CI build (a prerelease version itself) never checks.

`U` in either view asks "Quit Vantage and update to 0.10.0?". On yes Vantage opens a tab titled "Update Vantage" and quits; the tab waits for that Vantage to exit (a running `vantage.exe` locks its own files), runs `dotnet tool update -g Source191.Vantage --source https://api.nuget.org/v3/index.json` (so the package comes from the feed the check consulted, whatever other sources the machine has configured), and starts Vantage again when the update succeeds. When it fails, typically because another Vantage is still open in some tab, the tab stays open with dotnet's message: quit every Vantage and press `U` again, or run the command yourself. Background sessions run under Claude's supervisor and keep running throughout. With no newer release known, `U` says Vantage is up to date, or that the check is off.

**What the check sends:** one anonymous HTTPS request to `https://api.nuget.org/v3-flatcontainer/source191.vantage/index.json`, the public list of the package's versions, with a `Vantage/<version>` user agent and nothing else that identifies you or your machine. Turn it off with **Check for updates** on the [Config tab](#the-config-tab) or `"checkForUpdates": false` in `config.json`. Setting the environment variable `VANTAGE_NO_UPDATE_CHECK` to any non-empty value (for a script or test harness) turns it off without touching `config.json`: no request is made and no `update-check.json` is written. The hand-off check in `tools\HeadlessTerminal` sets it.

## What Vantage records

Vantage keeps a diagnostic log in memory while it runs (the latest 500 events) so a problem can be diagnosed; it never leaves your machine by itself. It holds only facts Vantage defines in its own code:

- **Start:** the Vantage, Claude Code, Windows and .NET versions and the terminal's size.
- **What you did, as kinds:** the view and each dialog opened, and each key's effect with the kind of its outcome and how long it took.
- **Failures:** where they happened and an exception summary: the exception's type and Vantage's stack frames (method, source file name and line), never the exception's message.
- **Child programs:** counts by program (git, claude, gh, az, pwsh, wt, vswhere, dotnet or another program), subcommand from a fixed list of words and exit code, with the longest run; a line for one that takes longer than 5 seconds or cannot be started.
- **Terminal.Gui:** its warnings and errors by level, without its message text, which can quote input it could not read.

It never holds a path, a repository, branch, worktree or session name, a job or session id, a prompt, pull-request detail, a file name, anything a program printed, your user or machine name, or anything you typed; `DiagnosticEventsShould` and `DiagnosticsPrivacyShould` fail the build's tests if it could. `VANTAGE_LOG` writes the same lines to a file, and a crash writes them to `last-crash.log` (see [Where Vantage keeps things](#where-vantage-keeps-things)).

## Reporting a problem

Press `!` in either view. A dialog shows a title box and, read-only, exactly the diagnostics the report will carry: the Vantage, Claude Code, Windows and .NET versions, the terminal's size, the editor preset, whether update checks are on and how many repository roots you have, the recent events of [What Vantage records](#what-vantage-records), the child program counts and, after a crash, the crash. It holds no names, paths or code.

**Open in browser** (or Enter) opens the repository's "Problem report" issue form with the title and the diagnostics filled in; you describe the problem there, where Markdown and screenshots work, change or delete anything, and submit it from your own GitHub account. Vantage sends nothing itself and holds no token. Opening the page does pass the title and the diagnostics to GitHub in the page's address, so read the preview first; nothing is public until you submit. A blank title becomes `Problem report from Vantage <version>`. The page's address is kept under 7,500 characters, so a long log leaves out its oldest ordinary lines first and says how many; the versions, failures and the crash are kept. A report that opens takes `last-crash.log` with it and removes it, so the next report does not repeat an old crash.

**Copy text** puts the title and the whole diagnostics on the clipboard instead, for someone without a GitHub account or when the browser cannot be opened. Esc cancels.

## Troubleshooting

- **Something else went wrong.** Press `!` to [report it](#reporting-a-problem) with Vantage's diagnostics filled in.
- **Keys stop working after returning from a session.** Set `$env:VANTAGE_LOG = "$env:TEMP\vantage.log"` before starting Vantage; the file gets Vantage's diagnostic log as it happens (every hand-off and its return, the views and dialogs, failures, Terminal.Gui's warnings and errors) and the child program counts when Vantage quits; see [What Vantage records](#what-vantage-records). The known cause (blocking the UI thread after a hand-off) is fixed and guarded by the check in `tools\HeadlessTerminal`.
- **"not trusted yet".** Claude Code refuses to start in a folder you have not trusted. Open a tab there, run `claude`, accept the prompt, then start again. Vantage never edits the trust records.
- **No bell when a session needs you.** Run `vantage setup-terminal` and see [Windows Terminal setup](#windows-terminal-setup); it covers `preferredNotifChannel` in `~\.claude\settings.json` and the bell style and sound of the Windows Terminal profile. Claude only rings when it thinks you are away from the terminal.
- **Tabs or sessions opened from Vantage have no colours.** Vantage was started from inside a Claude Code session, whose environment sets NO_COLOR; start Vantage from an ordinary tab.
- **The worktrees view is slow to appear.** The first scan runs `git worktree list` for every repository under the roots and three git commands per worktree. Narrow `repositoryRoots` if the folder is very large.
- **Claude Code was not found.** Vantage looks for `~\.local\bin\claude.exe`, then `claude.exe` in a folder on the PATH.
- **Claude Code was found only as a batch file.** A `claude.cmd` on its own (an npm install's shim) is named and refused: Windows would run it through `cmd.exe`, which reads session names and prompts as commands. Install the native build with `irm https://claude.ai/install.ps1 | iex`.
- **git was not found on the PATH.** Vantage needs `git.exe` in a folder on the PATH (`winget install --id Git.Git --exact --source winget`, then a new terminal); a `git.cmd` does not count.
- **Windows Terminal (wt.exe) or PowerShell 7 (pwsh.exe) was not found.** The key that needs it says so in the footer; both must be in a folder on the PATH.

## Contributing

Vantage welcomes contributions. [CONTRIBUTING.md](https://github.com/Source191/vantage/blob/main/CONTRIBUTING.md) explains how to propose a change, build and test Vantage, install your own build, and take a change through OpenSpec to a release.

## Support Vantage

Vantage is built in the gaps between rate limits. Help close the gaps: [buy me a sliver of a Claude Max plan](https://buymeacoffee.com/simonvane).

![QR code for buymeacoffee.com/simonvane](https://raw.githubusercontent.com/Source191/vantage/main/assets/support/buy-me-a-coffee-qr.png)

## Roadmap and known limits

- Windows only, with Windows Terminal; tab colours and titles use `wt.exe`.
- Still to observe at the terminal: whether `claude attach` to one session from two tabs is refused or allowed, and whether `claude --bg --resume <id>` or `claude respawn --all` is the right way to bring sessions back after a reboot (the listing side is documented: no pid, state failed or stopped).
- A take-over key (stop the supervised session and resume it as a normal foreground `claude`, at the cost of supervision) is undecided; `b` covers the other direction.
- Multiple Claude accounts: Claude Code's `CLAUDE_CONFIG_DIR` selects a configuration directory with its own login, settings, sessions and supervisor; Vantage would gain a profile column and set the variable when starting a session.
- Installing with `winget install`, as a self-contained `vantage.exe` that needs no .NET SDK ([#93](https://github.com/Source191/vantage/issues/93)).
- A per-path cache for the worktree scan, so the first look at the Worktrees tab is quick on a large roots folder.
- Pinning sessions, once the CLI exposes it outside the native agent view; configurable palette and Windows Terminal profile for the tabs Vantage opens.
