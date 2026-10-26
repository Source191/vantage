# Proposal

## Why

Vantage parses its command line by hand: list patterns in `CommandLine.Parse`, an option loop each in `HereCommand.Plan` and `TerminalSetupCommand.Parse`, help texts written out in `Wording`, and a problem record and sentence for every way an argument can be wrong. Adding an option means touching four files and two test classes and knowing Vantage's conventions. A first attempt at moving to System.CommandLine 2.0 (Microsoft, MIT, compatible with Vantage's Apache-2.0) kept every old refusal and grew the code by 45 % (PR #62). Most of those refusals protected accidental behaviour, not users. Used as intended, the library gives users generated help, typo suggestions and conventional syntax, and gives contributors, with or without a coding agent, one documented, declarative place to change the command line.

## What Changes

- One `RootCommand` in `Application\CommandLine` declares the whole command line: the console as the root action, `here` (an optional `name` argument, `-p`/`--prompt`), `setup-terminal` (`--sound`, `--session-sound`, `--yes`, `--remove`) and a hidden `claim-tab` (a GUID and a process id). Every symbol has a description, written in British English beside it. Each command's action calls the existing runner; `Program.cs` builds the actions and calls `InvokeAsync`.
- **BREAKING**: help is the library's, generated from the descriptions, for `--help`, `-h`, `/h`, `-?` and `/?` anywhere on the line. `vantage ?` and `vantage help` are no longer help (they are unrecognised), and the help no longer lists environment variables or the README address.
- **BREAKING**: `--version` is the library's and prints the tool's informational version, which can include build metadata after a `+`; `-v` is gone.
- **BREAKING**: parse errors are the library's: its own (American English) messages, suggestions of close matches, the command's help after them, and exit code 1 instead of 2. Vantage's own checks keep their sentences and exit code 2: a refused session name, a missing or non-wav sound, `--remove` with a sound.
- **BREAKING**: the library's defaults for syntax apply: `--prompt=text` and `-p:text` work, `--` ends the options, `@file` reads a response file, a repeated option is an error, and a name may start with `-`.
- Removed: `CommandLineSyntax`, `TerminalSetupCommand` and `TerminalSetupParse`, the `PrintHelp`/`PrintVersion`/`Usage` results, `HelpTopic`, the parse cases of `LaunchProblem` and `TerminalSetupProblem`, and the help, version, usage and unknown-command words in `Wording`, with their tests.
- Ctrl+C is unchanged: the library's termination handler is turned off (`ProcessTerminationTimeout = null`), since it would cancel and force an exit two seconds after a Ctrl+C.
- Version 0.13.0 becomes 0.14.0 (minor; pre-1.0 minor may break; 0.13.0 went to the wizard change, PR #63).

Out of scope: shell completion setup (`dotnet-suggest`) and its documentation; localising the library's messages; new commands or options; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `command-line`: help, version and parse errors become the library's, with its spellings, exit code and suggestions; conventional option syntax is accepted.
- `session-launch`: `vantage here` exits 1 for a command line that cannot be parsed and 2 only for a refused name; the removed `-d` option is now an unrecognised argument.

## Impact

- `Core`: `Application\CommandLine` rewritten as the `RootCommand` and its actions; `HereCommand.Plan` and `HereCommandRunner` take a name and a prompt instead of raw arguments; `TerminalSetupRunner` takes a `TerminalSetupRequest`; `CommandLineSyntax`, `TerminalSetupCommand` and the help and parse wording deleted.
- `Program.cs`: an actions record built from local functions, then `InvokeAsync`.
- Tests: `CommandLineShould` invokes the root command with fake actions and in-memory writers; `HereCommandShould`, `HereCommandRunnerShould` and `TerminalSetupRunnerShould` follow the new signatures; `TerminalSetupCommandShould` and the parse and help facts in `WordingShould` go.
- Docs: README (help and version, `vantage here` and `setup-terminal` exit codes), CLAUDE.md (Core's packages, the command line's home, the exceptions to the `Wording` rule).
- Supersedes the strict comparison in PR #62, on the same branch.
