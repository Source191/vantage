# Design

## Context

See proposal.md for the why. Today (0.12.x) `Program.cs` calls `CommandLine.Parse(args)`, switches on the `VantageCommand` it returns (`OpenConsole`, `StartHere`, `SetupTerminal`, `ClaimTab`, `PrintHelp`, `PrintVersion`, `Usage`), and hands the raw arguments of `here` and `setup-terminal` to `HereCommandRunner` and `TerminalSetupRunner`, which parse them again (`HereCommand.Plan`, `TerminalSetupCommand.Parse`) and report parse problems with exit code 2. PR #62 replaced the parsing underneath with System.CommandLine 2.0.12 but kept every result and refusal, which needed `CommandLineSyntax` to undo the library's defaults. Facts from the 2.0.12 source: `RootCommand` adds `HelpOption` (`--help`, `-h`, `/h`, `-?`, `/?`) and `VersionOption` (entry assembly's informational version); `ParseErrorAction` writes the errors, typo suggestions and help, and returns 1; the message resources are internal and cannot be replaced; `InvocationConfiguration.ProcessTerminationTimeout` defaults to two seconds and registers Ctrl+C, SIGINT and SIGTERM handlers that cancel and then exit with 130 or 143.

## Goals / Non-Goals

**Goals:**
- The command line is declared once, as data a contributor can read without knowing Vantage's conventions: commands, arguments and options with their descriptions, and an action per command.
- The library's defaults apply wherever the user chose them (help, version, errors, syntax); Vantage keeps only its domain checks.
- The declaration is tested in memory, without a process or a console.

**Non-Goals:**
- No change to what the runners do once they have their values: `vantage here` still plans, starts and attaches; `setup-terminal` still checks sounds, asks and writes.
- No customised help layout, error text or version output.

## Decisions

### The root command lives in Core, its actions come from Program.cs

`Application\CommandLine.Build(CommandActions actions)` returns the `RootCommand`. `CommandActions` is a record of four delegates: open the console; start here with a name and a prompt; set up the terminal with a `TerminalSetupRequest`; claim a tab with a registration id and a process id. Each returns the exit code. `Program.cs` builds the record from local functions holding today's code paths (Claude Code is still located only by the console and `here`), then calls `CommandLine.InvokeAsync(root, args, Console.Out, Console.Error)`. That helper parses and invokes with `ProcessTerminationTimeout = null` and the given writers, so tests and the tool run the same configuration.

Alternatives: build the root in `Program.cs` (untested, against the "no untested code but wiring" rule); an interface instead of the delegates record (one more type and a class to implement it in `Program.cs` for no gain).

### Descriptions sit beside their symbols

Each command, argument and option carries its `Description` inline in `CommandLine`, in British English. That is the library's idiom and keeps an option and its words in one place for a contributor. It is an exception to "user text lives only in `Wording`", which CLAUDE.md records together with the second exception: parse errors and the help layout are the library's.

Alternative: description constants in `Wording`. Rejected: it splits every option across two files, and `WordingShould` facts for each description would only restate the declaration.

### Vantage's own checks stay Vantage's

`HereCommand.Plan(directory, name, prompt, isRepository)` keeps the session-name rules and `LaunchPlanBuilder`; `HereCommandRunner` still writes `Wording.Describe(problem)` and exits 2 for a refused name. `TerminalSetupRunner.RunAsync(TerminalSetupRequest, …)` takes over the `--remove`-with-a-sound check from the deleted `TerminalSetupCommand` and keeps the sound checks, each worded with the usage line and exit code 2. Only the parse cases (`UnknownOption`, `OptionNeedsValue`, `UnexpectedArgument`) of `LaunchProblem` and `TerminalSetupProblem` go.

### Options are declared plainly

`-p`/`--prompt`, `--sound` and `--session-sound` are `Option<string>`; `--yes` and `--remove` are `Option<bool>`; `name` is an `Argument<string>` with arity zero or one; `claim-tab` takes `Argument<Guid>` and `Argument<int>` and is `Hidden`. No validators beyond the library's own, no parser configuration: response files, bundling, `--`, `=`/`:` values and the error for a repeated option are the defaults.

### Ctrl+C stays as it is

`ProcessTerminationTimeout = null` keeps the library from registering its termination handlers. With them, a Ctrl+C that reached Vantage while `vantage here` is attached would cancel the action and force an exit two seconds later. Today Claude and Terminal.Gui read Ctrl+C as a key, so the handlers would change nothing visible at best and end Vantage under an attached session at worst. The default exception handler stays on (the library's default), so an unhandled exception is reported on standard error with exit code 1 instead of a crash dialog's stack trace.

## Risks / Trade-offs

- [The help layout and error text change with a library update] → tests assert that the help names each command and option, not its layout; `Version` of the package is pinned and raised deliberately.
- [American spelling in parse errors ("Unrecognized command or argument")] → accepted by the user; CLAUDE.md records the exception.
- [`vantage here` uses exit code 1 for both a failed start and a command line that cannot be parsed] → accepted; the message on standard error tells them apart, and a refused name keeps 2.
- [`vantage here @notes` reads a file named notes; `vantage here --bogus` starts a session named `--bogus`] → accepted library defaults; the README says how to name a session that starts with `@` or `-` (`--` before it).
- [Contributors meet System.CommandLine's API] → it is documented on Microsoft Learn; the declaration in `CommandLine` is the only place it appears.

## Migration Plan

Version 0.12.1 becomes 0.13.0. No state files change. Scripts that used `vantage ?`, `vantage help` or `vantage -v`, or relied on exit code 2 for a parse error, need `--help`, `--version` or exit code 1. Rollback is reinstalling 0.12.0.
