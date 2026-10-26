# Proposal

## Why

Opening any dialog renames Vantage's tab to the dialog's title (press `?` and the tab reads "Keys"), because Terminal.Gui 2.5.0 turns on `BorderSettings.TerminalTitle` for every `Runnable`, dialogs included, and writes the dialog's title as OSC 0 while it is modal. The tab is how the user finds Vantage among session tabs, so it should always say Vantage. Separately, `vantage --help`, `vantage ?` and `vantage --version` open the console, because `CommandLine.Parse` treats anything it does not recognise as "open the console"; a terminal command is expected to print help or its version and exit, and to reject what it does not understand.

## What Changes

- Vantage's own tab is titled `Vantage`, without the version (the header already shows it, with `-local` for a local build). **BREAKING** for anyone matching the old `Vantage (<version>)` title; nothing in Vantage does.
- While a dialog is open the tab reads `Vantage (<word>)`, one short fixed word per dialog: Keys, New session, Rename, Resume, Settings, Delete session, Delete branch, Close empty, Force remove, Open solution, Update. Closing the dialog puts `Vantage` back. The dialog's own border title is unchanged.
- `vantage --help`, `-h`, `-?`, `/?`, `?` and `help` print a help text to standard output and exit 0: the commands (`vantage`, `vantage here`, `vantage setup-terminal`), their options, the environment variables (`VANTAGE_LOG`, `VANTAGE_NO_UPDATE_CHECK`) and the README link. `claim-tab` stays out of it; it is Vantage's own plumbing.
- `vantage --version` and `-v` print `vantage <version>` (`vantage 0.12.0`, `vantage 0.12.0-local` for a local build) to standard output and exit 0.
- `vantage here --help` and `vantage setup-terminal --help` (also `-h`, `-?`, `/?`, as the first argument after the command) print that command's usage and exit 0. `?` and `help` are not help after `here`, because `vantage here help` names a session "help".
- **BREAKING**: an unrecognised command or argument (`vantage frob`, `vantage --frob`) prints `Unknown command frob. Run vantage --help for the commands.` to standard error and exits 2, as the other usage errors do. Only a bare `vantage` opens the console. The update tab and the "Vantage" Windows Terminal profile both run a bare `vantage`, so neither is affected.
- Help and version need neither Claude Code nor a terminal tab, so they run before Claude Code is located, as `setup-terminal` does.
- Version 0.11.3 becomes 0.12.0 (minor: new behaviour, and pre-1.0 minor may break).

Out of scope: moving the parsing to System.CommandLine (a separate change, `system-commandline`, so the difference is a pull request of its own); shell completion; a `vantage help <command>` form; colour in the help text; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

- `command-line`: what Vantage does with its command line as a whole: the help and version options, help for each command, and the refusal of anything it does not recognise. `vantage here` and `vantage setup-terminal` keep their own behaviour in `session-launch` and `needs-input-notification`.

### Modified Capabilities

- `session-attach`: the "Vantage's own tab is recognisable" requirement drops the version from the title and gains the dialog word while a dialog is open.

## Impact

- `Core`: `CommandLine` gains help, version and unknown-command results; `Wording` gains the help texts, the version line, the unknown-command sentence, a `DialogName` word per dialog, and `TabTitle` loses its `ToolVersion`.
- `Tui`: every dialog (`Confirm`, `NameDialog`, `TypedConfirmationDialog`, `ResumeChecklistDialog`, `SettingsDialog`, `LaunchWizardDialog`) turns off Terminal.Gui's title writing and writes `Vantage (<word>)` itself when it becomes modal, through one helper.
- `Program.cs`: help and version join the early switch; the tool version is read before it.
- Tests: `CommandLineShould`, `WordingOfScreensShould`, `WordingShould`, `TerminalConsoleShould`; the HeadlessTerminal hand-off check and a `drive` capture of the title sequence.
- Docs: README (own tab title, the new options, `vantage here --help` in its example, the manual check), CLAUDE.md (the title facts), the csproj version.
