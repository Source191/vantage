# Spec Delta

## MODIFIED Requirements

### Requirement: Help is printed on request
`vantage --help`, `-h`, `/h`, `-?` and `/?` SHALL print Vantage's help to standard output and exit 0. The help SHALL describe Vantage, say where to support it (`Buy me a sliver of a Claude Max plan: https://buymeacoffee.com/simonvane`), list the `here` and `setup-terminal` commands with their descriptions, and list the help and version options. It SHALL NOT list the commands Vantage runs for itself.

#### Scenario: Help
- **WHEN** the user runs vantage --help
- **THEN** standard output holds the help with the here and setup-terminal commands, nothing is started, and the exit code is 0

#### Scenario: Help says where to support Vantage
- **WHEN** the user runs vantage --help
- **THEN** the description includes Buy me a sliver of a Claude Max plan: https://buymeacoffee.com/simonvane

#### Scenario: Every help spelling
- **WHEN** the user runs vantage with -h, /h, -? or /?
- **THEN** the same help is printed and the exit code is 0

#### Scenario: Plumbing stays out of the help
- **WHEN** the help is printed
- **THEN** it does not mention claim-tab

#### Scenario: Question mark on its own
- **WHEN** the user runs vantage ?
- **THEN** it is a parse error with exit code 1
