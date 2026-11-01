# Spec Delta

## MODIFIED Requirements

### Requirement: Claude Code's own bell covers attached tabs
The README SHALL have one short notifications section that tells the user to run `vantage setup-terminal`, which sets Claude Code's `preferredNotifChannel` to `terminal_bell` and so makes Claude ring the bell in the tab a session is attached in when it finishes a task or pauses for a permission prompt while the user appears away.

#### Scenario: Attached session needs permission
- **WHEN** a session attached in a Vantage-opened tab pauses for a permission prompt while the user is in another tab
- **THEN** Claude Code rings the bell in that tab and Windows Terminal flags it

#### Scenario: Choosing a sound by hand
- **WHEN** the user picks `C:\Windows\Media\Windows Message Nudge.wav` as the Vantage profile's bell sound in Windows Terminal's settings
- **THEN** a bell in the Vantage tab plays that sound and bells in other profiles keep theirs

#### Scenario: Reading the notifications section
- **WHEN** the user reads the README's notifications section
- **THEN** it names `vantage setup-terminal` as the one step that sets up both bells

## REMOVED Requirements

### Requirement: The README's setup section explains the bell's style and sound
**Reason**: The owner cut the README to a short getting-started guide on 1 November 2026; bell styles, sound files and the manual Windows Terminal steps are detail that `vantage setup-terminal` does for the user, and its `--help` names the `--sound` options.
**Migration**: Run `vantage setup-terminal`, with `--sound` and `--session-sound` to choose other `.wav` files; Windows Terminal's own profile settings (Advanced, Bell notification style and Bell sound) still work by hand.
