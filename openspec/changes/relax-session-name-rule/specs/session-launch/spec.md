# Spec Delta

## MODIFIED Requirements

### Requirement: A session name is mandatory
The wizard SHALL require a non-blank name, SHALL accept any other name, including one holding a semicolon or a double quote, and SHALL use the name exactly as typed as the session name, the tab title and the Remote Control name. A refused name SHALL be explained in a plain sentence, in the wizard and by `vantage here`, with nothing after it.

#### Scenario: Blank name
- **WHEN** the user submits a blank name
- **THEN** the wizard refuses to continue and explains that a name is required

#### Scenario: Name with a semicolon
- **WHEN** the user submits the name `fix a;b` and opens the session in a tab
- **THEN** the session is named `fix a;b`, exactly one tab opens, and it is titled `fix a;b`

#### Scenario: Name with a double quote
- **WHEN** the user submits the name `say "hi"` and opens the session in a tab
- **THEN** the session is named `say "hi"` and its tab is titled `say "hi"`

#### Scenario: The explanation is only the sentence
- **WHEN** the wizard or `vantage here` refuses a blank name
- **THEN** the explanation is exactly "A session name is required.", with no parameter name or other technical text after it, and it fits on the wizard's problem line on a 120-column terminal

#### Scenario: A blank name given to vantage here
- **WHEN** `vantage here ""` is run
- **THEN** it prints exactly "A session name is required." and starts nothing
