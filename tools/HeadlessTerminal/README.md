# HeadlessTerminal

Drives a console program inside a Windows pseudo-console (ConPTY) with scripted keystrokes and captures everything it writes, so hand-off bugs can be reproduced and screens read as text without touching the real terminal. A small .NET 10 console project, one class per job: `KeystrokeScript` (the script format), `TerminalQueries` (the Windows Terminal-like query responder), `PseudoConsole` and `NativeMethods` (ConPTY), `CapturedRun` (one program running with its capture), `Driver` (fixed-delay keystrokes), `StepRunner` and `CaptureMarker` (steps that wait for a title or text before sending keys), `ScreenReplay` and `MarkerCount` (reading a capture), `ClaudeJobs` and `HandoffCheck` (the hand-off check), `SmokeScript` and `SmokeCheck` (the smoke check). It references `Source191.Vantage.Core` for `Wording`.

The repeatable check is `check`: it starts its own throwaway background session, attaches, detaches with Ctrl+Z, opens and cancels the wizard and quits; exit code 0 means Vantage quit by itself within the deadline and Claude's own screen is in the capture, 1 means it had to be killed (a blocked UI thread), 2 that the check could not start, and 3 that Vantage quit but Claude's screen (its `Claude Code v` banner) never appeared during the hand-off. It stops and removes the throwaway session afterwards and never touches your sessions. Run it after any change to the hand-off code, the dialogs or Terminal.Gui.

The smoke check is `smoke <exe> <worktree-folder>`: it runs an installed Vantage and opens and cancels everything a machine with no sessions can reach, the Keys help, the new-session wizard and the problem report on the sessions view, the first repository root's dialog on the Config tab and its folder picker (F3), then the worktrees view's help, the force-remove confirmation, the fixed-folder wizard and the solution chooser for the named worktree, and quits. The worktree must be a linked worktree under a repository root with no session in it, or force remove is refused with no dialog. Each step waits for its marker (a dialog's tab title from `Wording.TabTitleDuring`, a column only that view has, the worktrees title's ", selected: " once the scan has finished) and its keys are looked up by their hints in `Wording.SessionsKeys`, `Wording.WorktreesKeys` and `Wording.ConfigKeys` (a tab key, which has no hint, by the help line naming its tab), so a renamed title or a rebound key needs no change here. Exit code 0 means every step's marker appeared and Vantage quit by itself, 1 that it had to be killed, 2 that there is no exe, and 4 that a marker never appeared, with the step named. In a dialog it only ever presses Esc, and F3 to open the folder picker it then cancels, so it is safe against your own repositories, and like `check` it needs NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_* cleared when started from a Claude Code session. CI runs it in `check` against the packed tool, installed into a temporary tool path.

```powershell
# from this folder
dotnet run -- check                                            # the Release build in this repository, in this repository
dotnet run -- check C:\path\to\vantage.exe C:\path\to\trusted\repo
dotnet run -- smoke "$env:USERPROFILE\.dotnet\tools\vantage.exe" Northwind.Orders-fix-123   # an idle worktree of yours

# the pieces: scripted keys ("delayMs:keys,..." with JSON escapes), a screen replay, marker counts
$env:HARNESS_PLAIN = 1; $env:HARNESS_RESPOND = 1
dotnet run -- drive "$env:USERPROFILE\.dotnet\tools\vantage.exe" '8000:w,60000:\u001b[B,2000:D,3000:x,1000:\r,2000:\u001b,2000:q'
dotnet run -- grid drive-raw.txt "worktrees, selected" 120 40
dotnet run -- count drive-raw.txt "Not the same: you typed x." "Cancelled; nothing removed."
```

- `HARNESS_COLUMNS` and `HARNESS_ROWS` set the pseudo-console's size for `drive` (120 by 40 when unset), for checking a layout at another terminal size; give `grid` the same size.
- `drive` writes the capture to `drive-raw.txt` in the current folder and exits 1 when the program had to be killed at the deadline (six seconds after the last key). Keys sent before Terminal.Gui has initialised (up to about eight seconds on a cold start) are lost, so scripts start at 8000 ms.
- HARNESS_RESPOND=1 answers terminal queries the way Windows Terminal would (kitty keyboard flags, primary device attributes, XTVERSION, cursor position, background colour); HARNESS_DCS_DELAY_MS delays the XTVERSION reply so it lands after the child has exited.
- HARNESS_PLAIN=1 leaves WT_SESSION and TERM_PROGRAM unset; without it the harness pretends to be Windows Terminal.
- `grid` replays the capture up to just after the last marker (or the alternate-screen exit) into a 120 by 40 character grid; `count` strips escape sequences and counts plain-text markers.
- A scripted answer in a force-remove dialog must never be the real folder name: a wrong answer is refused, a right one deletes.
- A comma separates `drive` steps, so a comma key is written `\u002c`.
- The pure parts (script parsing, query replies, command-line quoting, escape stripping, marker counting and matching, screen replay, the smoke script) are unit tested in `tools\HeadlessTerminal.Tests`, with the step runner and the pseudo-console driven against short `pwsh` children.

Every Vantage the tool starts gets `VANTAGE_NO_UPDATE_CHECK=1`, so a run never contacts nuget.org or writes `update-check.json`.
