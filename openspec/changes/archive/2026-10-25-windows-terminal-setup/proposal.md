# Proposal

## Why

A needs-input bell from Vantage sounds the same as every other Windows notification, so it is easy to miss. Windows Terminal can give a profile its own `bellSound`, but today the README only suggests a bell style for every profile, the advice is split across Install, Notifications and Troubleshooting, and setting it up means hand-editing two JSON files.

## What Changes

- New subcommand `vantage setup-terminal [--sound <wav>] [--session-sound <wav>] [--yes] [--remove]`. It writes a Windows Terminal JSON fragment at `%LOCALAPPDATA%\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json` with two profiles: "Vantage" (runs the installed `vantage`, bell style audible, window and taskbar, bell sound `C:\Windows\Media\Windows Notify Messaging.wav` by default, no tab colour so Vantage can still colour its own tab) and "Vantage session" (PowerShell, the same bell style, `C:\Windows\Media\Windows Proximity Notification.wav` by default). The user's `settings.json` is never rewritten.
- The command shows what it will write and asks first unless `--yes` is given, then says to restart Windows Terminal. `--remove` deletes the fragment folder.
- The command also offers to set `"preferredNotifChannel": "terminal_bell"` in `~\.claude\settings.json`, keeping every other key and Claude Code's bare LF line endings.
- Tabs Vantage opens with `wt new-tab` pass `--profile "Vantage session"` when the fragment is installed, so a bell from an attached session sounds different from the Vantage tab's bell and from other notifications. Without the fragment they use the default profile, as today.
- The README gets one "Windows Terminal setup" section that gathers the existing bell advice: the command, the manual steps in the Windows Terminal settings UI, and a list of suggested sounds shipped in `C:\Windows\Media`. The Install, Notifications and Troubleshooting sections point to it.

Out of scope: editing the user's Windows Terminal `settings.json` or profile defaults; a configurable profile name; colour schemes, fonts or other appearance settings; toasts, Claude Code hooks or Vantage playing sounds itself (attention stays the terminal bell); `CLAUDE_CONFIG_DIR` and multiple Claude accounts, which are a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `needs-input-notification`: the README's bell advice becomes a Windows Terminal setup section with suggested sounds; new requirements for the `setup-terminal` command (profiles fragment, confirmation, sounds, Claude's terminal bell, removal); the no-hooks rule gains a scenario for the command.
- `session-attach`: the open-in-tab key opens the tab in the "Vantage session" profile when the fragment is installed.

## Impact

- `Core`: `CommandLine` parses `setup-terminal` into a new command; a `TerminalSetupCommand` parser and a `TerminalSetupRunner` use case in `Application`; new ports for the fragment and for Claude Code's settings file.
- `Infrastructure`: an adapter that writes, reads and removes the fragment; an adapter that reads and updates `~\.claude\settings.json`; `WindowsTerminalArguments.ForTab` adds `--profile` when asked to.
- `Program.cs`: wires the subcommand before Claude Code is located (the command does not need it).
- Tests: `CommandLineShould`, new `TerminalSetupCommandShould`, `TerminalSetupRunnerShould`, adapter tests against a temporary folder, `WindowsTerminalArgumentsShould`; fakes in `TestDoubles`.
- Docs: README (new section, Install, Notifications, Troubleshooting), CLAUDE.md (Windows Terminal facts about fragments). Version: next minor after `main`'s.
