# Spec Delta

## Purpose

What Vantage does with its command line as a whole: which arguments open the console, the help and version options, help for each command, and the refusal of anything it does not recognise.

## ADDED Requirements

### Requirement: Only a bare vantage opens the console
`vantage` with no arguments SHALL open the console. Any other first argument SHALL be a command, an option this spec names, or a usage error; it SHALL never open the console.

#### Scenario: No arguments
- **WHEN** the user runs vantage
- **THEN** the console opens

#### Scenario: An unknown command
- **WHEN** the user runs vantage frob
- **THEN** standard error says "Unknown command frob. Run vantage --help for the commands.", the console does not open, and the exit code is 2

#### Scenario: An unknown option
- **WHEN** the user runs vantage --frob
- **THEN** standard error says "Unknown command --frob. Run vantage --help for the commands.", the console does not open, and the exit code is 2

### Requirement: Help is printed on request
`vantage --help`, `-h`, `-?`, `/?`, `?` and `help` SHALL print Vantage's help to standard output and exit 0. The help SHALL name Vantage's version, list `vantage`, `vantage here` and `vantage setup-terminal` with their options, list the `--help` and `--version` options, name the `VANTAGE_LOG` and `VANTAGE_NO_UPDATE_CHECK` environment variables, and give the README's address. It SHALL NOT list the commands Vantage runs for itself.

#### Scenario: Help
- **WHEN** the user runs vantage --help
- **THEN** standard output holds the help with the three commands, nothing is started, and the exit code is 0

#### Scenario: Every help spelling
- **WHEN** the user runs vantage with -h, -?, /?, ? or help
- **THEN** the same help is printed and the exit code is 0

#### Scenario: Plumbing stays out of the help
- **WHEN** the help is printed
- **THEN** it does not mention claim-tab

### Requirement: The version is printed on request
`vantage --version` and `-v` SHALL print `vantage` followed by Vantage's version to standard output and exit 0. A local build SHALL show its prerelease label without the build timestamp.

#### Scenario: Version
- **WHEN** Vantage 0.12.0 is installed and the user runs vantage --version
- **THEN** standard output is "vantage 0.12.0" and the exit code is 0

#### Scenario: Short form of a local build
- **WHEN** the installed tool's version is 0.12.0-local.20261026T101500 and the user runs vantage -v
- **THEN** standard output is "vantage 0.12.0-local" and the exit code is 0

### Requirement: Each command explains itself
`--help`, `-h`, `-?` or `/?` as the first argument after `here` or `setup-terminal` SHALL print that command's usage and options to standard output, start or write nothing, and exit 0. `?` and `help` after `here` SHALL stay session names.

#### Scenario: Help for vantage here
- **WHEN** the user runs vantage here --help
- **THEN** standard output holds the usage of vantage here with its -p option, no session starts, and the exit code is 0

#### Scenario: Help for setup-terminal
- **WHEN** the user runs vantage setup-terminal -h
- **THEN** standard output holds the usage of vantage setup-terminal with its options, no file is written, and the exit code is 0

#### Scenario: A session called help
- **WHEN** the user runs vantage here help in a trusted repository
- **THEN** a session named help starts, as for any other name

### Requirement: Help and version need no Claude Code
Help, version and the unknown-command refusal SHALL work on a machine where Claude Code cannot be found, and outside a Windows Terminal tab.

#### Scenario: Claude Code missing
- **WHEN** claude is not on the PATH and the user runs vantage --version
- **THEN** the version is printed and the exit code is 0, with no complaint about Claude Code
