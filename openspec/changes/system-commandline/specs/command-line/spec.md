# Spec Delta

## MODIFIED Requirements

### Requirement: Only a bare vantage opens the console
`vantage` with no arguments SHALL open the console. Any other first argument SHALL be a command, a help or version option, or a parse error; it SHALL never open the console. A parse error SHALL name what was not recognised on standard error, suggest close matches where there are any, show the help, and exit 1.

#### Scenario: No arguments
- **WHEN** the user runs vantage
- **THEN** the console opens

#### Scenario: An unknown command
- **WHEN** the user runs vantage frob
- **THEN** standard error names frob as unrecognised, the help is shown, the console does not open, and the exit code is 1

#### Scenario: An unknown option
- **WHEN** the user runs vantage --frob
- **THEN** standard error names --frob as unrecognised, the console does not open, and the exit code is 1

### Requirement: Help is printed on request
`vantage --help`, `-h`, `/h`, `-?` and `/?` SHALL print Vantage's help to standard output and exit 0. The help SHALL describe Vantage, list the `here` and `setup-terminal` commands with their descriptions, and list the help and version options. It SHALL NOT list the commands Vantage runs for itself.

#### Scenario: Help
- **WHEN** the user runs vantage --help
- **THEN** standard output holds the help with the here and setup-terminal commands, nothing is started, and the exit code is 0

#### Scenario: Every help spelling
- **WHEN** the user runs vantage with -h, /h, -? or /?
- **THEN** the same help is printed and the exit code is 0

#### Scenario: Plumbing stays out of the help
- **WHEN** the help is printed
- **THEN** it does not mention claim-tab

#### Scenario: Question mark on its own
- **WHEN** the user runs vantage ?
- **THEN** it is a parse error with exit code 1

### Requirement: The version is printed on request
`vantage --version` SHALL print the installed tool's informational version to standard output and exit 0; the version MAY carry build metadata after a `+`.

#### Scenario: Version
- **WHEN** Vantage 0.13.0 is installed and the user runs vantage --version
- **THEN** standard output starts with 0.13.0 and the exit code is 0

#### Scenario: Short form of a local build
- **WHEN** the installed tool's version is 0.13.0-local.20261026T101500 and the user runs vantage -v
- **THEN** it is a parse error with exit code 1, and vantage --version prints a version starting with 0.13.0-local.20261026T101500

### Requirement: Each command explains itself
`--help`, `-h`, `/h`, `-?` or `/?` anywhere after `here` or `setup-terminal` SHALL print that command's usage, arguments and options with their descriptions to standard output, start or write nothing, and exit 0.

#### Scenario: Help for vantage here
- **WHEN** the user runs vantage here --help
- **THEN** standard output holds the usage of vantage here with its name argument and its --prompt option, no session starts, and the exit code is 0

#### Scenario: Help after other arguments
- **WHEN** the user runs vantage here fix-tests -h
- **THEN** the help for vantage here is printed, no session starts, and the exit code is 0

#### Scenario: Help for setup-terminal
- **WHEN** the user runs vantage setup-terminal -h
- **THEN** standard output holds the usage of vantage setup-terminal with --sound, --session-sound, --yes and --remove, no file is written, and the exit code is 0

#### Scenario: A session called help
- **WHEN** the user runs vantage here help in a trusted repository
- **THEN** a session named help starts, as for any other name

### Requirement: Help and version need no Claude Code
Help, version and parse errors SHALL work on a machine where Claude Code cannot be found, and outside a Windows Terminal tab.

#### Scenario: Claude Code missing
- **WHEN** claude is not on the PATH and the user runs vantage --version
- **THEN** the version is printed and the exit code is 0, with no complaint about Claude Code

## ADDED Requirements

### Requirement: Close matches are suggested
When an argument is not recognised and a command or option of the same command is spelt nearly the same, the parse error SHALL suggest it.

#### Scenario: A mistyped option
- **WHEN** the user runs vantage here --promt "Fix the tests"
- **THEN** standard error names --promt as unrecognised and suggests --prompt, no session starts, and the exit code is 1

### Requirement: Conventional option syntax is accepted
An option's value SHALL be accepted after a space, an equals sign or a colon, `--` SHALL end the options so that what follows is taken as arguments, and an option that takes one value SHALL be refused when it is given twice.

#### Scenario: A joined value
- **WHEN** the user runs vantage here fix-tests --prompt="Fix the tests" in a trusted repository
- **THEN** a session named fix-tests starts with the opening prompt Fix the tests

#### Scenario: A name that starts with a dash
- **WHEN** the user runs vantage here -- -experiment in a trusted repository
- **THEN** a session named -experiment starts

#### Scenario: A repeated prompt
- **WHEN** the user runs vantage here -p first -p second
- **THEN** it is a parse error with exit code 1, and no session starts
