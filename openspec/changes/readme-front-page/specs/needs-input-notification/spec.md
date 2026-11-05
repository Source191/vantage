# Spec Delta

## MODIFIED Requirements

### Requirement: Claude Code's own bell covers attached tabs
The user guide SHALL have one short notifications section, and the README's getting started one step, that tell the user to run `torni setup-terminal`, which sets Claude Code's `preferredNotifChannel` to `terminal_bell` and so makes Claude ring the bell in the tab a session is attached in when it finishes a task or pauses for a permission prompt while the user appears away.

#### Scenario: Attached session needs permission
- **WHEN** a session attached in a Torni-opened tab pauses for a permission prompt while the user is in another tab
- **THEN** Claude Code rings the bell in that tab and Windows Terminal flags it

#### Scenario: Choosing a sound by hand
- **WHEN** the user picks `C:\Windows\Media\Windows Message Nudge.wav` as the Torni profile's bell sound in Windows Terminal's settings
- **THEN** a bell in the Torni tab plays that sound and bells in other profiles keep theirs

#### Scenario: Reading the notifications section
- **WHEN** the user reads the user guide's notifications section
- **THEN** it names `torni setup-terminal` as the one step that sets up both bells

#### Scenario: Reading the getting started
- **WHEN** the user reads the README's getting started
- **THEN** one step names `torni setup-terminal` as what adds the Torni profiles and both bells, and links to the user guide's notifications section
