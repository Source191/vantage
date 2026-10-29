# Spec Delta

## MODIFIED Requirements

### Requirement: A name that looks like an option needs --
`vantage here` SHALL refuse a name that starts with `-` unless `--` came before it: standard error SHALL name the token and say to put `--` before it to use it as a name, the help for `vantage here` SHALL follow, nothing SHALL start, and the exit code SHALL be 1. `--` ends Vantage's own options only: the Claude Code CLI has no such marker, so an opening prompt that starts with `-` is refused by the session start (session-launch) rather than passed on.

#### Scenario: A mistyped option on its own
- **WHEN** the user runs vantage here --promt
- **THEN** standard error names --promt and says to put -- before it, the help for vantage here is shown, no session starts, and the exit code is 1

#### Scenario: A dash-led name on purpose
- **WHEN** the user runs vantage here -- -experiment in a trusted repository
- **THEN** a session named -experiment starts

#### Scenario: A prompt that starts with a dash
- **WHEN** the user runs vantage here fix-tests -p "-fix the tests" in a trusted repository
- **THEN** the command line parses, the start refuses the prompt with the sentence that an opening prompt cannot start with a dash, no session starts, and the exit code is 2
