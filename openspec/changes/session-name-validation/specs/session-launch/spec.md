# Spec Delta

## MODIFIED Requirements

### Requirement: A session name is mandatory
The wizard SHALL require a non-blank name, SHALL reject a name containing a semicolon or a double quote, and SHALL use the name as the session name, the tab title and the Remote Control name. A refused name SHALL be explained in a plain sentence, in the wizard and by `vantage here`, with nothing after it.

#### Scenario: Blank name
- **WHEN** the user submits a blank name
- **THEN** the wizard refuses to continue and explains that a name is required

#### Scenario: Name with a semicolon
- **WHEN** the user submits a name containing a semicolon
- **THEN** the wizard refuses it because Windows Terminal treats semicolons as command separators

#### Scenario: The explanation is only the sentence
- **WHEN** the wizard or `vantage here` refuses a name
- **THEN** the explanation is exactly "A session name is required." or "A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely.", with no parameter name or other technical text after it
