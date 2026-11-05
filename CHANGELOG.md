# Changelog

What changes in each release of Torni, written for the people who use it. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/). Internal lists changes nobody using Torni can see; Torni never shows it, or the Unreleased section, in its What's new dialog.

## Unreleased

### Added

- Installs with winget install Source191.Torni as a self-contained program, with no .NET to install first; winget brings Windows Terminal, PowerShell 7 and git when they are missing, and every release carries the zip for a machine without winget (#206)
- U opens a What's new dialog listing the changes in every release since the running one, with Update now, Release page and Later; a newly found release is announced once in the status line, and the first start after an update shows what changed, once (#180)
- Sessions, Worktrees and Config are tabs of one screen, each title carrying its count with the state counts and glyphs beside it, and a two-line key bar at the bottom (#114)
- A Config tab lists every setting and the setup it reads (settings file, terminal profiles, Claude's bell, diagnostic log, last update check); Enter edits a setting in a small dialog and saves at once, and setup that needs attention says what to run (#117)
- Left and Right, or a click on a title, move between the tabs; Esc on Worktrees or Config with no filter returns to Sessions (#117, #183)
- A detail pane under the Sessions and Worktrees tables shows the selected row's facts: what a session is waiting for, when it started and was last prompted, folder, session id, worktree, branch, tab, changed files, unpushed commits, last commit, memory and health with its reason (#119)
- The Config tab describes the selected setting, and a Setup heading separates the settings from the setup rows (#165)
- Browse (F3) in the repository roots and worktree root dialogs opens a folder picker inside the terminal (#125)
- A Default place setting, New worktree or Current branch, chooses where the new-session wizard starts (#125)
- m on a running session opens a breakdown of its memory: Claude Code, then each program it started (MCP servers, shells), largest first (#106)
- ! opens Report a problem: a preview of privacy-safe diagnostics that can be opened as a pre-filled GitHub issue in the browser or copied; nothing is sent automatically (#101)
- Recent diagnostic events are kept in memory and written to last-crash.log in %LOCALAPPDATA%\Torni after a crash (#100)
- Automated Claude runs (claude -p, the Agent SDK) are kept out of the Sessions table and shown in grey in its counts with their memory (#121)
- Every key and column has a tooltip, the help dialog scrolls, and < and > scroll a wide table sideways so every column can be reached (#98)
- The STATE column shows a glyph beside each state, and the TAB column a swatch in the tab's colour (solid for a session tab, hollow for a plain claude) (#113)
- The Worktrees table gains a HEALTH column with a glyph and word per health, and an unknown session state reads ? so it differs from stopped by shape (#164)
- The Worktrees tab is scanned as the console opens and rescanned quietly each time it is shown, so its rows are usually ready at once (#139)
- torni --help carries a support line (#88)
- Rider installed by JetBrains Toolbox 2.x is found; presets that cannot be found are marked (not found) in the Open folder with dialog; F3 there browses for a Custom editor executable (#178)
- With TORNI_LOG set, the log records refresh timing by part, UI stall counts, and typical durations and counts every 10 minutes (#179)

### Changed

- Vantage is now Torni: run torni, torni here and torni setup-terminal; install with winget install Source191.Torni; settings and state live in %LOCALAPPDATA%\Torni, the Windows Terminal profiles are Torni and Torni session, the variables are TORNI_LOG and TORNI_NO_UPDATE_CHECK, and the icon is a T. Nothing is carried over from %LOCALAPPDATA%\Vantage (#219)
- U upgrades with winget, and the daily update check asks the winget community repository, so a release is offered only once winget can install it (#206)
- The Worktrees table's LAST COMMIT column is right-aligned, as AGE is on the Sessions tab, so the durations line up (#190)
- Removing a worktree on a detached head, or whose head git could not name, deletes no branch and asks about none; the folder's name no longer stands in for a branch (#186)
- The update notice in the settings row is amber (#180)
- The header is one grey settings row with the console's version and memory and Claude Code's version; the repository roots and worktree root moved to the Config tab, so every table gains rows (#117)
- A status line appears under the settings row only when there is something to say, marked by its tone (✓ done, ! refused, ✗ failed, a spinner while working), wraps up to three rows and can be closed with ✕ (#129)
- Enter on a session opens it in its own Windows Terminal tab, or switches to that tab; the console never attaches in its own tab, and F5 in the wizard starts the session and opens its tab (#128)
- torni here says "Ctrl+Z detaches; the session keeps running." before it attaches (#128)
- f opens a row's folder and s its solution on every tab; on Worktrees n starts a session in the worktree and Enter opens the session already running there (#161)
- The Sessions table reads NAME, TAB, STATE, REPOSITORY, PR, AGE and the Worktrees table WORKTREE, HEALTH, LAST COMMIT, SESSION; the other facts moved to the detail pane (#119)
- The new-session wizard is one column read top to bottom and fits a 120 x 36 terminal (#122)
- The wizard's Tab and Enter follow the form's order, Enter never closes it, repository roots are no longer offered as choices, and the wizard for a worktree is compact; the solution chooser is a list (#105)
- R resumes every stopped or failed session the table shows, each in its own tab, without a dialog; a / filter narrows it (#163)
- A session's tab closes when its attach ends, and stop, delete, rename and adopt close or move the tab; opening a tab checks the old one is still alive (#102)
- Pull requests are looked up per branch for the rows on screen, refresh within about 75 seconds, run at most four lookups at once, and the Azure DevOps link is built from the remote (#99)
- Session names may hold any character, including ; and " (#84)
- The tab palette is reordered so the console's tab and the first session tabs stay apart under red-green colour blindness, and dialog fields are easier to read (#165)
- Grey text is lighter, for contrast (#164)
- The TAB swatch is drawn in the shade Windows Terminal shows for that tab while another is selected (#124)
- The Visual Studio preset opens the newest install found by vswhere (#125)
- A missing editor or solution application is reported in plain words and points to the Config tab (#178)
- torni --version prints exactly the package version, without the commit (#86)
- The package holds only Windows native assets and English resources (#92)

### Fixed

- A stop, removal or listing Claude Code refuses is reported in its own words: x keeps the tab, r, R and d resume, close and remove nothing after a refused removal, and torni here erases its detach hint and restores the terminal when the attach fails (#187)
- Deleting a session whose worktree's repository has gone, or that git cannot inspect, keeps the worktree and says why; a leftover folder a tab or program holds open is reported as in use; a junction or an unreadable subfolder inside a leftover no longer fails the Worktrees scan; a new worktree is recorded before its include files are copied, so a failed copy leaves it known to Torni; adopt and close-empty finish when part of a process tree has already gone, and close-empty closes a tab whose root process is claude (#186)
- State files are written beside and moved over and created worktrees are one file each, so an interrupted save keeps the previous settings and two running copies cannot lose each other's records; a config.json that cannot be read, or one holding a number for the default base or place, starts with the defaults instead of ending it (#185)
- A GitHub remote with a user name before github.com, as Git Credential Manager writes it, shows its branches' pull requests (#188)
- A session just stopped or deleted no longer comes back for a few seconds, rows that arrive while a dialog is open are kept when it closes, the spinner keeps turning until every action has reported and stays through a filter typed before the first listing, Esc clears the status message with the filter, and a child program that has not answered after 60 seconds is ended and reported instead of running on (#184)
- R continues each session in its own row instead of leaving a duplicate, and a session just started or resumed shows as starting rather than /blocked (#162)
- Times in the detail pane and the Config tab are shown in the local time zone; Rider's newest install is chosen by version number; changed files with non-ASCII names are listed as typed (#159)
- Ignored files with non-ASCII names, spaces or apostrophes are copied into a new worktree (#82)
- A folder holding a semicolon opens its tab correctly (#84)
- Quitting restores the tab's own colour, and the return hint before an attach is erased afterwards (#95)
- torni here -d x names -d as the unknown option (#95)
- A NUL character in a state file is read as a bad value rather than crashing at start-up (#166)
- Several status messages that named retired keys now name the current ones (#174)

### Removed

- No longer a dotnet tool on nuget.org: install with winget, and try a preview from the zip on its GitHub prerelease, which tools\install-local.ps1 -FromArtifact extracts (#206)
- The a key (native agent view); run claude agents in any shell instead (#95)
- The w and , keys and the settings dialog, replaced by the Config tab (#117)
- o on the Sessions tab, F6 in the wizard and starting a session without a tab (#128)
- The e and v keys, replaced by f and s (#161)
- The Cursor editor preset; a config.json naming Cursor reads as VS Code (#125)

### Security

- git, gh, wt.exe and pwsh.exe are found once on the PATH and run by full path, never from the current directory; the console exits when git is missing (#110)
- Claude Code must be claude.exe (a claude.cmd or claude.bat alone is refused), a custom editor must be a full path, and an opening prompt starting with - is refused (#110)
- Only safe branch names reach az, pull request links open only over https, close-empty and adopt check a process is still the session's, and the update tab installs from nuget.org only (#110)
- Deleting a leftover folder refuses a relative path, a drive root or a share root (#83)
- Leftover folders are deleted only inside a repository root or the worktree root, a prunable worktree that still holds files is refused, and a .git file pointing at a live checkout is never treated as a leftover (#166)
- Relative PATH entries are never searched, the tab shell is found only through real ancestors, and session ids from outside the program must have the CLI's shape (#166)
- Diagnostics never record paths, names, prompts or program output, and the TORNI_LOG file no longer carries free text (#100)

### Internal

- Guard rule: no null guards inside the solution, since nullable reference types carry null, and ArgumentNullException banned in src so the build keeps it that way (#207)
- Small repetition removed: one home each for the crash-line test, tab ownership, palette colours, the pull-request parse and the escape scanner; failed scans recorded once per folder (#199)
- RemovalDecision, WorktreeObservation, Session and RemoteRepository are closed hierarchies with their cases nested, held by an Architecture fact for Domain and Ports (#198)
- Ports take Domain values where one exists, a folder is deleted only through IFolderDeleter held by WorktreeRemover, and IDesktop.OpenAsync names what it throws, each guarded by a fact (#197)
- Every process read opens one limited handle, a process is ended only as a ConfirmedProcess its three checks mint, and a command line never leaves ProcessRegistry.UsageOf (#196)
- A session's listed name is a typed value, named or unnamed, decided once as the listing is parsed, and the Claude CLI port has one resume method (#195)
- TableBody's column sizing, tooltips and detail reading each in a class of their own, one middle ellipsis in TextFit, IScreenDefinition slimmed to a Tab and a TabRefresh, and the key bar's removed labels disposed (#194)
- Each view state holds its ListSelection directly; SelectionKey and the With copies are gone (#193)
- Every dialog title and button, the waiting words, the STATE words and the wizard's preview are declared in Wording and pinned by a fact, and a source fact keeps sentences out of the rest of Application (#191)
- Convention nets: a banned-symbols list for src, layering, reducer, handler, diagnostic-line, key-parity and source-text facts, and UnreachableException in the closed discards (#189)
- Smoke test of the packed tool in CI, its script built from Wording (#75)
- Prerelease versions are published as GitHub prereleases (#78)
- Repository guards for going public: code owner and approval for nuget.org publishing (#80)
- Faster CI checks and a documentation-only fast path (#96)
- Dependabot pull requests raise Version automatically (#107)
- CONTRIBUTING.md added as the contributor's guide (#115)
- C# 14 idioms held in .editorconfig (#116)
- The null programme: no null in Domain, Ports and Application, guarded by NullFreeModelsShould (#148)
- One MaybeNullWhen(false) shape for every Try method's out parameter (#175)
- Every class that can be sealed is sealed, guarded by SealedTypesShould (#158)
- One top-level type per file, guarded by OneTypePerFileShould (#167)
- Program.cs's decisions moved into tested Application classes (#177)
- CLAUDE.md split into path-scoped rule files, and comments citing external APIs link to their documentation (#152)
- README, CONTRIBUTING and CLAUDE.md corrected and trimmed before going public (#174)
- System.CommandLine 2.0.14 (#108)
- One ViewUpdate record and one RowAction for every tab reducer, and the console keys q, ?, U and ! answered once in SharedKeys (#192)

## 0.16.1 - 2026-10-27

Release notes start with the next release; earlier releases keep their notes on their GitHub Releases.
