## Context

`Desktop.OpenAsync` calls `Process.Start` with `UseShellExecute = true`. `ProcessStartInfo.Environment` cannot be combined with that flag (.NET throws), so the cleaning `ProcessRunner.StartInfo` does for every other child cannot apply, and ShellExecute starts the associated application from the calling process's environment. The editor key avoids the problem by starting the editor executable directly through `Desktop.StartAsync`; a solution has no executable of its own, only an association.

`OpenAsync` also serves the pull request key (a URL, opened in the default browser), so whatever is chosen applies to both.

## Goals / Non-Goals

**Goals:** the application opened for a solution inherits no variable `InheritedEnvironment` drops; correct for every legal path (`& ^ % ( ) ! ; , = [ ] '` and non-ASCII); a failure to open is still reported; `InheritedEnvironment` stays the only place that knows the names; the port and the handlers do not change.

**Non-Goals:** choosing the application, waiting for it, or changing the editor key.

## Decisions

Candidates were tried against a throwaway file type (`.vtgexp`) and URL protocol (`vtgexp:`) registered under `HKCU\Software\Classes` for the experiment (and removed afterwards) whose handler is `pwsh -NoProfile -File dump.ps1 "%1"`, a script that writes the handler's own environment names and its argument to a file. Nothing that opens a window of an IDE, browser or Explorer was run. The caller held `NO_COLOR`, `CLAUDECODE`, `CLAUDE_PID` and fourteen `CLAUDE_CODE_*` variables. Test files were named `plain`, `a & b`, `a ^ b`, `a %PATH% b`, `a (1) 'q' !x! ;,=`, `a[1]` and `é☃ x`.

- **Chosen: a cleaned PowerShell child performs the ShellExecute.** `Desktop` takes the `IProcessRunner` Program.cs already builds and runs `pwsh.exe -NoProfile -NonInteractive -EncodedCommand <script>` through `RunAsync`, so `ProcessRunner.StartInfo` drops the markers exactly as for every other child and gives it no console window. The script builds the same `ProcessStartInfo` with `UseShellExecute = true` that `Desktop` builds today and starts it; ShellExecute therefore runs inside a process that never had the markers. Measured: all seven names opened, the handler's environment held none of the markers (empty list), and a missing file failed in about 430 ms with exit code 1 and no dialog (.NET starts ShellExecute with its no-UI flag). A URL (`vtgexp://host/item?a=1&b=%25x&c=d`) went through the same way with no markers. The script catches an error, writes its message to standard error as UTF-8 and exits 1; `Desktop` waits for the child (about 0.4 s, it returns when ShellExecute does, not when the application closes) and throws `InvalidOperationException` with that message on a non-zero exit, so the footer still says why nothing opened, as the `Win32Exception` did.
- **The path never becomes script text.** It enters the script as the Base64 of its UTF-8 bytes in a single-quoted literal, decoded in the script. The Base64 alphabet holds no character PowerShell or any shell reads specially, so there is nothing to quote, for any path or URL. This is the only value in the script. It is a second route into a PowerShell script beside `PowerShellScript.Quote`, which lives in `Application`, a layer `Adapters` may not reference (`LayeringShould`); it needs no quoting rules, which is the reason for it.
- **Rejected: change Vantage's own environment around `Process.Start`.** Tried: the markers removed from the process for four seconds around the call. The handler still received all of them. ShellExecute does not read the caller's live environment block, so this cannot work (and it would be process-wide state touched from a background thread).
- **Rejected: `cmd.exe /d /c start "" "<path>"` with the cleaned environment.** Cleaning worked and, with the path passed through an environment variable and `/v:off`, the metacharacters and `%` were safe for all seven names. But `start` of a file that does not exist shows a "cannot find" dialog from the hidden cmd and blocks: the run never finished and left a cmd process with the path as its window title until it was killed. A solution deleted between the listing and the key, or a type with no association, would leave a hidden dialog and a stuck process, and the failure could not be reported. The carrier variable also reached the opened application.
- **Rejected: `explorer.exe <path>`.** Not run, because a failure opens an Explorer window; which environment the application then gets (the cleaned child's or the running shell's) was not measured. Explorer also gives no exit code to report a failure from, and reads `,` and `/` in its argument.
- **Rejected: resolve the association ourselves (`AssocQueryString`) and start its command line.** It cannot honour DDE and `DelegateExecute` handlers, which Visual Studio's and the browsers' associations may be.
- **Rejected: Vantage re-running itself as the launcher.** Correct, but needs a new command-line verb and its tests for something a PowerShell child already does, and `vantage here` is a documented surface.
- **Rejected: pass the path in an environment variable of the child.** It would reach the opened application, as the `cmd` variable did.

## Risks / Trade-offs

- [Foreground] The application is now started by a windowless child, not by Vantage, so Windows may let the IDE's window flash in the taskbar instead of coming forward. This cannot be checked without opening an IDE here, so it is a manual check on the pull request.
- [Cost] About 0.4 s and one short-lived `pwsh` per `v` or `p`; PowerShell 7 is already required for every tab.
- [Pull request key] Its browser is cleaned too. A browser already running takes the URL itself and keeps its own environment either way, so the visible effect is only on a browser that Vantage starts fresh.
- [Script injection] Excluded by construction (Base64), and a test with the shell characters above runs against real `pwsh` and a real ShellExecute.

## Verification

`DesktopShould` runs real `pwsh` with `CLAUDECODE` set in the test process, opening a `.cmd` file (a harmless batch file whose one line writes its own environment to a report in a temporary folder) under names holding `' ( ) & ^ % ! ;` and `é☃`, and checks that the report has no marker and still has PATH, that a missing file throws with a message, and that the script holds no part of the path in clear. The registered-handler experiment above covered `&`, `^`, `%`, `[ ]` and a URL end to end; tests do not register file types on a developer machine.
