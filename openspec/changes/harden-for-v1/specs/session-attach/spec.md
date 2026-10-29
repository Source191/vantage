# Spec Delta

## MODIFIED Requirements

### Requirement: Terminal sessions can be adopted
The adopt key on an interactive session that is idle or waiting and has at least one message SHALL end its process and resume its transcript under the supervisor with its name, after which the row is a background session and the old tab is closed; it SHALL be refused while the session is working or before its first message, and a failed resume SHALL leave the old tab's shell at its prompt in the session's folder. The process SHALL be ended only while it is still the session's, that is while the process with the listed id started no later than the session did; when that id now belongs to another process, nothing SHALL be ended or resumed and the footer SHALL say the process is no longer the session's and to refresh.

#### Scenario: Adopt an idle terminal session
- **WHEN** the user presses the adopt key on an idle interactive session that has messages
- **THEN** its process ends, the transcript is resumed as a background session with the same name, its old tab closes, and the footer says the session was moved and its tab closed

#### Scenario: Adoption fails
- **WHEN** the resume under the supervisor fails
- **THEN** the old tab stays open at a shell prompt in the session's folder and the footer says why the resume failed

#### Scenario: Process id reused
- **WHEN** the user presses the adopt key on an interactive session whose listed process id now belongs to a process that started after the session did
- **THEN** nothing is ended or resumed, and the footer says the session's process is no longer its own and to refresh

### Requirement: Empty terminal sessions can be closed
For an interactive session that has never been sent a message, the delete key SHALL end its claude process and then end the Windows Terminal tab's shell with exit code 0 so the tab closes; the close-empty key SHALL do the same for every such session after a confirmation naming them; a session not inside a Windows Terminal tab is only ended. A process SHALL be ended only while it is still the session's (it started no later than the session did); a session whose listed process id now belongs to another process SHALL be left alone and named in the footer.

#### Scenario: Close every empty tab
- **WHEN** two interactive sessions have never been sent a message and the user presses the close-empty key and confirms
- **THEN** both claude processes end, both tabs close, and the footer names them

#### Scenario: Nothing to close
- **WHEN** no interactive session is without messages
- **THEN** the footer says so and nothing is ended

#### Scenario: A tab whose process id was reused
- **WHEN** the user confirms close-empty while one of the named sessions' process ids now belongs to a process that started after that session did
- **THEN** that process and its tab are left alone, the other sessions are closed, and the footer names the one left alone
