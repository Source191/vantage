# Tasks

## 1. Command line results

- [x] 1.1 `CommandLineShould.OpenTheConsoleOnlyWithNoArguments` and `RefuseAnUnknownCommandWithUsage` (`frob`, `--frob`): replace the `_ => OpenConsole` arm with `Usage(Wording.UnknownCommand(first))`; add `UnknownCommand` with a `WordingShould` fact for "Unknown command frob. Run vantage --help for the commands."; verify the Core test suite passes
- [x] 1.2 `CommandLineShould.AskForTheGeneralHelp` as a theory over `--help`, `-h`, `-?`, `/?`, `?`, `help`: add `PrintHelp(HelpTopic)` and `HelpTopic` (Vantage, Here, SetupTerminal); verify the theory passes
- [x] 1.3 `CommandLineShould.AskForACommandsHelp` (`here --help`, `here -?`, `setup-terminal -h`, `setup-terminal /?`) and `KeepHelpAfterHereAsASessionName` (`here help` and `here ?` stay `StartHere`): add the arms ahead of `here` and `setup-terminal`; verify they pass
- [x] 1.4 `CommandLineShould.AskForTheVersion` over `--version` and `-v`: add `PrintVersion`; verify it passes

## 2. Words for help, version and the tab

- [x] 2.1 `WordingShould.PrintTheVersionLine` (`vantage 0.12.0`, `vantage 0.12.0-local` from `ToolVersion.FromInformational("0.12.0-local.20261026T101500+abc")`): add `Wording.VersionLine(ToolVersion)`; verify it passes
- [x] 2.2 `WordingShould.DescribeEveryCommandInTheGeneralHelp`, `LeavePlumbingOutOfTheHelp` and `NameTheEnvironmentVariablesAndReadme`: add `Wording.Help(HelpTopic.Vantage, ToolVersion)` naming the version, `vantage`, `vantage here`, `vantage setup-terminal`, `--help`, `--version`, `VANTAGE_LOG`, `UpdateCheckOptOut.VariableName` and `https://github.com/Source191/vantage#readme`, and never `claim-tab`; verify the facts pass
- [x] 2.3 `WordingShould.StartEachCommandsHelpWithItsUsageLine`: `Help(HelpTopic.Here, …)` starts with `HereUsage` and explains `-p`/`--prompt`; `Help(HelpTopic.SetupTerminal, …)` starts with `SetupTerminalUsage` and explains each option; verify it passes
- [x] 2.4 `WordingOfScreensShould.TitleTheTabVantage` (replacing the two version facts) and `NameEachDialogInTheTabTitle` as a theory over every `DialogName` member: add `Application\DialogName`, make `Wording.TabTitle` a property and add `TabTitleDuring(DialogName)` (a property and a method cannot share a name) with a switch naming every member; update `Program.cs` and `TerminalConsoleShould` to the plain title; verify the build (warnings as errors) and the Core and Infrastructure suites pass

## 3. Program and dialogs

- [x] 3.1 `Program.cs` (wiring, untested by rule): read `ToolVersion` before the early switch, handle `PrintHelp` and `PrintVersion` there (standard output, exit 0); verify by hand with the Debug build that `vantage --help`, `-v`, `here --help`, `setup-terminal --help` print and exit 0, and `vantage frob` exits 2, all with `claude` removed from the PATH for the session
- [ ] 3.2 Add `Tui\DialogTabTitle.Apply(Dialog, DialogName)` (clears `BorderSettings.TerminalTitle`, writes `Wording.TabTitleDuring(name)` through `app.Driver.SetTerminalTitle` when the dialog becomes modal); give `Confirm.Query` a `DialogName` and call `Apply` from all six dialogs with the words in the session-attach delta; verify `dotnet build -c Debug` passes
- [ ] 3.3 Verify the title sequence with a HeadlessTerminal `drive` capture of `8000:?,2000:\u001b,2000:q` (environment cleared as CLAUDE.md says): the OSC 0 titles are exactly `Vantage`, `Vantage (Keys)`, `Vantage`; repeat for `,` (Settings); then run `dotnet run --project tools\HeadlessTerminal -- check` and verify exit 0

## 4. Documentation and release

- [ ] 4.1 README: the own-tab paragraph (plain `Vantage`, the dialog word), a "Help and version" note under Start Vantage (the spellings, exit codes, unknown commands exit 2), the `vantage here` example without `-d` and with `vantage here --help`, and the manual check (item 6) plus the `drive` title capture; verify the README's commands run as written
- [ ] 4.2 CLAUDE.md: the Windows Terminal title paragraph (plain `Vantage` from `Wording.TabTitle`, `DialogTabTitle` and why, the `Runnable` flag) and the Program.cs line naming the early commands; verify the text matches the code
- [ ] 4.3 Raise `Version` to 0.12.0 in `src\Source191.Vantage\Source191.Vantage.csproj`; verify `.github\scripts\Assert-VersionBumped.ps1` passes locally against `main`
- [ ] 4.4 Run `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate cli-help-and-tab-title --strict`; verify all pass

## Workflow follow-up

- Archive the change in the same pull request (`/opsx:archive`), so `openspec\specs` gains `command-line` and the new own-tab requirement.
