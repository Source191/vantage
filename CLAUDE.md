# Vantage

A Terminal.Gui console for Claude Code sessions and git worktrees on Windows, packaged as the dotnet global tool `vantage`. The README is the user guide; `openspec/` holds the specifications and the changes made against them.

## Build, test, release

- Test loop: `dotnet build -c Debug` (warnings are errors), `dotnet test test\Source191.Vantage.UnitTests -c Debug --no-build` (xunit.v3 on Microsoft.Testing.Platform), `dotnet format --verify-no-changes`.
- Release: bump `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` (the tool cache keeps a version for ever), `dotnet pack src\Source191.Vantage -c Release`, `dotnet tool update -g Source191.Vantage --add-source .\artifacts\packages`. Stop a running `vantage.exe` first: a running exe locks its output and `dotnet tool update` fails.
- Hand-off check after any change to attach, dialogs or Terminal.Gui: `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal` (starts and removes its own throwaway session; exit 1 means a blocked UI thread). The same tool has `drive` (scripted keystrokes into a ConPTY pseudo-console, capture to drive-raw.txt), `grid` (replay a capture as a screen) and `count` (plain-text markers). Keys sent before Terminal.Gui has initialised (about 8 s on a cold start) are lost, so scripts start at 8000 ms.
- Mutation testing: Stryker with `--test-runner mtp` (xunit.v3 scores 0 % under the VSTest bridge), scoped with `--mutate "**/Domain/*.cs"` style globs, never solution-relative paths.
- Never start `vantage`, or anything that opens tabs, from inside a Claude Code session without clearing NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_*: Windows Terminal hands the caller's environment to new tabs and everything in them runs monochrome. Vantage drops those from its own children (`InheritedEnvironment`), but the Vantage process itself inherits whatever launched it.

## Code rules

- TDD: red, green, refactor, a commit per step; no untested code except `Program.cs` and wiring. Sociable tests; fakes only at boundaries (`test\...\TestDoubles`: process runner, process registry, terminal, transcripts, git roots).
- Concrete types, primary constructors, records, collection expressions, file-scoped namespaces, TimeProvider, braces always, British English in text. Reducers (`Screens\*View.cs`) are pure functions of state and input; everything a key does is a tested effect; the Terminal.Gui layer (`Screens\Tui`) only renders and runs effects.
- Test fixtures use made-up Northwind.* repository names and `C:\Users\me` paths; never real repositories or people.
- Commit only when asked.

## Claude Code CLI facts (verified 2026-10-23, CLI 2.1.309)

- `claude agents --json [--all]` is the scripting surface. Fields: pid, id, cwd, kind (interactive or background), startedAt, sessionId, name, status (busy, idle, waiting), state (working, blocked, done, failed, stopped), waitingFor. The job `id` is the first 8 characters of `sessionId`. A stopped session is a row without pid or status.
- `claude --bg -n <name> [prompt]` starts a supervised session; with no prompt it idles on the agent list. `attach` accepts a name, `stop` and `rm` need the id. Attaching to a session with no messages lands on Claude's agent list (Esc leaves it, Space sends a prompt); Ctrl+Z only detaches from a conversation.
- A session's transcript `~\.claude\projects\<cwd-encoded>\<sessionId>.jsonl` is written on the first message. Resuming a session with no transcript fails with "source session not found", so adopt (`b`), rename (`r`), attach and resume all check `ITranscripts` first.
- `claude --bg --resume <sessionId> -n <name>` continues a *non-background* transcript under the supervisor with the same id (how `b` adopts a terminal session after ending its process). For a *listed background* job the same command with flags starts a copy, as the docs say ("keeps its own saved options"); `claude rm <id>` first (the transcript stays on disc) and the resume then continues the same id under the new name, which is how `r` renames. Never run `claude --bg --resume --help`: it starts a session.
- Interactive sessions register in `~\.claude\sessions\<pid>.json`; they cannot be attached, stopped or removed from outside. Windows Terminal closes a tab when its root shell exits with code 0 and keeps it after a kill, so `d` and `C` end claude and then end the tab's shell cleanly (`TerminalTab`, `IProcessRegistry.EndCleanly`).
- Workspace trust is per repository (linked worktrees inherit it), granted only interactively; `claude --bg` in an untrusted folder exits with "Workspace not trusted". Never script edits to the trust records.
- Background sessions do not commit or push on their own (bgIsolation none, sibling worktrees). permission_prompt and idle_prompt notifications fire unattended; agent_needs_input and agent_completed fire only while the native agent view is open, which is why Vantage rings its own bell for background rows.
- `"preferredNotifChannel": "terminal_bell"` in settings.json makes Claude ring the terminal bell when it finishes a task or pauses for a permission prompt while the user appears away, which flags the tab showing a session. Claude Code writes `settings.json` with bare LF; anything that rewrites it must keep that.
- On detach Claude leaves focus reporting on and the cursor hidden and sends a late terminal-version query; `ConsoleRecovery` resets these after every child.

## Terminal.Gui 2.5.0 facts

- Instance API: `Application.Create().Init()`, `app.Run(window)`, `app.RequestStop`, `app.Invoke`, `app.AddTimeout`; set `app.Driver.Force16Colors = false` or the palette collapses to 16 colours.
- Never block the UI thread on an awaited call (`GetResult()`): Terminal.Gui installs a synchronisation context and the continuation deadlocks. It only showed after an attach-and-return; the harness check exists because of it.
- Focus roles: a scheme with Focus equal to Normal hides focus everywhere; ListView draws the selected row with Focus when focused and Active otherwise; a TextField needs its own background or it is invisible when empty, and a read-only TextField is drawn with the ReadOnly role once it loses focus.
- Dialogs: a Button with IsDefault takes the initial focus; set focus from the dialog's `Initialized` event, not before `Run`. A Dialog of Height 12 has 8 usable rows and the button row overdraws the last. A Label that receives text later needs a fixed Height and non-empty initial Text. A TextField's Enter raises its own `Accepting`; KeyDown handlers on the field or dialog do not see it. Ctrl+C in a TextField copies its selection through `app.Clipboard`, which Windows Terminal passes through when it has no selection of its own.
- `TextView` is obsolete (pragma CS0618) but still the wrapping multi-line box. `OptionSelector` replaces RadioGroup. Table rows take colours through `TableView.Style.RowColorGetter`.
- User text must never be interpolated into PowerShell source: session names reach `wt.exe` tab commands as an encoded script and Base64 arguments.

## Open items

- Spike 0.2 (attach to one session from two tabs) and 0.3 (which of `--bg --resume` or `respawn --all` brings sessions back after a reboot) still need observation at the terminal; the listing side is documented.
- A take-over key (stop, then `claude --resume` in a tab, normal foreground UI, leaves supervision) is undecided.
- The roadmap in the README lists the planned enhancements.
