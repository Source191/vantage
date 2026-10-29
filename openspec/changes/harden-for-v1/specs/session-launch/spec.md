# Spec Delta

## MODIFIED Requirements

### Requirement: The session starts under the supervisor and opens where the user chose
Vantage SHALL start the session as a named background session in the chosen directory, passing the opening prompt when one was given and starting an idle session when none was, and SHALL then open the session in its own Windows Terminal tab or return to the list as the user chose. An opening prompt that starts with a dash SHALL be refused before anything starts, in the wizard and by `vantage here`, with a sentence saying the CLI would read it as options and to start it with a word.

#### Scenario: No opening prompt
- **WHEN** the user leaves the opening prompt blank
- **THEN** the session starts idle and waits for input under the given name

#### Scenario: Start and open in a tab
- **WHEN** the user chooses "start and open in tab"
- **THEN** Vantage starts the session, opens it in a new Windows Terminal tab titled with the session name as the open-in-tab key does, and the sessions view stays on screen with the footer naming the tab's colour

#### Scenario: Started session not listed yet
- **WHEN** the user chooses "start and open in tab" and the supervisor has not yet listed the new session when the start returns
- **THEN** the footer reports the session as started and says to open it with the open-in-tab key once it appears, and no tab is opened

#### Scenario: Prompt that starts with a dash
- **WHEN** the user types the opening prompt "- fix the tests" or "--dangerously-skip-permissions" and starts
- **THEN** nothing starts and the wizard says an opening prompt cannot start with a dash because Claude Code would read it as options

#### Scenario: Prompt with a dash inside it
- **WHEN** the user types the opening prompt "Fix the tests - all of them"
- **THEN** the session starts with that prompt

### Requirement: A session can be started from the shell in the current folder
`vantage here [name] [-p prompt]` SHALL start a supervised session in the current folder on its current branch, named after the folder unless a name is given, record the recent repository, attach in the same tab, and exit 0 after a detach, 1 when the start failed or the command line could not be parsed, and 2 when the name or the opening prompt is refused.

#### Scenario: Start here with a prompt
- **WHEN** the user runs vantage here fix-tests -p "Fix the tests" in a trusted repository
- **THEN** a background session named fix-tests starts there with that opening prompt and the shell is attached to it until Ctrl+Z

#### Scenario: The removed description option
- **WHEN** the user runs vantage here fix-tests -d "Flaky orders suite"
- **THEN** it exits 1 naming what was not recognised, and starts nothing

#### Scenario: A refused name
- **WHEN** the user runs vantage here "a;b"
- **THEN** it exits 2 with the sentence for the refused name, and starts nothing

#### Scenario: A refused prompt
- **WHEN** the user runs vantage here fix-tests -p "-fix the tests" in a trusted repository
- **THEN** it exits 2 with the sentence that an opening prompt cannot start with a dash, and starts nothing
