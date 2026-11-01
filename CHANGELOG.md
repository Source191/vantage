# Changelog

What changes in each release of Vantage, written for the people who use it. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/). Internal lists changes nobody using Vantage can see; Vantage never shows it, or the Unreleased section, in its What's new dialog.

## Unreleased

### Added

- U opens a What's new dialog listing the changes in every release since the running one, with Update now, Release page and Later; a newly found release is announced once in the status line, and the first start after an update shows what changed, once (#180)
- Sessions, Worktrees and Config are tabs of one screen, each title carrying its count with the state counts and glyphs beside it, and a two-line key bar at the bottom (#114)
- A Config tab lists every setting and the setup Vantage reads (settings file, terminal profiles, Claude's bell, diagnostic log, last update check); Enter edits a setting in a small dialog and saves at once, and setup that needs attention says what to run (#117)
- Left and Right, or a click on a title, move between the tabs; Esc on Worktrees or Config with no filter returns to Sessions (#117, #183)
- A detail pane under the Sessions and Worktrees tables shows the selected row's facts: what a session is waiting for, when it started and was last prompted, folder, session id, worktree, branch, tab, changed files, unpushed commits, last commit, memory and health with its reason (#119)
- The Config tab describes the selected setting, and a Setup heading separates the settings from the setup rows (#165)
- Browse (F3) in the repository roots and worktree root dialogs opens a folder picker inside the terminal (#125)
- A Default place setting, New worktree or Current branch, chooses where the new-session wizard starts (#125)
- m on a running session opens a breakdown of its memory: Claude Code, then each program it started (MCP servers, shells), largest first (#106)
- ! opens Report a problem: a preview of privacy-safe diagnostics that can be opened as a pre-filled GitHub issue in the browser or copied; Vantage sends nothing itself (#101)
- Vantage keeps its recent diagnostic events in memory and writes them to last-crash.log in %LOCALAPPDATA%\Vantage when it crashes (#100)
- Automated Claude runs (claude -p, the Agent SDK) are kept out of the Sessions table and shown in grey in its counts with their memory (#121)
- Every key and column has a tooltip, the help dialog scrolls, and < and > scroll a wide table sideways so every column can be reached (#98)
- The STATE column shows a glyph beside each state, and the TAB column a swatch in the tab's colour (solid for a Vantage tab, hollow for a plain claude) (#113)
- The Worktrees table gains a HEALTH column with a glyph and word per health, and an unknown session state reads ? so it differs from stopped by shape (#164)
- The Worktrees tab is scanned as the console opens and rescanned quietly each time it is shown, so its rows are usually ready at once (#139)
- vantage --help carries a support line (#88)
- Rider installed by JetBrains Toolbox 2.x is found; presets that cannot be found are marked (not found) in the Open folder with dialog; F3 there browses for a Custom editor executable (#178)
- With VANTAGE_LOG set, the log records refresh timing by part, UI stall counts, and typical durations and counts every 10 minutes (#179)

### Changed

- Removing a worktree on a detached head, or whose head git could not name, deletes no branch and asks about none; the folder's name no longer stands in for a branch (#186)
- The update notice in the settings row is amber (#180)
- The header is one grey settings row with Vantage's version and memory and Claude Code's version; the repository roots and worktree root moved to the Config tab, so every table gains rows (#117)
- A status line appears under the settings row only when there is something to say, marked by its tone (✓ done, ! refused, ✗ failed, a spinner while working), wraps up to three rows and can be closed with ✕ (#129)
- Enter on a session opens it in its own Windows Terminal tab, or switches to that tab; the console never attaches in its own tab, and F5 in the wizard starts the session and opens its tab (#128)
- vantage here says "Ctrl+Z detaches; the session keeps running." before it attaches (#128)
- f opens a row's folder and s its solution on every tab; on Worktrees n starts a session in the worktree and Enter opens the session already running there (#161)
- The Sessions table reads NAME, TAB, STATE, REPOSITORY, PR, AGE and the Worktrees table WORKTREE, HEALTH, LAST COMMIT, SESSION; the other facts moved to the detail pane (#119)
- The new-session wizard is one column read top to bottom and fits a 120 x 36 terminal (#122)
- The wizard's Tab and Enter follow the form's order, Enter never closes it, repository roots are no longer offered as choices, and the wizard for a worktree is compact; the solution chooser is a list (#105)
- R resumes every stopped or failed session the table shows, each in its own tab, without a dialog; a / filter narrows it (#163)
- A session's tab closes when its attach ends, and stop, delete, rename and adopt close or move the tab; opening a tab checks the old one is still alive (#102)
- Pull requests are looked up per branch for the rows on screen, refresh within about 75 seconds, run at most four lookups at once, and the Azure DevOps link is built from the remote (#99)
- Session names may hold any character, including ; and " (#84)
- The tab palette is reordered so Vantage's tab and the first session tabs stay apart under red-green colour blindness, and dialog fields are easier to read (#165)
- Grey text is lighter, for contrast (#164)
- The TAB swatch is drawn in the shade Windows Terminal shows for that tab while another is selected (#124)
- The Visual Studio preset opens the newest install found by vswhere (#125)
- A missing editor or solution application is reported in Vantage's own words and points to the Config tab (#178)
- vantage --version prints exactly the package version, without the commit (#86)
- The package holds only Windows native assets and English resources (#92)

### Fixed

- Deleting a session whose worktree's repository has gone, or that git cannot inspect, keeps the worktree and says why; a leftover folder a tab or program holds open is reported as in use; a junction or an unreadable subfolder inside a leftover no longer fails the Worktrees scan; a new worktree is recorded before its include files are copied, so a failed copy leaves it known to Vantage; adopt and close-empty finish when part of a process tree has already gone, and close-empty closes a tab whose root process is claude (#186)
- State files are written beside and moved over and created worktrees are one file each, so an interrupted save keeps the previous settings and two Vantage processes cannot lose each other's records; a config.json Vantage may not read, or one holding a number for the default base or place, starts Vantage with the defaults instead of ending it (#185)
- A session just stopped or deleted no longer comes back for a few seconds, rows that arrive while a dialog is open are kept when it closes, the spinner keeps turning until every action has reported and stays through a filter typed before the first listing, Esc clears the status message with the filter, and a child program that has not answered after 60 seconds is ended and reported instead of running on (#184)
- R continues each session in its own row instead of leaving a duplicate, and a session just started or resumed shows as starting rather than /blocked (#162)
- Times in the detail pane and the Config tab are shown in the local time zone; Rider's newest install is chosen by version number; changed files with non-ASCII names are listed as typed (#159)
- Ignored files with non-ASCII names, spaces or apostrophes are copied into a new worktree (#82)
- A folder holding a semicolon opens its tab correctly (#84)
- Quitting restores the tab's own colour, and the return hint before an attach is erased afterwards (#95)
- vantage here -d x names -d as the unknown option (#95)
- A NUL character in a state file is read as a bad value rather than crashing at start-up (#166)
- Several status messages that named retired keys now name the current ones (#174)

### Removed

- The a key (native agent view); run claude agents in any shell instead (#95)
- The w and , keys and the settings dialog, replaced by the Config tab (#117)
- o on the Sessions tab, F6 in the wizard and starting a session without a tab (#128)
- The e and v keys, replaced by f and s (#161)
- The Cursor editor preset; a config.json naming Cursor reads as VS Code (#125)

### Security

- git, gh, wt.exe and pwsh.exe are found once on the PATH and run by full path, never from the current directory; Vantage exits when git is missing (#110)
- Claude Code must be claude.exe (a claude.cmd or claude.bat alone is refused), a custom editor must be a full path, and an opening prompt starting with - is refused (#110)
- Only safe branch names reach az, pull request links open only over https, close-empty and adopt check a process is still the session's, and the update tab installs from nuget.org only (#110)
- Deleting a leftover folder refuses a relative path, a drive root or a share root (#83)
- Leftover folders are deleted only inside a repository root or the worktree root, a prunable worktree that still holds files is refused, and a .git file pointing at a live checkout is never treated as a leftover (#166)
- Relative PATH entries are never searched, the tab shell is found only through real ancestors, and session ids from outside Vantage must have the CLI's shape (#166)
- Diagnostics never record paths, names, prompts or program output, and the VANTAGE_LOG file no longer carries free text (#100)

### Internal

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

## 0.16.1 - 2026-10-27

Release notes start with the next release; earlier releases keep their notes on their GitHub Releases.
