# Design

## Context

See proposal.md for the why. Two facts shape the approach.

- **Terminal.Gui's title writing.** `Runnable`'s constructor sets `Border.Settings |= BorderSettings.TerminalTitle`, and its `BorderView` calls `driver.SetTerminalTitle(runnable.Title)` (OSC 0) whenever the runnable's `IsModal` changes to true or its `Title` changes. A HeadlessTerminal `drive` capture of `?` then Esc on 0.11.3 shows `]0;Vantage (0.11.3)` at start, `]0;Keys` when the help opens and `]0;Vantage (0.11.3)` again when it closes. The main `Window` therefore restores its own title once a dialog closes; only the dialog's own write is wrong. No dialog opens another dialog: the six `app.Run(dialog)` sites (`Confirm`, `NameDialog`, `TypedConfirmationDialog`, `ResumeChecklistDialog`, `SettingsDialog`, `LaunchWizardDialog`) are each reached from the table screen.
- **The command line.** `CommandLine.Parse` matches list patterns and ends with `_ => new OpenConsole()`. `Program.cs` handles `ClaimTab`, `SetupTerminal` and `Usage` in an early switch before Claude Code is located; `Usage` writes its message to standard error and returns 2. `vantage here` and `setup-terminal` parse their own options (`HereCommand.Plan`, `TerminalSetupCommand.Parse`) and report problems as values that `Wording` words. The tool version is read later in `Program.cs`, after Claude Code is located. Every caller of the vantage executable (the update tab, the "Vantage" profile, the `v` shortcut) passes no arguments; `claim-tab` passes its own.

## Goals / Non-Goals

**Goals:**
- One word per dialog in `Wording`, chosen through a closed enum so a new dialog without a word fails the build.
- The title rule stays a tested pure function (`Wording.TabTitle`), and the Terminal.Gui part shrinks to one helper.
- Help, version and refusal are values returned by the existing parser and worded by `Wording`, with no new package.

**Non-Goals:**
- No change to how `here` and `setup-terminal` parse their own options; only a help flag in first position is intercepted before them.
- No test of the Tui helper beyond the headless capture; the Tui layer has no unit tests.

## Decisions

### Dialogs turn off Terminal.Gui's title and write their own

A helper in `Tui` (`DialogTabTitle.Apply(dialog, DialogName)`) clears `BorderSettings.TerminalTitle` on the dialog and handles the dialog's `IsModalChanged`: when it becomes modal it calls `app.Driver.SetTerminalTitle(Wording.TabTitleDuring(name))`. Each of the six dialogs calls it straight after it is built. The main `Window` keeps the flag, so Terminal.Gui writes `Vantage` back when the dialog closes, as the capture shows.

Alternatives: clearing the flag only and writing `Vantage` after `app.Run` returns would never show the dialog's word, which the user asked for; setting the dialog's `Title` to `Vantage (Keys)` would change the border title too; writing OSC 2 through `TerminalConsole` is outside the Tui and would race Terminal.Gui's own OSC 0 writes.

### A closed `DialogName` enum and `Wording.TabTitle`

`Application\DialogName` has one member per dialog: `Keys`, `NewSession`, `Rename`, `Resume`, `Settings`, `DeleteSession`, `DeleteBranch`, `CloseEmpty`, `ForceRemove`, `OpenSolution`, `Update`. `Wording.TabTitle` becomes a property answering `Vantage`, and `Wording.TabTitleDuring(DialogName)` answers `Vantage (Keys)` through a switch that names every member (CS8509 catches a missing one). `Confirm.Query` takes a `DialogName` after the application, since it serves six flows; the single-purpose dialogs pass their own. `ScreenContext.Title`, the `Window` title and `TerminalConsole` all take `Wording.TabTitle`, so the "one value" rule holds; `TabTitle` no longer takes `ToolVersion`.

Alternative: derive the word from the dialog's title. Rejected: the titles are long ("Close empty terminal sessions") or dynamic ("New session in Northwind.Orders").

### Help and version are parser results

`CommandLine.Parse` gains three outcomes, avoiding the name `ShowHelp`, which is already an effect:

- `PrintHelp(HelpTopic Topic)`, where `HelpTopic` is `Vantage`, `Here` or `SetupTerminal`. The top-level help spellings (`--help`, `-h`, `-?`, `/?`, `?`, `help`) in first position give `Vantage`. `here` or `setup-terminal` followed by `--help`, `-h`, `-?` or `/?` give that command's topic. These arms come before the `here` and `setup-terminal` arms.
- `PrintVersion` for `--version` or `-v` in first position.
- `Usage(Wording.UnknownCommand(first))` replaces the `_ => OpenConsole` arm. `[]` stays the only way to `OpenConsole`.

`Program.cs` reads `ToolVersion` before the early switch, and handles `PrintHelp` and `PrintVersion` there, writing `Wording.Help(topic, version)` or `Wording.VersionLine(version)` to standard output and returning 0. `Wording.Help(HelpTopic.Here, …)` and `Wording.Help(HelpTopic.SetupTerminal, …)` start with the existing `HereUsage` and `SetupTerminalUsage` lines, so the usage lines keep one spelling each. The help ends with `https://github.com/Source191/vantage#readme`.

Alternative: System.CommandLine 2.0 (MIT). Rejected for this change: its help headings and errors are its own American English words outside `Wording`, its parse errors exit 1 rather than 2, and Core would take a second package. It is the follow-up change `system-commandline`, so the comparison is a pull request of its own.

## Risks / Trade-offs

- [A Terminal.Gui update changes when `IsModalChanged` fires or stops the `Window` restoring its title] → the README's manual checks gain a `drive` capture of `?` then Esc that must show `Vantage`, `Vantage (Keys)`, `Vantage`; CLAUDE.md records the mechanism.
- [Someone scripted `vantage <anything>` to open the console] → it now exits 2 with a sentence naming `--help`. Nothing in Vantage or its Windows Terminal profile does; the README notes the change.
- [`/?` is rewritten by Git Bash's path conversion] → irrelevant on PowerShell, the supported shell; `-?` and `--help` work everywhere.

## Migration Plan

Version 0.11.3 becomes 0.12.0. No state files change. Rollback is reinstalling 0.11.3.
