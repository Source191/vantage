# Spec Delta

## MODIFIED Requirements

### Requirement: A session can be started from the shell in the current folder
`vantage here [name] [-p prompt]` SHALL start a supervised session in the current folder on its current branch, named after the folder unless a name is given, record the recent repository, attach in the same tab, and exit 0 after a detach, 1 when the start failed or the command line could not be parsed, and 2 when the name is refused.

#### Scenario: Start here with a prompt
- **WHEN** the user runs vantage here fix-tests -p "Fix the tests" in a trusted repository
- **THEN** a background session named fix-tests starts there with that opening prompt and the shell is attached to it until Ctrl+Z

#### Scenario: The removed description option
- **WHEN** the user runs vantage here fix-tests -d "Flaky orders suite"
- **THEN** it exits 1 naming what was not recognised, and starts nothing

#### Scenario: A refused name
- **WHEN** the user runs vantage here "a;b"
- **THEN** it exits 2 with the sentence for the refused name, and starts nothing
