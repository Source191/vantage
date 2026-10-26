# Tasks

## 1. Pin today's behaviour

- [x] 1.1 Add facts the hand-rolled parsers already pass, before any change: `HereCommandShould.TakeAPromptThatStartsWithADash`, `NameASessionStartingWithAnAt` (`@name`), `RefuseBundledShortOptions` (`-py`), `RefuseAnOptionJoinedToItsValue` (`--prompt=text`, `-p:text`); `TerminalSetupCommandShould.RefuseASoundJoinedToItsValue` (`--sound=x.wav`); verify they pass on the current code and record the line counts of the three parsers

## 2. Move the parsing

- [x] 2.1 Add the `System.CommandLine` package (newest 2.0.x at least two weeks old) to `Source191.Vantage.Core` and update the csproj comment; verify `dotnet build -c Debug` passes and the package's licence is MIT
- [x] 2.2 Reimplement `TerminalSetupCommand.Parse` over a `setup-terminal` command with response files and bundling off; verify `TerminalSetupCommandShould` passes unchanged
- [x] 2.3 Reimplement `HereCommand.Plan`'s option reading over a `here` command; verify `HereCommandShould` and `HereCommandRunnerShould` pass unchanged
- [ ] 2.4 Reimplement `CommandLine.Parse`'s command selection (`here`, `setup-terminal`, hidden `claim-tab`, unknown command) over one `RootCommand`, keeping the help and version arms ahead of it; verify `CommandLineShould` passes unchanged
- [ ] 2.5 Refactor: remove what the library made unused and keep the problem mapping in one place; verify the Core suite passes and `dotnet format --verify-no-changes` is clean

## 3. Documentation and release

- [ ] 3.1 CLAUDE.md: Core's packages (Logging.Abstractions and System.CommandLine) and that the library only parses, never prints; verify the text matches the code
- [ ] 3.2 Raise `Version` to 0.12.1; verify `.github\scripts\Assert-VersionBumped.ps1` passes against `main`
- [ ] 3.3 Run `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes`, `openspec validate system-commandline --strict`, and by hand `vantage --help`, `vantage frob`, `vantage here --help`, `vantage setup-terminal --sound` with the Debug build; verify the output and exit codes match 0.12.0
- [ ] 3.4 Write the line counts before and after, and any input the library accepts that was refused before, into the pull request description; verify both are there

## Workflow follow-up

- Archive the change in the same pull request if it is merged; close it unmerged if the comparison does not favour the library.
