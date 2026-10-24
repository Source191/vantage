# Spec Delta

## Purpose

Tells the user, without blocking the session or the terminal, that a session needs them.

## ADDED Requirements

### Requirement: The console rings the terminal bell when a session starts waiting
Because the agent_needs_input and agent_completed hook types fire only while the native agent view is open, Vantage SHALL write the terminal bell character to its own terminal whenever a refresh shows a background session in needs input that was in another state at the previous refresh, and SHALL NOT ring for a session that was already waiting, for completed or failed transitions, for interactive sessions, or on the first load.

#### Scenario: Session starts waiting
- **WHEN** a refresh shows a background session in needs input that was working at the previous refresh
- **THEN** the bell rings once in the Vantage tab, so Windows Terminal flags it according to the profile's bell style

#### Scenario: No duplicate bell
- **WHEN** a session was already in needs input at the previous refresh
- **THEN** no bell rings for it until its state changes again

#### Scenario: Interactive sessions ring their own tab
- **WHEN** an interactive session starts waiting
- **THEN** Vantage stays silent, because Claude Code rings the bell in that session's own tab

### Requirement: Claude Code's own bell covers attached tabs
The README SHALL tell the user to set Claude Code's `preferredNotifChannel` to `terminal_bell`, which makes Claude ring the bell in the tab a session is attached in when it finishes a task or pauses for a permission prompt while the user appears away, and SHALL suggest a Windows Terminal bell style that flags the window and taskbar.

#### Scenario: Attached session needs permission
- **WHEN** a session attached in a Vantage-opened tab pauses for a permission prompt while the user is in another tab
- **THEN** Claude Code rings the bell in that tab and Windows Terminal flags it

### Requirement: No toasts and no hooks
Vantage SHALL NOT raise Windows toast notifications and SHALL NOT install Claude Code hooks; attention is the terminal bell, the header's needs-input count and the amber rows.

#### Scenario: Install
- **WHEN** Vantage is installed
- **THEN** nothing is written to the user's Claude Code hooks
