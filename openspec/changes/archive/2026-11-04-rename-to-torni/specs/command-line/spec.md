## RENAMED Requirements

- FROM: `### Requirement: Only a bare vantage opens the console`
- TO: `### Requirement: Only a bare torni opens the console`

- FROM: `### Requirement: -- ends only Vantage's own options`
- TO: `### Requirement: -- ends only Torni's own options`

## MODIFIED Requirements

### Requirement: Only a bare torni opens the console
`torni` with no arguments SHALL open the console. Any other first argument SHALL be a command, a help or version option, or a parse error; it SHALL never open the console. A parse error SHALL name what was not recognised on standard error, suggest close matches where there are any, show the help, and exit 1.

#### Scenario: No arguments
- **WHEN** the user runs torni
- **THEN** the console opens

#### Scenario: An unknown command
- **WHEN** the user runs torni frob
- **THEN** standard error names frob as unrecognised, the help is shown, the console does not open, and the exit code is 1

#### Scenario: An unknown option
- **WHEN** the user runs torni --frob
- **THEN** standard error names --frob as unrecognised, the console does not open, and the exit code is 1

### Requirement: Help is printed on request
`torni --help`, `-h`, `/h`, `-?` and `/?` SHALL print Torni's help to standard output and exit 0. The help SHALL describe Torni, say where to support it (`Buy me a sliver of a Claude Max plan: https://buymeacoffee.com/simonvane`), list the `here` and `setup-terminal` commands with their descriptions, and list the help and version options. It SHALL NOT list the commands Torni runs for itself.

#### Scenario: Help
- **WHEN** the user runs torni --help
- **THEN** standard output holds the help with the here and setup-terminal commands, nothing is started, and the exit code is 0

#### Scenario: Help says where to support Vantage
- **WHEN** the user runs torni --help
- **THEN** the description includes Buy me a sliver of a Claude Max plan: https://buymeacoffee.com/simonvane

#### Scenario: Every help spelling
- **WHEN** the user runs torni with -h, /h, -? or /?
- **THEN** the same help is printed and the exit code is 0

#### Scenario: Plumbing stays out of the help
- **WHEN** the help is printed
- **THEN** it does not mention claim-tab

#### Scenario: Question mark on its own
- **WHEN** the user runs torni ?
- **THEN** it is a parse error with exit code 1

### Requirement: The version is printed on request
`torni --version` SHALL print the installed tool's package version to standard output, with any prerelease label and without build metadata such as the commit, and exit 0.

#### Scenario: Version
- **WHEN** Torni 0.13.0 is installed and the user runs torni --version
- **THEN** standard output is 0.13.0 and the exit code is 0

#### Scenario: Prerelease version
- **WHEN** Torni 0.17.0-preview.2 is installed from nuget.org and the user runs torni --version
- **THEN** standard output is 0.17.0-preview.2, with no `+` and commit after it

#### Scenario: Short form of a local build
- **WHEN** the installed tool's version is 0.13.0-local.20261026T101500 and the user runs torni -v
- **THEN** it is a parse error with exit code 1, and torni --version prints a version starting with 0.13.0-local.20261026T101500

### Requirement: Each command explains itself
`--help`, `-h`, `/h`, `-?` or `/?` anywhere after `here` or `setup-terminal` SHALL print that command's usage, arguments and options with their descriptions to standard output, start or write nothing, and exit 0.

#### Scenario: Help for vantage here
- **WHEN** the user runs torni here --help
- **THEN** standard output holds the usage of torni here with its name argument and its --prompt option, no session starts, and the exit code is 0

#### Scenario: Help after other arguments
- **WHEN** the user runs torni here fix-tests -h
- **THEN** the help for torni here is printed, no session starts, and the exit code is 0

#### Scenario: Help for setup-terminal
- **WHEN** the user runs torni setup-terminal -h
- **THEN** standard output holds the usage of torni setup-terminal with --sound, --session-sound, --yes and --remove, no file is written, and the exit code is 0

#### Scenario: A session called help
- **WHEN** the user runs torni here help in a trusted repository
- **THEN** a session named help starts, as for any other name

### Requirement: Help and version need no Claude Code
Help, version and parse errors SHALL work on a machine where Claude Code cannot be found, and outside a Windows Terminal tab.

#### Scenario: Claude Code missing
- **WHEN** claude is not on the PATH and the user runs torni --version
- **THEN** the version is printed and the exit code is 0, with no complaint about Claude Code

### Requirement: Close matches are suggested
When an argument is not recognised and a command or option of the same command is spelt nearly the same, the parse error SHALL suggest it.

#### Scenario: A mistyped command
- **WHEN** the user runs torni hre
- **THEN** standard error names hre as unrecognised, standard output suggests here, and the exit code is 1

#### Scenario: A mistyped option
- **WHEN** the user runs torni setup-terminal --sund C:\Windows\Media\chimes.wav
- **THEN** standard error names --sund as unrecognised, standard output suggests --sound, nothing is written, and the exit code is 1

### Requirement: Conventional option syntax is accepted
An option's value SHALL be accepted after a space, an equals sign or a colon, `--` SHALL end the options so that what follows is taken as arguments, and an option that takes one value SHALL be refused when it is given twice.

#### Scenario: A joined value
- **WHEN** the user runs torni here fix-tests --prompt="Fix the tests" in a trusted repository
- **THEN** a session named fix-tests starts with the opening prompt Fix the tests

#### Scenario: A name that starts with a dash
- **WHEN** the user runs torni here -- -experiment in a trusted repository
- **THEN** a session named -experiment starts

#### Scenario: A repeated prompt
- **WHEN** the user runs torni here -p first -p second
- **THEN** it is a parse error with exit code 1, and no session starts

### Requirement: A name that looks like an option needs --
`torni here` SHALL refuse a name that starts with `-` unless `--` came before it: standard error SHALL name the token and say to put `--` before it to use it as a name, before any other parse error the command line has, the help for `torni here` SHALL follow, nothing SHALL start, and the exit code SHALL be 1.

#### Scenario: A mistyped option on its own
- **WHEN** the user runs torni here --promt
- **THEN** standard error names --promt and says to put -- before it, the help for torni here is shown, no session starts, and the exit code is 1

#### Scenario: A mistyped option followed by a value
- **WHEN** the user runs torni here -d x
- **THEN** standard error names -d and says to put -- before it, then names x as unrecognised, the help for torni here is shown, no session starts, and the exit code is 1

#### Scenario: A dash-led name on purpose
- **WHEN** the user runs torni here -- -experiment in a trusted repository
- **THEN** a session named -experiment starts

#### Scenario: A prompt that starts with a dash
- **WHEN** the user runs torni here fix-tests -p "-fix the tests" in a trusted repository
- **THEN** the command line parses, the start refuses the prompt with the sentence that an opening prompt cannot start with a dash, no session starts, and the exit code is 2

### Requirement: -- ends only Torni's own options
`--` SHALL end Torni's own options only. The Claude Code CLI has no such marker, so an opening prompt that starts with `-` SHALL be refused by the session start (session-launch) rather than passed on to Claude Code, even after `--`.

#### Scenario: A dash-led prompt never reaches Claude Code
- **WHEN** the opening prompt given to torni here starts with `-`
- **THEN** no Claude Code command is run with it, and the session start refuses it with the sentence that an opening prompt cannot start with a dash
