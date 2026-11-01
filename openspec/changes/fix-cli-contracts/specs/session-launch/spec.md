## MODIFIED Requirements

### Requirement: vantage here says how to detach
Before attaching, `vantage here` SHALL print one line saying that Ctrl+Z detaches and the session keeps running, and SHALL erase that line when the attach exits, whether it exited because the user detached, because it could not start, or because it was interrupted. In every one of those cases the terminal SHALL be put back in order (focus reporting off, the cursor shown, late replies discarded) before `vantage here` returns.

#### Scenario: The line before attaching
- **WHEN** vantage here starts a session and attaches
- **THEN** the last line printed before handing over is exactly "Ctrl+Z detaches; the session keeps running.", and it is gone from the terminal once the attach exits

#### Scenario: The attach cannot start
- **WHEN** vantage here has started a session and the attach command cannot be started, for example because the Claude executable was removed between the start and the attach
- **THEN** the "Ctrl+Z detaches" line is gone from the terminal, and the cursor is shown and focus reporting is off when the shell prompt returns
