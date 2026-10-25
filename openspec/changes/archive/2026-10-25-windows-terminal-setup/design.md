# Design

## Context

See proposal.md. Today `WindowsTerminalArguments.ForTab` builds `wt -w 0 new-tab --title --tabColor --suppressApplicationTitle --startingDirectory pwsh.exe -NoExit -EncodedCommand ...` for all three kinds of tab (`TabScripts.Attach`, `Trust` and `Shell`), with no `--profile`, so every tab uses the user's default profile. `CommandLine.Parse` knows `here` and `claim-tab`; `Program.cs` handles `claim-tab` before Claude Code is located and `here` after. Windows Terminal reads JSON fragments from `%LOCALAPPDATA%\Microsoft\Windows Terminal\Fragments\<app>\*.json` at start-up; a fragment can add profiles (each must have a name) but cannot change `profiles.defaults`. Fragments must be UTF-8. `bellSound` is a per-profile path (or list of paths) and applies when `bellStyle` includes audible. Claude Code writes `~\.claude\settings.json` with bare LF (CLAUDE.md).

## Goals / Non-Goals

**Goals:** a one-command, reversible setup that never rewrites a file the user edits by hand except the one Claude Code setting they agree to; the tab profile choice made in one place for every tab Vantage opens; every sentence the command prints in `Wording`.

**Non-Goals:** detecting whether Windows Terminal has loaded the fragment (it cannot be asked); a profile name or bell style option; installing sounds.

## Decisions

- **A fragment, not an edit to settings.json.** Windows Terminal's settings file is JSON with comments; rewriting it with System.Text.Json drops the comments and reorders nothing safely. A fragment is Microsoft's documented way for an installed program to add profiles, and deleting its folder undoes it. Alternative rejected: a script that patches `profiles.list`, which would also need to find the right settings file among the Store, Preview and unpackaged locations.
- **Two profiles, named once.** `TerminalProfiles` in `Ports` holds the two names ("Vantage", "Vantage session"), the bell style (`audible`, `window`, `taskbar`), the Vantage executable path and the two optional sounds. The fragment adapter serialises it and `WindowsTerminalArguments` reads the session profile name from it, so the name cannot drift between what is written and what `wt` is asked for.
- **The "Vantage" profile runs `vantage.exe` directly** (the path Program.cs already has as `vantageExecutable`, quoted in `commandline`), with no `tabColor` (an explicit colour stops Vantage colouring its own tab, CLAUDE.md) and no `suppressApplicationTitle` (Vantage titles its tab itself). Quitting Vantage exits 0, so Windows Terminal closes the tab. Alternative considered: `pwsh -NoExit -Command vantage`, which leaves a shell behind after quitting; rejected because the profile exists to be Vantage's tab, and the registration walk (`TabRegistration.OwningTerminal`, capped at five steps) works either way.
- **The "Vantage session" profile runs `pwsh.exe -NoLogo`.** `wt new-tab` passes its own command line, so the profile's command only matters if the user opens the profile by hand.
- **Every tab Vantage opens uses the session profile when the fragment exists.** `WindowsTerminal` takes `ITerminalFragment` and asks `Exists` on each `OpenTabAsync`; `ForTab(launch, profile)` adds `--profile "Vantage session"` when it is not null. Attach, trust and shell tabs all go through this one method; a shell tab with the session profile's bell is harmless and keeps one rule. Checking the file on each open (one `File.Exists`) means a setup run while Vantage is open takes effect without restarting Vantage.
- **Ports.** `ITerminalFragment` (`Location`, `Exists`, `WriteAsync(TerminalProfiles)`, `RemoveAsync()` returning whether anything was removed) and `IClaudeSettings` (`ReadNotificationChannelAsync()` returning a `NotificationChannelState`: `Missing`, `Unset`, `Set(value)` or `Unreadable(reason)`; `EnableTerminalBellAsync()`). Both adapters live in `Infrastructure\Terminal` and `Infrastructure\Claude`; fakes in `TestDoubles`.
- **Parsing is pure, checking is the runner's.** `TerminalSetupCommand.Parse(arguments)` in `Application` returns a `TerminalSetupRequest(string? Sound, string? SessionSound, bool Yes, bool Remove)` or a usage problem (unknown option, option without a value, `--remove` with a sound). `TerminalSetupRunner` checks a given sound with `IFolders.FileExists` and a `.wav` extension (exit 2, nothing written) and drops a missing default with a note. Exit codes follow `vantage here`: 0 done, declined or nothing to remove; 1 a write failed; 2 usage.
- **Two questions, profiles first.** The runner prints the fragment path and the two profiles with their sounds, asks "Write the Windows Terminal profiles? [y/N]", then, only when Claude's setting is not already `terminal_bell` and the file is readable, prints the settings path and the line it adds and asks a second question. `--yes` answers both. Input is a `TextReader`; end of input is no. Two questions let the user take one without the other.
- **Claude's settings file.** Parsed with `JsonNode`, the property set in place (insertion order is kept, so other keys stay where they were), written back with two-space indentation, `\n` line endings and the trailing newline the original had; a missing file becomes `{ "preferredNotifChannel": "terminal_bell" }`. Invalid JSON is never touched. `CLAUDE_CONFIG_DIR` is ignored here, as everywhere in Vantage until the multiple-accounts change.
- **Fragment bytes.** UTF-8 without a byte order mark, indented, written with `FileMode.Create`; the folder is created when missing. `RemoveAsync` deletes `Fragments\Vantage` and nothing above it.
- **Program.cs** dispatches `SetupTerminal` next to `ClaimTab`, before `ClaudeExecutable.Locate`, so the command works on a machine where Claude Code is not yet on the PATH.

## Risks / Trade-offs

- [Windows Terminal must restart before the profiles exist; until then `wt --profile "Vantage session"` names an unknown profile] → the command says to restart; the manual check confirms what `wt` does with an unknown profile name, and if it fails rather than falling back, `WindowsTerminal` retries once without `--profile`.
- [A user who disables fragment profiles in Windows Terminal gets the same unknown-profile case] → same mitigation; the README says the profiles appear in the new-tab menu once loaded.
- [Profile settings in a fragment might be ignored for some keys] → the manual check confirms that `bellSound` and `bellStyle` from the fragment take effect on Windows Terminal 1.25.
- [Claude Code rewrites settings.json while the command writes it] → the write is one small file operation done once; the output says the bell applies to sessions started from now on.
- [Default sounds vary by Windows edition] → a missing default is left out with a note, and the README lists alternatives from `C:\Windows\Media`.

## Migration Plan

Nothing to migrate: without the fragment every tab opens as today. Rollback is `vantage setup-terminal --remove` and, if wanted, removing the one line from `~\.claude\settings.json`.

## Dependencies on other changes

None required. It can merge in any order relative to `header-and-help`, `remove-session-descriptions`, `settings-and-worktree-root`, `folder-column-and-open-keys` and `pull-request-column`. Rebase notes: `session-attach`'s "Open a session in its own Windows Terminal tab" is modified only here; if `header-and-help` is archived first, its change to "Vantage's own tab is recognisable" does not touch this requirement. `remove-session-descriptions` edits the README's `vantage here` text and `CommandLine.cs`'s comment, and every change adds to `Wording`, so expect textual merges only.

## Open Questions

- Whether `preferredNotifChannel` is read by running Claude Code sessions or only at start-up. The wording "from now on" is safe either way.
