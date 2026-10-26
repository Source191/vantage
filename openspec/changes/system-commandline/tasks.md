# Tasks

## 1. Runners take values, not arguments

- [x] 1.1 `HereCommandShould` over `HereCommand.Plan(directory, name, prompt, isRepository)` (default name, given name and prompt, refused names, a folder that is not a repository): change the signature, delete the parse facts and the `UnknownOption`, `OptionNeedsValue` and `UnexpectedArgument` cases of `LaunchProblem` with their words and `WordingShould` facts; verify the Core suite passes
- [x] 1.2 `HereCommandRunnerShould` over `RunAsync(directory, name, prompt, …)`: a refused name still writes its sentence and exits 2; verify it passes
- [x] 1.3 `TerminalSetupRunnerShould` over `RunAsync(TerminalSetupRequest, …)`, with `RefuseRemoveWithASound` moved from `TerminalSetupCommandShould` (usage line, exit 2): delete `TerminalSetupCommand`, `TerminalSetupParse`, `TerminalSetupCommandShould` and the three parse cases of `TerminalSetupProblem` with their words; verify the Core suite passes

## 2. The command line as one root command

- [ ] 2.1 `CommandLineShould` invoking `CommandLine.Build(actions)` through `CommandLine.InvokeAsync` with recording fake actions and `StringWriter`s: no arguments opens the console; `here fix-tests -p "Fix them"`, `here --prompt=text`, `here -- -experiment` and `here help` reach the here action with those values; `setup-terminal --sound x.wav --yes` reaches the setup action as a request; `claim-tab <guid> 4242` reaches the claim action; verify each passes
- [ ] 2.2 `CommandLineShould` for the library's help, version and errors: `--help`, `-h`, `/h`, `-?`, `/?` print help naming here and setup-terminal and not claim-tab, exit 0, call no action; `here --help` and `here fix-tests -h` name `--prompt`; `setup-terminal -h` names its four options; `--version` prints a version, exit 0; `frob`, `--frob`, `?`, `-v` and `here -p a -p b` exit 1 with the problem on the error writer and call no action; `here --promt x` suggests `--prompt`; verify each passes
- [ ] 2.3 Refactor: delete `CommandLineSyntax`, `VantageCommand` and its records, `HelpTopic`, `Wording.Help`, `VersionLine`, `UnknownCommand`, `ClaimTabUsage`, `HereUsage` where unused, and their facts; verify the build (warnings as errors), the Core suite and `dotnet format --verify-no-changes`

## 3. Program

- [ ] 3.1 `Program.cs` (wiring): local functions for the four actions holding today's code paths, `CommandLine.Build`, then `CommandLine.InvokeAsync(root, args, Console.Out, Console.Error)`; verify by hand with the Debug build and `claude` off the PATH that `--help`, `here --help`, `setup-terminal -h` and `--version` exit 0 and `frob` exits 1
- [ ] 3.2 Run `dotnet run --project tools\HeadlessTerminal -- check` against a Release build (the console's start-up moved into an action); verify exit 0

## 4. Documentation and release

- [ ] 4.1 README: the help and version paragraph (library spellings, typo suggestions, exit 1 for parse errors), `vantage here` exit codes and the `--` tip for names starting with `-` or `@`, `setup-terminal` exit codes; verify the commands run as written
- [ ] 4.2 CLAUDE.md: Core's packages, `Application\CommandLine` as the command line's one home with descriptions beside the symbols, the exceptions to the `Wording` rule (descriptions, parse errors, help layout), the `ProcessTerminationTimeout = null` reason; verify it matches the code
- [ ] 4.3 Raise `Version` to 0.13.0; verify `.github\scripts\Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` passes
- [ ] 4.4 Run `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes`, `openspec validate system-commandline --strict`; record the line counts against `main` for the pull request; verify all pass

## Workflow follow-up

- Archive the change in the same pull request once it is ready to merge.
