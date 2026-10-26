# Proposal

## Why

Vantage parses its command line by hand: list patterns in `CommandLine.Parse` and a queue loop each in `HereCommand.Plan` and `TerminalSetupCommand.Parse`, about 120 lines after `cli-help-and-tab-title`. System.CommandLine 2.0 (Microsoft, MIT, compatible with Vantage's Apache-2.0) is the .NET standard for this, and moving to it may make the parsing shorter and set Vantage up for more commands. Doing it as a change of its own, with no behaviour change, makes the pull request a direct comparison of the two approaches, so the user can decide whether to keep it.

## What Changes

- `Source191.Vantage.Core` takes a package reference to `System.CommandLine` (2.0.x, MIT), its second package after `Microsoft.Extensions.Logging.Abstractions`.
- `CommandLine.Parse`, `HereCommand.Plan` and `TerminalSetupCommand.Parse` are reimplemented over one System.CommandLine `RootCommand` with `here`, `setup-terminal` and a hidden `claim-tab` subcommand, keeping their signatures and their results (`VantageCommand`, `LaunchPlanResult`, `TerminalSetupParse`) so the existing tests stay unchanged and are the contract.
- Every sentence the user sees still comes from `Wording`: the library's own help output, parse-error messages and colours are not used; its parse results are mapped to the existing problem values (`LaunchProblem`, `TerminalSetupProblem`, the unknown-command `Usage`).
- The library's extras that would change behaviour are turned off: response files (`@file`, which would make `vantage here @name` read a file) and POSIX bundling of short options.
- Version 0.12.0 (after `cli-help-and-tab-title`) becomes 0.12.1, a patch, since behaviour does not change.

Out of scope: the library's generated help, "did you mean" suggestions, `--option=value` syntax and shell completion (each changes behaviour and would be its own change once this one is accepted); any change to the commands, options, words or exit codes; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The `command-line`, `session-launch` and `needs-input-notification` specs describe the behaviour, which stays the same; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Core`: `Application\CommandLine.cs`, `Application\Launch\HereCommand.cs`, `Application\TerminalSetup\TerminalSetupCommand.cs`; the csproj comment on its one package.
- Tests: `CommandLineShould`, `HereCommandShould`, `TerminalSetupCommandShould` unchanged and passing; new facts only for the library behaviour turned off (response files, bundling).
- Docs: CLAUDE.md (Core's packages, where the parsing lives), README's Developing section if it lists packages; the csproj version.
- Depends on `cli-help-and-tab-title` being merged first.
