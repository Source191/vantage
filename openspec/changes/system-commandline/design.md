# Design

## Context

See proposal.md for the why. After `cli-help-and-tab-title`, three pure parsers in Core turn `string[]` into values: `CommandLine.Parse` (the command, help, version, unknown command), `HereCommand.Plan` (`[name] [-p|--prompt text]`, refusing `OptionNeedsValue`, `UnknownOption`, `UnexpectedArgument`) and `TerminalSetupCommand.Parse` (`--sound`, `--session-sound`, `--yes`, `--remove`, the same three refusals plus `RemoveWithSound`). `Wording` turns every refusal into a sentence ending with the command's usage line, and `Program.cs` maps refusals to exit code 2. System.CommandLine 2.0 parses into a `ParseResult` (values by option or argument, `Errors` as message strings, `UnmatchedTokens`) and, when invoked, prints its own help and errors and exits 1 on a parse error.

## Goals / Non-Goals

**Goals:**
- The three parsers' signatures and results stay the same, so `CommandLineShould`, `HereCommandShould` and `TerminalSetupCommandShould` pass unchanged and the pull request shows only the parsing.
- No sentence from the library reaches the user, and the exit codes stay as the specs say.

**Non-Goals:**
- No use of `Invoke`/`InvokeAsync`, actions or the library's help builder; `Program.cs` keeps its switch.

## Decisions

### Parse only, never invoke

One `RootCommand` is built in Core (`Application\CommandLineSyntax`), with a `here` subcommand (an optional `name` argument, `-p`/`--prompt` with one value), a `setup-terminal` subcommand (the two sound options with one value, the two flags) and a hidden `claim-tab` subcommand. `CommandLine.Parse` calls `Parse(arguments, configuration)` and maps the `ParseResult` to a `VantageCommand`; `HereCommand.Plan` and `TerminalSetupCommand.Parse` parse their own subcommand's arguments the same way. Invoking would print the library's words and exit 1.

Alternative: let the library invoke actions that return the existing records. Rejected: actions add a second dispatch beside `Program.cs`'s switch, and the help and error output would still need replacing.

### Help and version stay Vantage's

`?` and `help` are not options to the library, and `?`/`help` after `here` are session names, so `CommandLine.Parse` keeps its list-pattern arms for help and version ahead of the library, and the root command is built without the default `HelpOption` and `VersionOption`. The library then only parses what is a real command.

Alternative: the library's `HelpOption` (it also accepts `/h`) and `VersionOption`, with a custom help action writing `Wording.Help`. Considered at implementation time; it is used only if it keeps the list arms and accepts nothing the `command-line` spec does not name.

### Errors are mapped to problems, not shown

The library's error messages are discarded. The mapping reads the parse result:

- an unmatched token starting with `-` → `UnknownOption`; any other unmatched token → `UnexpectedArgument`;
- an option present with no value → `OptionNeedsValue`;
- an unknown first token → the unknown-command `Usage`.

`RemoveWithSound` stays a check after parsing, as now. If the mapping needs the error text to tell cases apart, that is the finding to report in the pull request, not a reason to show the text.

### Behaviour-changing extras off

`ParserConfiguration` sets `ResponseFileTokenReplacer = null` and `EnablePosixBundling = false`, each with a fact (`vantage here @name` names a session `@name`; `-py` is an unknown option, not `-p y`).

## Risks / Trade-offs

- [The mapping ends up as long as the hand-rolled loops it replaces] → that is the comparison the change exists to show; the pull request description states the line counts before and after, and the user may close it unmerged.
- [The library accepts input the hand-rolled parser refused, such as `--prompt=text`, `-p:text` or an option after `--`] → a fact per such form pins today's refusal, or the pull request lists it as a behaviour change for the user to accept, in which case the change gains a `session-launch` delta and stops being `skip_specs`.
- [A prompt that starts with `-` (`-p "-fix the tests"`)] → the hand-rolled parser takes the next token as the value whatever it is; a fact checks the library does the same, before the switch.
- [The package's version is newer than two weeks on the day of implementation] → pin the newest 2.0.x release at least two weeks old.

## Migration Plan

Version 0.12.0 becomes 0.12.1. No state files change. Rollback is reverting the pull request or reinstalling 0.12.0.

## Open Questions

- Whether `--prompt=text` and `-p:text` should be accepted is left to the user once the pull request shows the library accepts them; it changes only whether this change gains a spec delta.
