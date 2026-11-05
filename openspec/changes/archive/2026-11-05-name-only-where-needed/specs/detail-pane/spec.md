## MODIFIED Requirements

### Requirement: The last prompt is the transition Torni saw
The pane's Prompted line SHALL show the most recent time, while this Torni ran, that the session left needs input or idle for working, which is when a prompt or an answer reached it. When Torni has not seen such a transition for the session, the line SHALL say so rather than guess, and the transcript SHALL NOT be read for it.

#### Scenario: An answer reaches the session
- **WHEN** fix-123 was needs input at one refresh and working at the next, at 14:03
- **THEN** its Prompted line reads 14:03 and the minutes since

#### Scenario: Not seen
- **WHEN** a session has been idle since Torni started
- **THEN** its Prompted line reads not seen since the console started
