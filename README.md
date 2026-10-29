# Vantage

![Vantage: mission control for Claude Code sessions, every session in one tab, still there after a reboot. Beside it, the icon: two branches merging into one node, shaped as a V](https://raw.githubusercontent.com/Source191/vantage/main/assets/header/readme-banner.png)

[![Buy me a sliver of a Claude Max plan](https://img.shields.io/badge/Buy_me_a_sliver_of_a-Claude_Max_plan-FFDD00?logo=buymeacoffee&logoColor=black)](https://buymeacoffee.com/simonvane)

Vantage is mission control for Claude Code on Windows: one Windows Terminal tab that shows every session on the machine, so you no longer have to remember which tab is doing what or which one is waiting for you.

- **See every session at once:** each one is listed with its state, repository, branch and pull request.
- **Start in a fresh worktree:** a new session gets its own git worktree, so sessions never trip over each other's changes.
- **Attach, or open a tab:** attach to any session from the list, or open it in its own tab, named after the session and coloured so you can tell it apart at a glance.
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
- [Attaching, tabs and getting back](#attaching-tabs-and-getting-back)
- [The worktrees view](#the-worktrees-view)
- [Pull requests](#pull-requests)
- [vantage here](#vantage-here)
- [Notifications](#notifications)
- [Windows Terminal setup](#windows-terminal-setup)
- [Common scenarios](#common-scenarios)
- [Where Vantage keeps things](#where-vantage-keeps-things)
- [What Vantage trusts](#what-vantage-trusts)
- [Settings](#settings)
- [Updates](#updates)
- [Troubleshooting](#troubleshooting)
- [Developing Vantage](#developing-vantage)
- [Continuous integration](#continuous-integration)
- [Working with OpenSpec](#working-with-openspec)
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

   To work on Vantage itself, pack and install the tool from this repository:

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

2. Set up the terminal bell (optional but recommended): run `vantage setup-terminal`. It adds two Windows Terminal profiles with their own bell sounds and can turn on Claude Code's terminal bell; see [Windows Terminal setup](#windows-terminal-setup).

3. Add the profile shortcuts (optional). These two lines in `$PROFILE` give `v` for `vantage` and `vh` for `vantage here`:

   ```powershell
   Set-Alias v -Value vantage
   function vh { vantage here @args }
   ```

New shells pick up `~\.dotnet\tools` on the PATH; a shell that was open before the install needs restarting.

## Start Vantage

Open a tab and run `vantage` (or `v`). The screen has three parts:

- **Header:** Vantage's version, the Claude Code version (`unknown` when `claude --version` cannot be read, and `2.1.302 (below 2.1.309, update Claude Code)` when it is older than the minimum Vantage supports), the repository roots being scanned, counts of sessions by state, and the key hints. A local install shows its prerelease label without the build timestamp (`0.6.0-local`). Rest the mouse on a key hint, or on its words, to see what the key does and when to use it; the tooltip is black on amber.
- **Table:** one row per session, titled with what it counts and which row is selected: `7 sessions, selected: fix-123`, or `2 of 7 sessions match "templates", selected: templates-sync` while a filter is set. The selection bar is cyan. Rest the mouse on a column header to see what the column shows; the tooltip appears below the table.
- **Footer:** the current view as a crumb, then the last message: what a key did, why it was refused, or what the selected row needs. A spinner sits beside it while an action runs (starting a session, opening a tab, renaming, deleting), and one sits in the middle of the empty table until the first listing or worktree scan arrives.

The table refreshes every five seconds. `?` shows the keys and the columns of the current view (and, in the sessions view, the path of the Claude Code executable Vantage drives) and `q` quits. Quitting Vantage never stops a session.

Vantage names its own tab `Vantage` (the version is in the header) and colours it green (or the first free palette colour when a session tab is already green), so it is easy to find among the session tabs; they are restored when you return from an attached session (Claude names the tab itself while attached) and put back when you quit. A Windows Terminal profile that sets its own `tabColor` keeps it, because the terminal ignores colour changes from inside such a tab. While a dialog is open the tab says which, such as `Vantage (Keys)` for the help or `Vantage (Settings)`, and goes back to `Vantage` when it closes.

**Help and version.** `vantage --help` (also `-h`, `/h`, `-?` or `/?`) lists the commands and options, and the same option after a command, anywhere on the line, shows that command's own (`vantage here --help`, `vantage setup-terminal -h`). `vantage --version` prints the installed version. Both exit 0 and neither needs Claude Code. Anything Vantage cannot parse is refused with exit code 1: the message names what was not recognised, suggests close matches (`vantage hre` suggests `here`) and shows the help. Options take their value after a space, `=` or `:` (`--prompt="Fix the tests"`), and `--` ends the options. Only `vantage` on its own opens the console.

## The sessions view

Each row is a Claude Code session, ordered so that the ones needing attention come first:

| Column | Meaning |
| --- | --- |
| NAME | The session name given at launch (`-n`), or the folder name for an unnamed one |
| STATE | needs input (amber), working (cyan), idle (white), completed (green), failed (red), stopped (grey) |
| REPOSITORY | The repository the session runs in (a linked worktree shows its main checkout) |
| FOLDER | The folder the session runs in, relative to the repository root or the worktree root that holds it (`Northwind.Orders-fix-123`), else relative to your home folder (`~\scratch\notes`), else in full. A worktree is recognisable because its folder name differs from its repository |
| BRANCH | The branch checked out there |
| PR | The branch's pull request: `#123 open` on GitHub (open, draft, merged, closed), `!123 active` on Azure DevOps (active, draft, completed, abandoned); an open one wins, else the most recent. Blank when there is none or it cannot be looked up; `p` says why. See [Pull requests](#pull-requests) |
| AGE | Time since the session started |
| TAB | `terminal` for a session started as plain `claude` in a tab, `open` while a Vantage tab shows the session (the tab closes when its attach ends, so `open` clears with it) |

NAME, REPOSITORY, FOLDER and BRANCH are cut at 28 characters with an ellipsis, so one long name cannot push the other columns off screen; Left and Right scroll the table to them, and the title, the filter and every key still use the full value.

Keys (every key is dispatched by the tested reducer, so this table is the reference):

| Key | Action |
| --- | --- |
| Up, Down | Move the selection bar |
| Left, Right | Scroll the table sideways, one column at a time, when its columns do not all fit |
| Enter | Attach to the selected background session in this tab; Ctrl+Z returns |
| o | Open the session in its own coloured Windows Terminal tab; when one is already open for it, switch to that tab |
| n | New session wizard |
| r | Rename the session from Vantage: type the new name, Enter. The session restarts under the new name with the same id and conversation, so it is refused while the session is working or before its first message. A Vantage tab showing it closes and a tab under the new name opens in the same colour |
| x | Stop the session (it stays listed and can be resumed); a Vantage tab showing it closes |
| d | Delete the session, after a confirmation, closing a Vantage tab showing it first, and the worktree Vantage created for it when nothing in it would be lost; asks before deleting that worktree's branch when it is not merged. A worktree whose `.git` has gone has its folder deleted and its registration pruned; one whose folder a tab or program still has open is reported as in use, by name, and a remove from the worktrees view finishes it once that is closed. On a `terminal` row that has never been sent a message, closes that tab instead |
| C | Close every tab whose plain `claude` has never been sent a message, after a confirmation naming them |
| b | Move a plain terminal session under Vantage's supervision; its old tab closes once the session has resumed (a failed resume leaves it at its prompt) |
| R | Resume sessions that stopped with a reboot |
| c | Copy the selected row's full folder path to the clipboard |
| e | Open the folder in your editor (VS Code unless you chose another in [Settings](#settings)); works on terminal and stopped sessions too |
| v | Open the folder's solution with Windows' default application: `*.sln` or `*.slnx` in the folder, else in its `src` folder; several open the one named after the repository, else ask which; none says so in the footer |
| p | Open the row's pull request in your default browser; with none, the footer says why (no origin remote, a host Vantage does not know, `gh` or `az` missing or signed out, no pull request for the branch, or the lookup still running) |
| , | Settings: the repository roots, the worktree root, the editor, the default worktree base and the update check (see [Settings](#settings)) |
| U | Update Vantage when the header names a newer release: after a confirmation, a new tab waits for this Vantage to quit, updates it and starts it again (see [Updates](#updates)) |
| w | Switch to the worktrees view |
| / | Filter by name, repository or branch; Enter keeps the filter, Esc clears it |
| ? | Help: every key and what each column shows; on a short terminal the arrow keys and PageUp and PageDown scroll it |
| q | Quit |

Rows marked `terminal` are sessions started by hand with `claude` in some tab. Vantage can see them but cannot attach to or stop them; press `b` to bring one under supervision, or type `/bg` inside it. One that has never been sent a message has nothing to keep: `d` closes its tab, `C` closes all such tabs, and the footer says so. Vantage ends the claude process, then ends the tab's shell with a clean exit code, which is what makes Windows Terminal close the tab; a session not inside a Windows Terminal tab is just ended.

## The new session wizard

Press `n`. The left column lists your repositories (recent ones first, then everything under the roots, then the roots themselves for a session with no repository); type to filter it and use the arrows to choose, then Enter confirms the repository and moves to the form. The right column is the form:

- **Session name (required):** the label shown in the NAME column. It also names the worktree folder and branch when you choose a worktree.
- **Where:** `New worktree`, the default, creates `<repository>-<name>` under the worktree root (`~\source\worktrees` unless you change it in [Settings](#settings)) on branch `<name>`, creating the worktree root the first time, copying the git-ignored files named in the repository's `.worktreeinclude` so builds work there too; `Current branch` runs the session in the repository as it is. Use the arrows to change the choice. A folder that is not a repository needs `Current branch`: Start refuses a worktree there.
- **Worktree base:** the branch the new worktree starts from, `origin` default branch or the current HEAD.
- **Opening prompt:** sent to the session as soon as it starts. It wraps as you type. Leave it blank and the session waits at its prompt until you send it something.

A cyan line under the choices says exactly what will happen ("Worktree Northwind.Orders-fix-tests under C:\Users\you\source\worktrees, on branch fix-tests."). Then:

- **F5 Start:** start the session and stay in Vantage.
- **F6 Start and open in tab:** start it and open it in its own Windows Terminal tab straight away, as `o` does; Vantage stays on screen.
- **Esc:** cancel.

Enter never starts a session, so there is nothing to undo if you press it too soon. If a repository has not been trusted by Claude Code yet, the start is refused with an explanation and Vantage offers to open a plain tab there so you can run `claude` once and accept the trust prompt.

## Attaching, tabs and getting back

**Attach in this tab (Enter):** Vantage hands the terminal to `claude attach`. Press Ctrl+Z to detach and return to Vantage; the session keeps running. A session that has never had a message opens at its own prompt, and Ctrl+Z returns from it in the same way. Vantage prints a one-line reminder before every attach and erases it when you come back, so the scrollback keeps no copies.

**Open in a tab (o):** Vantage opens a new Windows Terminal tab, titled with the session name and coloured from a palette so each session's tab is recognisable, and attaches there. The TAB column shows `open`. While that tab is open, `o` switches to it instead of opening a second one (Vantage finds the tab by its title through Windows UI Automation, so it works in whichever window holds the tab); detaching in it (Ctrl+Z), stopping, renaming or deleting the session closes the tab, because its script exits when `claude attach` ends cleanly; a failed attach keeps the tab open to show the error. Close a tab by hand and the column clears within a few seconds; `o` checks the tab's shell is still running before switching, and opens a new tab when it is not.

## The worktrees view

Press `w`. Vantage lists every linked worktree of the repositories under your roots, wherever its folder is (under the worktree root, or beside the repository as versions before 0.8.0 placed them), plus the leftovers git has forgotten under the roots and the worktree root. The first scan of a large roots folder takes a while (around forty seconds for fifty repositories); it refreshes every fifteen seconds after that.

```text
 Worktrees:  4 (3 ok, 1 broken)                               <s>       Start session     <?>     Help
 At risk:    1 with changes, 2 with unpushed commits          <o>       Open shell        <q>     Quit
                                                              <enter>   Go to session
  4 worktrees, selected: Northwind.Billing-invoice-pdf
 ╭──────────────────────────────────────────────────────────────────────────────────────────────────────────────────╮
 ││REPOSITORY          │FOLDER                        │BRANCH                   │DIRTY│UNPUSHED│LAST COMMIT│SESSION   │HEALTH │
 │ Northwind.Billing    Northwind.Billing-invoice-pdf   invoice-pdf                     12       3d4h       invoice-pdf ok    │
 │ Northwind.Billing    Northwind.Billing-study         study/reporting          5                9d16h                 ok    │
 │ Northwind.Orders     Northwind.Orders-fix-tests      fix-tests                        2        1d0h       fix-tests   ok    │
 │ Northwind.Sales      Northwind.Sales-republish       -                                         -                     broken│
```

| Column | Meaning |
| --- | --- |
| REPOSITORY, FOLDER, BRANCH | The owning repository, the worktree folder (relative to its root, as in the sessions view) and its branch |
| DIRTY | Changed or untracked files in the worktree |
| UNPUSHED | Commits on its branch that no remote has |
| LAST COMMIT | Age of the last commit |
| PR | The branch's pull request, as in the sessions view |
| SESSION | The session running inside it, if any |
| HEALTH | `ok`, `locked` (by another tool), `orphan` (a folder under `.claude\worktrees` git no longer lists), `broken` (a registration whose folder is gone, or a folder under a root or the worktree root whose registration or repository is gone), `empty` (a leftover `.worktrees` folder with nothing in it), `elsewhere` (a live worktree of a repository that is not under the roots, so Vantage never scanned it; the row names that repository, and the footer says where it is) |

When nothing else needs saying, the footer explains the selected row's health in full. Long repository, folder and branch names are cut with an ellipsis as in the sessions view, and Left and Right scroll the table.

| Key | Action |
| --- | --- |
| d | Remove the worktree, or delete a leftover folder. Refused, with the reason, when it has changed files, unpushed commits, a lock or a live session, and always for an `elsewhere` worktree, which is not a leftover: add its repository's root in Settings, or remove it with git there. A broken leftover also has its stale registration pruned from the repository. A folder a tab or program still has open is reported as in use, by name; what git could delete is gone, and a second `d` once it is closed finishes the job. Junctions and links inside a leftover (pnpm's `node_modules`, for one) are removed as links and never followed, so nothing outside the folder is touched. A leftover's files are counted up to 1,000, so a large one is refused as "at least 1000 files" |
| D | Force removal: the folder name opens selected in a read-only box, so Ctrl+C copies it (F2 also copies, from anywhere in the dialog); Tab or Enter moves to the answer box, Ctrl+V pastes, Enter confirms. There is no Remove button; a wrong or empty answer keeps the dialog open and says what was typed, and Esc cancels. Never forces a locked worktree, one with a live session, or an `elsewhere` one |
| P | Prune stale registrations in the row's repository (`p` before 0.11.0) |
| p | Open the row's pull request in your default browser, as in the sessions view |
| s | Start a session in this worktree (the wizard opens with the directory fixed) |
| o | Open a plain PowerShell tab in the worktree |
| Enter | Go to the session running in the worktree (selected by its id, so two sessions with the same name cannot be confused) |
| w, Esc | Back to the sessions view (Esc clears a filter first) |
| Left, Right | Scroll the table sideways, as in the sessions view |
| / | Filter by repository, folder, branch or health |
| c, e, v | Copy the folder path, open the folder in your editor, open its solution, as in the sessions view; refused for a leftover whose folder is gone |
| , | Settings, as in the sessions view |
| U | Update Vantage, as in the sessions view |

Branches: when a removed worktree's branch was created by Vantage and is merged, the branch is deleted too; when Vantage created it but it is not merged, or you created it by hand, Vantage asks before deleting it.

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

There are no toasts; the header's Need input count and the amber rows are the overview. Bell styles and sounds are set in [Windows Terminal setup](#windows-terminal-setup).

## Windows Terminal setup

Windows Terminal plays the bell with whatever sound the profile has, so a bell from Vantage sounds like every other notification until it has a profile of its own. `vantage setup-terminal` gives it two:

- **Vantage:** runs the installed `vantage`; open it from the new-tab menu to get Vantage in a tab that closes when you quit. It has no tab colour, so Vantage still colours its own tab.
- **Vantage session:** PowerShell. Every tab Vantage opens (`o`) uses it once it exists, so a bell from an attached session sounds different from Vantage's own.

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

**Undo.** `vantage setup-terminal --remove` deletes the fragment; restart Windows Terminal and the profiles are gone, and `o` opens tabs in your default profile again. Remove the `preferredNotifChannel` line from `~\.claude\settings.json` yourself if you want Claude's bell off.

## Common scenarios

**Start work on a story in a clean worktree.** `n`, choose the repository, name the session after the story (`New worktree` is already chosen), type the opening prompt, F6. The session opens in its own coloured tab and the overview stays where it is. The worktree is `<repository>-<name>` under the worktree root, and its FOLDER cell shows the worktree.

**Kick off several sessions and let them run.** Use `n` with F5 for each, giving each a distinct name so you can tell them apart. Leave Vantage open: rows turn amber when one needs you, and the Vantage tab rings the bell even when another tab is in front. Enter on the amber row, answer, Ctrl+Z.

**Keep one session in its own tab.** `o` opens it in a coloured tab you can leave open all day. The TAB column shows `open`; `o` again switches to that tab until it is closed.

**Start Claude from a repository folder you are already in.** `vh fix-tests -p "..."` instead of `claude`. It shows up in Vantage with a job id, so `o`, `x` and `d` work on it later.

**A session you started with plain `claude` needs the same treatment.** Select its `terminal` row and press `b` while it is idle or waiting. Vantage ends the plain process and resumes the same conversation under the supervisor with the same name; the old tab becomes an ordinary shell. A session you have not sent anything to yet is refused, because Claude writes no transcript until the first message and there would be nothing to resume: `d` closes that tab instead.

**After a reboot.** Sessions show as stopped or failed with no process. `R` resumes the failed ones and offers the stopped ones as a checklist.

**Finish a story and clean up.** `d` on the session deletes it and, if Vantage created the worktree and it is clean and pushed, offers to remove the worktree and its merged branch. Anything with uncommitted or unpushed work is refused with the reason.

**Find out what is cluttering the repositories folder.** `w` lists every worktree with dirty and unpushed counts and health. Empty leftover folders go with `d`; a worktree with uncommitted work needs `D` and the copied folder name, which is deliberate.

**Something needs Claude Code's own screen.** Run `claude agents` in any shell for the native agent view; Esc returns.

## Where Vantage keeps things

Everything Vantage owns is under `%LOCALAPPDATA%\Vantage`:

| File or folder | Contents |
| --- | --- |
| `config.json` | Your settings: `repositoryRoots` (default `~\source\repos`), `worktreeRoot` (default `worktrees` beside the first root, so `~\source\worktrees`), `editor` (default VS Code), `defaultBase` (`RemoteDefault` or `Head`) and `checkForUpdates` (default `true`); see [Settings](#settings) |
| `update-check.json` | When Vantage last asked nuget.org for a newer release of itself and the newest release it found; see [Updates](#updates) |
| `recent\` | One file per recently used repository, with when it was last used; the ten most recent are shown first in the wizard |
| `created.json` | Worktrees Vantage created, which decides the branch-deletion rules |
| `tabs\` | One file per tab Vantage opened, so a second `o` switches to the tab instead of opening another |

`vantage setup-terminal` also writes `%LOCALAPPDATA%\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json`, which belongs to Windows Terminal's fragment mechanism rather than to Vantage's own folder, and can set one line in `~\.claude\settings.json` (see [Windows Terminal setup](#windows-terminal-setup)).

A missing or corrupt file is treated as empty. Recent repositories are one file each so that `vantage here` and the console can record at the same time without losing a write; the `recent.json` file of versions before 0.3.0 is imported once and removed. The `descriptions\` folder and `sessions.json` left by earlier versions are no longer used and can be deleted. Sessions themselves belong to Claude Code (`claude agents --json`), never to Vantage.

## What Vantage trusts

Vantage trusts every repository under its roots: it runs git in each of them on every scan, copies the gitignored files a repository's `.worktreeinclude` names into new worktrees, and opens solutions with the application Windows associates with them, so a repository you would not open in an editor should not sit under a root.

It runs the Claude Code it finds at `~\.local\bin\claude.exe` or as `claude.exe` on the PATH, `git`, `gh` and `az` for repositories and pull requests, and `wt.exe` and `pwsh.exe` for tabs. Each is found in the PATH folders once, as Vantage starts, and run by its full path, so a program planted in the folder you started Vantage from is never run (Windows would otherwise look there first for a bare name). A `.cmd` or `.bat` is never run with a session name, a prompt or a path: Claude Code must be an executable, a custom editor's executable must be a full path to one, and the Azure CLI, which is a batch file, is given plain text only (see [Pull requests](#pull-requests)). An opening prompt that starts with a dash is refused rather than passed to Claude Code as options.

Its own state lives in `%LOCALAPPDATA%\Vantage`; it never edits Claude Code's trust records and stores no credential. Once a day it asks nuget.org for its newest version, which the setting or `VANTAGE_NO_UPDATE_CHECK` turns off, and the update tab installs from nuget.org alone. The diagnostic log that `VANTAGE_LOG` turns on records Terminal.Gui's internals, key events included, so a log you share may hold what you typed.

## Settings

Press `,` in either view. The dialog shows:

- **Repository roots:** one folder per line; the wizard offers every repository directly under each, and the worktrees view scans them. Each must be a full path that exists.
- **Worktree root:** where new worktrees go, as `<worktree root>\<repository>-<name>`. It must be a full path; it is created when the first worktree goes there. Worktrees made beside their repository by earlier versions stay where they are and are still listed, removed and opened as before.
- **Editor:** VS Code, Cursor, Rider, Visual Studio, Explorer, or Custom: the full path of an executable and its arguments, with `{folder}` where the folder goes (double quotes keep an argument with spaces together). A bare name or a relative path is refused, because Windows would look for it in the folder Vantage was started from before the PATH; a `.cmd` or `.bat` file is refused, because Windows runs it through `cmd.exe`, which would read the folder path as commands. The `e` key opens the selected row's folder in this editor.
- **Worktree base:** where a new worktree's branch starts by default, origin's default branch or the current HEAD.
- **Check for updates daily:** whether Vantage asks nuget.org once a day for a newer release of itself (see [Updates](#updates)); on unless you turn it off.

F5 saves: the dialog checks everything first, names any problem and keeps the dialog open; once saved, `config.json` is written and the next scan, wizard and header use the new values at once, without a restart. Esc closes the dialog without changing anything.

`config.json` can also be edited by hand while Vantage is closed (a hand edit made while it runs is overwritten by the next save from the dialog, and otherwise applies on the next start). A file from an earlier version keeps working: every property it leaves out reads as its default, and a custom editor without `{folder}`, or whose executable is not a full path to something other than a batch file, reads as VS Code.

```json
{
  "repositoryRoots": ["C:\\Users\\you\\source\\repos", "D:\\work"],
  "defaultBase": "RemoteDefault",
  "worktreeRoot": "C:\\Users\\you\\source\\worktrees",
  "editor": { "preset": "Custom", "executable": "C:\\Tools\\subl.exe", "arguments": ["-n", "{folder}"] },
  "checkForUpdates": true
}
```

## Updates

A dotnet global tool never updates itself, so Vantage looks for you. Once a day at most, in the background as Vantage opens, it asks nuget.org for the versions of `Source191.Vantage`; when a higher release exists the header's Vantage line says so, for example `Vantage: 0.9.0 (0.10.0 available, U to update)`. The check runs in the background with a 3 second timeout: a slow or failed request shows nothing, delays nothing and is tried again on the next start. Prerelease versions on nuget.org are ignored, and a local or CI build (a prerelease version itself) never checks.

`U` in either view asks "Quit Vantage and update to 0.10.0?". On yes Vantage opens a tab titled "Update Vantage" and quits; the tab waits for that Vantage to exit (a running `vantage.exe` locks its own files), runs `dotnet tool update -g Source191.Vantage --source https://api.nuget.org/v3/index.json` (so the package comes from the feed the check consulted, whatever other sources the machine has configured), and starts Vantage again when the update succeeds. When it fails, typically because another Vantage is still open in some tab, the tab stays open with dotnet's message: quit every Vantage and press `U` again, or run the command yourself. Background sessions run under Claude's supervisor and keep running throughout. With no newer release known, `U` says Vantage is up to date, or that the check is off.

**What the check sends:** one anonymous HTTPS request to `https://api.nuget.org/v3-flatcontainer/source191.vantage/index.json`, the public list of the package's versions, with a `Vantage/<version>` user agent and nothing else that identifies you or your machine. Turn it off with **Check for updates daily** in [Settings](#settings) or `"checkForUpdates": false` in `config.json`. Setting the environment variable `VANTAGE_NO_UPDATE_CHECK` to any non-empty value (for a script or test harness) turns it off without touching `config.json`: no request is made and no `update-check.json` is written. The hand-off check in `tools\HeadlessTerminal` sets it.

## Troubleshooting

- **Keys stop working after returning from a session.** Set `$env:VANTAGE_LOG = "$env:TEMP\vantage.log"` before starting Vantage; the log records every hand-off and Terminal.Gui's own diagnostics, key events included, so read it before sharing it. The known cause (blocking the UI thread after a hand-off) is fixed and guarded by the check in `tools\HeadlessTerminal`.
- **"not trusted yet".** Claude Code refuses to start in a folder you have not trusted. Open a tab there, run `claude`, accept the prompt, then start again. Vantage never edits the trust records.
- **No bell when a session needs you.** Run `vantage setup-terminal` and see [Windows Terminal setup](#windows-terminal-setup); it covers `preferredNotifChannel` in `~\.claude\settings.json` and the bell style and sound of the Windows Terminal profile. Claude only rings when it thinks you are away from the terminal.
- **Tabs or sessions opened from Vantage have no colours.** Vantage was started from inside a Claude Code session, whose environment sets NO_COLOR; start Vantage from an ordinary tab.
- **The worktrees view is slow to appear.** The first scan runs `git worktree list` for every repository under the roots and three git commands per worktree. Narrow `repositoryRoots` if the folder is very large.
- **Claude Code was not found.** Vantage looks for `~\.local\bin\claude.exe`, then `claude.exe` in a folder on the PATH.
- **Claude Code was found only as a batch file.** A `claude.cmd` on its own (an npm install's shim) is named and refused: Windows would run it through `cmd.exe`, which reads session names and prompts as commands. Install the native build with `irm https://claude.ai/install.ps1 | iex`.
- **git was not found on the PATH.** Vantage needs `git.exe` in a folder on the PATH (`winget install --id Git.Git --exact --source winget`, then a new terminal); a `git.cmd` does not count.
- **Windows Terminal (wt.exe) or PowerShell 7 (pwsh.exe) was not found.** The key that needs it says so in the footer; both must be in a folder on the PATH.

## Developing Vantage

```powershell
dotnet build -c Debug                                              # warnings are errors
dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build  # the fast in-memory suite, about two seconds
dotnet test -c Debug --no-build                                    # every suite, including real git and processes
dotnet format --verify-no-changes
dotnet run --project tools\HeadlessTerminal -- check               # the hand-off check, from tools\HeadlessTerminal
```

- Four projects under `src\`: `Source191.Vantage.Core` (`Domain` rules, `Ports` boundary interfaces, `Application` with the `Views` reducers, `Actions` and `Launch` use cases, the screen loop and command line), `Source191.Vantage.Infrastructure` (git, Claude CLI, Windows Terminal, processes, files, state), `Source191.Vantage.Tui` (Terminal.Gui rendering and dialogs) and `Source191.Vantage` (`Program.cs`, the only composition root, packed as the tool with the other three inside). Project references and an architecture test keep the dependencies pointing one way.
- Each project has a README with what is worth knowing about it, and its `.csproj` carries no comments (the root `Directory.Build.props` keeps its own): [Source191.Vantage](https://github.com/Source191/vantage/blob/main/src/Source191.Vantage/README.md) (including its package settings), [Core](https://github.com/Source191/vantage/blob/main/src/Source191.Vantage.Core/README.md), [Infrastructure](https://github.com/Source191/vantage/blob/main/src/Source191.Vantage.Infrastructure/README.md), [Tui](https://github.com/Source191/vantage/blob/main/src/Source191.Vantage.Tui/README.md), [Core.Tests](https://github.com/Source191/vantage/blob/main/test/Source191.Vantage.Core.Tests/README.md), [Infrastructure.Tests](https://github.com/Source191/vantage/blob/main/test/Source191.Vantage.Infrastructure.Tests/README.md), [TestDoubles](https://github.com/Source191/vantage/blob/main/test/Source191.Vantage.TestDoubles/README.md), [HeadlessTerminal](https://github.com/Source191/vantage/blob/main/tools/HeadlessTerminal/README.md) and [HeadlessTerminal.Tests](https://github.com/Source191/vantage/blob/main/tools/HeadlessTerminal.Tests/README.md).- Tests are sociable: real collaborators, fakes only at the boundaries (`test\Source191.Vantage.TestDoubles`). `Core.Tests` is the fast in-memory suite; `Infrastructure.Tests` uses real git, processes and files. Every key in the tables above is dispatched by a reducer test. The domain rules are mutation tested with Stryker.
- `CLAUDE.md` records the verified Claude Code CLI and Terminal.Gui facts Vantage depends on, and the rules for working on it; `openspec\` holds the specifications and changes (below).
- `tools\HeadlessTerminal` drives the built exe inside a pseudo-console with scripted keys, replays the screen as text and runs the hand-off check; it is how the hand-off bug was reproduced and how the screen in this README was rendered.
- A Release build of `vantage.exe` cannot be rebuilt while it is running; use Debug for the test loop and stop the running instance before `dotnet pack` (`tools\install-local.ps1` does both).

## Continuous integration

`.github\workflows\check.yml` runs on every pull request and every push to `main`, on a `windows-latest` runner because the code uses P/Invoke, ConPTY and `cmd.exe` in its integration tests. Its `check` job builds in Release (warnings are errors) with the runner image's .NET SDK, which `global.json` chooses, so no SDK is downloaded, runs the whole test suite against that build, checks whitespace with `dotnet format whitespace --verify-no-changes` (the build already fails on every style and analyser warning, so the full `dotnet format` of the local loop would only repeat it), packs the tool as `<Version>-ci.<run number>` and uploads the `.nupkg` as a workflow artefact named `Source191.Vantage-<that version>`, so a working package is downloadable from any commit and each run's package is a version the tool cache has never seen (see Install). A pull request that changes only documentation (files under `openspec\`, or `.md` files outside `.github\`, as `.github\scripts\Test-DocumentationOnly.ps1` decides) passes `check` after the version check without building; a push to `main` always runs every step. A new commit on a pull request cancels the run for the one before it; runs for pushes to `main` are never cancelled. Branch protection on `main` requires `check` on an up-to-date branch, so a red pull request cannot merge. Every change to `main` goes through a pull request; nobody can push to it directly. The "Owner approves main" ruleset requires a code owner's approval, and `.github\CODEOWNERS` names the owner for every path, so a pull request opened by anyone else (an app or Dependabot included) waits for that review. The owner merges their own pull requests through the ruleset's bypass (`gh pr merge --admin`), so auto-merge does not fire for them. The "Release tags" ruleset stops anyone but the owner moving or deleting a `v*` tag.

**Supply chain.** Every action the workflows use is pinned to the commit of its release, with the version in a comment, so a moved tag cannot change what runs; Dependabot updates the pins weekly, and the repository setting that requires such pins is turned on once every workflow on `main` carries them. `.github\workflows\codeql.yml` runs CodeQL over the C# (without a build) and over the workflows themselves on every pull request, every push to `main` and once a week; it is not a required check and skips documentation-only changes. Dependabot security updates are on, so a dependency with a known vulnerability gets a pull request at once (the build's NuGet audit fails on it anyway). The `CLAUDE_CODE_OAUTH_TOKEN` secret reaches the latest Claude Code CLI that the `handoff` and `claude-contract` jobs install from `claude.ai`, so it belongs to an account that has nothing else and is rotated like a password.

**Releases.** `.github\workflows\release.yml` runs on every push to `main` and is a no-op unless that push changed `Version` in `src\Source191.Vantage\Source191.Vantage.csproj`. When it did, it builds, tests and packs again, attaches a signed build provenance attestation to the package (`gh attestation verify <nupkg> --repo Source191/vantage` checks which commit and workflow built it), and publishes a GitHub Release tagged `v<version>` with the `.nupkg` attached; a version with a prerelease label (`0.16.2-preview.1`) is published as a GitHub prerelease, so it never becomes the Latest release. Bumping `Version` is therefore the release action, and every pull request that changes the packaged tool makes it, so each merge of a tool change is a release. A release is not published to nuget.org: that is a separate, manual step (below), so a package is tried by hand before it goes where a version can never be replaced.

**Publishing to nuget.org.** `.github\workflows\publish.yml` is run by hand (Actions, "publish", with the release tag, e.g. `v0.5.0`). It downloads that GitHub Release's `.nupkg` and pushes it to nuget.org without building anything, so what is published is the package that was built, tested and released from `main`. It needs no stored API key: the `NuGet/login` action swaps the run's GitHub identity for a one-hour key through a Trusted Publishing policy on nuget.org (owner `Source191`, repository `vantage`, workflow `publish.yml`, environment `nuget`), and the nuget.org profile name is the `NUGET_USER` repository secret. The job runs in the `nuget` environment, which accepts only runs started from `main` and waits for the owner to approve each one (Actions, the waiting run, Review deployments), so a copy of the workflow on a branch, or a run started by an app, never gets a key. nuget.org versions cannot be replaced, only unlisted, so a bad publish is unlisted and fixed by a higher patch version. A prerelease tag is published the same way; nuget.org leaves it out of search unless prereleases are included, and `dotnet tool install` takes it only with `--prerelease` or `--version`, which is how a version reaches nuget.org before it is offered to everyone.

**Version bumps.** The first step of the `check` job, on pull requests only, runs `.github\scripts\Assert-VersionBumped.ps1` against the two parents of the pull request's merge commit. It fails when the pull request changes anything under `src\`, or `Directory.Build.props`, `global.json` or `nuget.config`, without raising `Version` above `main`'s (compared as semantic versions, so 0.10.0 is above 0.9.0). Pull requests touching only docs, CI, `openspec\`, `test\` or `tools\` need no bump. Run it locally with `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD`. Versions follow [semantic versioning](https://semver.org/):

- **Patch:** fixes, refactors with no behaviour change, and changes to text.
- **Minor:** a new key or a new behaviour that keeps existing keys working.
- **Major:** a change that breaks how existing keys, state files or the `vantage here` command line behave. While the tool is `0.y.z`, a minor bump may break.

Branch protection on `main` has "Require branches to be up to date before merging" on. Without it a pull request's check can pass against an old `main`: two open pull requests that both bump to the same version merge without a conflict, and the second one is never released. With it, updating the branch reruns the check against the current `main`, and the second one fails until it bumps again.

**The hand-off job.** `check.yml` has a second job, `handoff`, that installs Claude Code on the runner and runs the hand-off check from `tools\HeadlessTerminal` against a real throwaway session. It needs a `CLAUDE_CODE_OAUTH_TOKEN` repository secret (from `claude setup-token`); without one it skips with a notice, as it does for a documentation-only pull request. It builds only the tool (`src\Source191.Vantage`), because `check` builds and runs the tests, and installs Claude Code in the background while it builds. It never blocks a merge, because its keystroke timings were tuned on a laptop and a slow runner can fail it for reasons that are not bugs; read its `handoff-capture` artefact when it does. The runner has no desktop, so it seeds workspace trust for the checkout in its own `~\.claude.json`, which, with the nightly contract check below, is the only place that record is ever written by a script. Vantage strips `CLAUDE_CODE_*` from the environment of the `claude` processes it starts (`InheritedEnvironment`), so the token reaches the throwaway session the harness starts but not Vantage's own `claude agents` and `claude attach`; the live runs show the attach reaches Claude's screen all the same. The check exits 3 when it does not: Claude's screen must be in the capture, not only Vantage's.

**The nightly Claude Code contract check.** Claude Code's native installer updates the CLI in the background, so users soon run a newer CLI than Vantage was verified against. `.github\workflows\claude-contract.yml` runs every night at 03:17 UTC, and by hand from Actions, "claude-contract": it installs the latest CLI, runs the `ClaudeContract` tests (Vantage's own Claude adapters against that CLI: the version, a background session started with no prompt, listed with every field Vantage reads in a state it knows, then stopped and removed; and the commands and options in `claude --help` and `claude agents --help` against the snapshots in `test\Source191.Vantage.Infrastructure.Tests\Contract\Snapshots`), then the hand-off check. It needs the same `CLAUDE_CODE_OAUTH_TOKEN` secret as `handoff` and seeds trust the same way; without the secret it skips with a warning. It never runs on pull requests, so a CLI release never blocks a merge.

- **The claude-cli issue:** when the latest CLI fails to install or a contract test fails, the workflow comments on the open issue labelled `claude-cli`, or opens one when none is open, with the CLI version, the run link and the failing tests' messages. It assigns a new issue to the GitHub login in the `CLAUDE_CLI_WATCHER` repository variable (Settings, Secrets and variables, Actions, Variables); an assignee is subscribed to the issue, so GitHub notifies them of every comment. Close the issue by hand once the fix is merged. The hand-off check marks the run but never raises the issue.
- **Accepting a snapshot:** a difference leaves `<name>.received.txt` in the run's `contract-results` artefact (and in the test output folder locally). Copy it over the snapshot in a change that re-checks the CLI facts in CLAUDE.md, so a new flag is either used, deliberately ignored or recorded.
- **Locally:** `$env:VANTAGE_CLAUDE_CONTRACT = 1; dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-trait "Category=ClaudeContract"` from a trusted repository. An ordinary `dotnet test` skips these tests, so it never adds a session to your `claude agents` listing.

### Manual checks for tab and attach changes

The runner drives Vantage inside a pseudo-console, which is the same plumbing Windows Terminal uses for a tab, so rendering, keys and the attach hand-off are covered. Anything that goes out through `wt.exe` is not: a runner has no desktop for a tab to appear on. Before merging a pull request that changes how Vantage drives `wt.exe`, opens, titles or colours tabs, or attaches to a session, install its package (`tools\install-local.ps1`) and run these in a real Windows Terminal:

1. `o` on a session opens a coloured tab titled with the session name, and `o` again switches to that tab while it is open.
2. `n`, choose a repository, name the session and leave `New worktree` chosen, F6: a new tab opens attached to the new session, and its FOLDER cell shows the worktree.
3. `d` on that session ends claude and the tab closes by itself.
4. A background session that pauses for input turns its row amber and rings the bell in the Vantage tab while another tab is in front.
5. Ctrl+Z from an attached session returns to the table with the keys still working.
6. The tab Vantage runs in is titled `Vantage` and green, reads `Vantage (Keys)` while `?` is open and `Vantage` again after Esc, keeps that title after attaching and pressing Ctrl+Z, and goes back to the profile's title and colour on `q`.

After a Terminal.Gui update, check the dialog title from `tools\HeadlessTerminal` without a terminal: `dotnet run -- drive <vantage.exe> '8000:?,2000:\u001b,2000:q'`, then the `ESC ] 0 ;` titles in `drive-raw.txt` must read `Vantage`, `Vantage (Keys)`, `Vantage`. Terminal.Gui writes every Runnable's title to the terminal unless it is told not to, so a new dialog that skips `DialogTabTitle.Apply` shows up here as its own title.

## Working with OpenSpec

The repository uses [OpenSpec](https://github.com/Fission-AI/OpenSpec) for spec-driven changes. `openspec\specs` describes what Vantage does today, one capability per folder (session listing, launch, attach, worktree management, needs-input notification). Each piece of work is a change under `openspec\changes\<name>` with a proposal, a design, the delta specs it adds or modifies, and a task list of red-green-refactor steps; `openspec\changes\archive` keeps finished changes.

- **Propose:** `/opsx:propose <name>` (or `openspec new change`) writes the proposal, design, delta specs and tasks for a feature or fix, in that order.
- **Apply:** `/opsx:apply <name>` works through the tasks test-first, ticking each one; `openspec status --change <name>` shows progress.
- **Archive:** `/opsx:archive <name>` when the tasks are done; the delta specs are merged into `openspec\specs` so they stay the single account of current behaviour.
- `openspec\config.yaml` holds the project context every change inherits (platform, CLI surface, worktree conventions, coding style); update it when a rule changes rather than repeating the rule in each proposal.

## Support Vantage

Vantage is built in the gaps between rate limits. Help close the gaps: [buy me a sliver of a Claude Max plan](https://buymeacoffee.com/simonvane).

![QR code for buymeacoffee.com/simonvane](https://raw.githubusercontent.com/Source191/vantage/main/assets/support/buy-me-a-coffee-qr.png)

## Roadmap and known limits

- Windows only, with Windows Terminal; tab colours and titles use `wt.exe`.
- Still to observe at the terminal: whether `claude attach` to one session from two tabs is refused or allowed, and whether `claude --bg --resume <id>` or `claude respawn --all` is the right way to bring sessions back after a reboot (the listing side is documented: no pid, state failed or stopped).
- A take-over key (stop the supervised session and resume it as a normal foreground `claude`, at the cost of supervision) is undecided; `b` covers the other direction.
- Multiple Claude accounts: Claude Code's `CLAUDE_CONFIG_DIR` selects a configuration directory with its own login, settings, sessions and supervisor; Vantage would gain a profile column and set the variable when starting a session.
- Installing with `winget install`, as a self-contained `vantage.exe` that needs no .NET SDK ([#93](https://github.com/Source191/vantage/issues/93)).
- A per-path cache for the worktree scan, so the first press of `w` is quick on a large roots folder.
- Pinning sessions, once the CLI exposes it outside the native agent view; configurable palette and Windows Terminal profile for the tabs Vantage opens.
