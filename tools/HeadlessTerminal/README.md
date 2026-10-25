# HeadlessTerminal

Drives a console program inside a Windows pseudo-console (ConPTY) with scripted keystrokes and captures everything it writes, so hand-off bugs can be reproduced and screens read as text without touching the real terminal. A small .NET 10 console project, one class per job: `KeystrokeScript` (the script format), `TerminalQueries` (the Windows Terminal-like query responder), `PseudoConsole` and `NativeMethods` (ConPTY), `Driver` (the run), `ScreenReplay` and `MarkerCount` (reading a capture), `ClaudeJobs` and `HandoffCheck` (the repeatable check).

The repeatable check is `check`: it starts its own throwaway background session, attaches, detaches with Ctrl+Z, opens and cancels the wizard and quits; exit code 0 means Vantage quit by itself within the deadline, 1 means it had to be killed (a blocked UI thread). It stops and removes the throwaway session afterwards and never touches your sessions. Run it after any change to the hand-off code, the dialogs or Terminal.Gui.

```powershell
# from this folder
dotnet run -- check                                            # the Release build in this repository, in this repository
dotnet run -- check C:\path\to\vantage.exe C:\path\to\trusted\repo

# the pieces: scripted keys ("delayMs:keys,..." with JSON escapes), a screen replay, marker counts
$env:HARNESS_PLAIN = 1; $env:HARNESS_RESPOND = 1
dotnet run -- drive "$env:USERPROFILE\.dotnet\tools\vantage.exe" '8000:w,60000:\u001b[B,2000:D,3000:x,1000:\r,2000:\u001b,2000:q'
dotnet run -- grid drive-raw.txt "worktrees, selected" 120 40
dotnet run -- count drive-raw.txt "Not the same: you typed x." "Cancelled; nothing removed."
```

- `drive` writes the capture to `drive-raw.txt` in the current folder and exits 1 when the program had to be killed at the deadline (six seconds after the last key). Keys sent before Terminal.Gui has initialised (up to about eight seconds on a cold start) are lost, so scripts start at 8000 ms.
- HARNESS_RESPOND=1 answers terminal queries the way Windows Terminal would (kitty keyboard flags, primary device attributes, XTVERSION, cursor position, background colour); HARNESS_DCS_DELAY_MS delays the XTVERSION reply so it lands after the child has exited.
- HARNESS_PLAIN=1 leaves WT_SESSION and TERM_PROGRAM unset; without it the harness pretends to be Windows Terminal.
- `grid` replays the capture up to just after the last marker (or the alternate-screen exit) into a 120 by 40 character grid; `count` strips escape sequences and counts plain-text markers.
- A scripted answer in a force-remove dialog must never be the real folder name: a wrong answer is refused, a right one deletes.
- The pure parts (script parsing, query replies, command-line quoting, escape stripping, marker counting, screen replay) are unit tested in `test\Source191.Vantage.UnitTests\HeadlessTerminal`.
