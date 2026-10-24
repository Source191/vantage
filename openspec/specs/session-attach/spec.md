# session-attach Specification

## Purpose
Lets the user work inside a session from the console, in the same tab or in a new Windows Terminal tab, and stop, delete or bring back sessions, including after a reboot.

## Requirements

### Requirement: Drill into a session in the same tab
Pressing Enter on a background session SHALL suspend the console screen and run the Claude Code attach command for that session in the same terminal, and SHALL redraw the console when the attach command exits.

#### Scenario: Attach and return
- **WHEN** the user presses Enter on a background session and later presses Ctrl+Z inside it
- **THEN** the attach command exits, the console redraws with a fresh listing, and the session keeps running

#### Scenario: Live tab already attached
- **WHEN** a Vantage-opened tab for the session is still alive
- **THEN** Enter does not attach and the footer says the session is open in another tab

#### Scenario: Footer teaches the return key
- **WHEN** the console starts an attach
- **THEN** the last line printed before handing over says that Ctrl+Z returns to Vantage

### Requirement: Open a session in its own Windows Terminal tab
The open-in-tab key SHALL open a new tab in the current Windows Terminal window that runs the attach command for the session, titled with the session name, with application title changes suppressed, starting in the session's working directory, and coloured from the palette. When a Vantage-opened tab for the session is still alive, the key SHALL instead switch to that tab.

#### Scenario: Tab opened
- **WHEN** the user presses the open-in-tab key on a background session
- **THEN** a new tab appears with the session's name as its title and the attach command running inside it

#### Scenario: Tab already live
- **WHEN** a Vantage-opened tab for that session is still alive
- **THEN** no second tab is opened, the Windows Terminal tab titled with the session name is selected in the window that owns it, and the footer says Vantage switched to it

#### Scenario: Live tab cannot be found
- **WHEN** a Vantage-opened tab for that session is recorded as alive but no tab item with the session's name can be found in its Windows Terminal window
- **THEN** no tab is opened and the footer says the tab is open but could not be found

### Requirement: Tab colours avoid colours in use
Vantage SHALL choose the first palette colour not used by any live Vantage-opened tab, and when every colour is in use SHALL reuse the least used colour and warn.

#### Scenario: First free colour
- **WHEN** the first two palette colours are used by live tabs
- **THEN** the next tab gets the third colour

#### Scenario: Palette exhausted
- **WHEN** every palette colour is in use
- **THEN** the least used colour is reused and the footer shows a warning

### Requirement: Live tabs are tracked by process
Vantage SHALL record each tab it opens with the tab shell's process ID and start time, SHALL treat a record as live only while that process exists with the same start time, and SHALL remove stale records whenever it reads them.

#### Scenario: Tab closed
- **WHEN** the user closes a Vantage-opened tab
- **THEN** on the next read its record is removed and its colour becomes free

#### Scenario: Process ID reused
- **WHEN** a record's process ID now belongs to a process with a different start time
- **THEN** the record is treated as stale and removed

### Requirement: Stop and delete resolve the session ID
Stop and delete SHALL act on the session's ID taken from the listing, never on its name.

#### Scenario: Stop
- **WHEN** the user presses the stop key on a background session
- **THEN** the CLI stop command runs with that session's ID and the row shows stopped on the next refresh

### Requirement: Delete offers to remove the session's worktree
Deleting a session whose working directory is a worktree Vantage created SHALL offer to remove that worktree under the worktree-management safety rules.

#### Scenario: Clean worktree
- **WHEN** the user deletes a session in a Vantage-created worktree that has no changes and no unpushed commits
- **THEN** Vantage offers removal and, if accepted, removes the worktree and its branch

#### Scenario: Worktree with changes
- **WHEN** the worktree has uncommitted changes
- **THEN** the session is deleted but the worktree is kept and the row explains why

### Requirement: Resume sessions after a reboot without reviving deliberate stops
The resume-all key SHALL run the CLI's background resume for every session in the failed state, SHALL present sessions in the stopped state as a checklist with none preselected because a deliberate stop also shows as stopped, and SHALL show on each row whether the CLI continued the session or started a copy.

#### Scenario: Resume after restart
- **WHEN** three sessions show failed after a reboot and the user presses resume-all
- **THEN** the background resume command runs for each of the three and their rows return to idle or working on the next refresh

#### Scenario: Stopped sessions are opt-in
- **WHEN** two sessions show stopped when the user presses resume-all
- **THEN** they are listed with unticked boxes and only the ones the user ticks are resumed

### Requirement: Rename without attaching
The rename key SHALL ask for a new name in a dialog and then, because the CLI has no rename verb and resuming a listed background job with new flags starts a copy, SHALL remove the job and resume it by session ID under the new name, which keeps the session ID and its conversation. Renaming SHALL be refused while the session is working or when it has never been sent a message, and SHALL resume the job under its old name when the resume under the new name fails.

#### Scenario: Rename an idle session
- **WHEN** the user presses the rename key on an idle background session and types a new name
- **THEN** the job is removed and resumed by session ID with the new name, and the row shows the new name with the same session ID on the next refresh

#### Scenario: Session is working
- **WHEN** the user presses the rename key on a working session
- **THEN** nothing happens and the footer explains that renaming restarts the session

### Requirement: Terminal sessions can be adopted
The adopt key on an interactive session that is idle or waiting and has at least one message SHALL end its process and resume its transcript under the supervisor with its name, after which the row is a background session; it SHALL be refused while the session is working or before its first message.

#### Scenario: Adopt an idle terminal session
- **WHEN** the user presses the adopt key on an idle interactive session that has messages
- **THEN** its process ends, the transcript is resumed as a background session with the same name, and its old tab shows a plain shell

### Requirement: Empty terminal sessions can be closed
For an interactive session that has never been sent a message, the delete key SHALL end its claude process and then end the Windows Terminal tab's shell with exit code 0 so the tab closes; the close-empty key SHALL do the same for every such session after a confirmation naming them; a session not inside a Windows Terminal tab is only ended.

#### Scenario: Close every empty tab
- **WHEN** two interactive sessions have never been sent a message and the user presses the close-empty key and confirms
- **THEN** both claude processes end, both tabs close, and the footer names them

#### Scenario: Nothing to close
- **WHEN** no interactive session is without messages
- **THEN** the footer says so and nothing is ended

### Requirement: Sessions with nothing to resume are refused
Attach, open-in-tab, rename and resume SHALL be refused for a session that has no process and no transcript, because Claude writes a session's transcript only on its first message, and the footer SHALL say that delete removes it.

#### Scenario: Stub session
- **WHEN** the user presses Enter on a failed session whose transcript does not exist
- **THEN** nothing happens and the footer says it was never sent a message and that delete removes it

### Requirement: The native agent view is one key away
The agent-view key SHALL suspend the console and run the native Claude Code agent view in the same tab, returning to the console when it exits.

#### Scenario: Open native view
- **WHEN** the user presses the agent-view key
- **THEN** the native agent view opens in the same tab and the console redraws when it closes

### Requirement: Vantage's own tab is recognisable
When the console starts in a Windows Terminal tab, Vantage SHALL title that tab "Vantage" and colour it green when no live Vantage-opened tab is green, otherwise with the first free palette colour; SHALL count its own tab's colour as in use when colouring session tabs; and SHALL clear the title and reset the colour when it quits.

#### Scenario: Green is free
- **WHEN** Vantage starts and no live Vantage-opened tab is green
- **THEN** the tab is titled Vantage and coloured green, and the next session tab opened gets a colour other than green

#### Scenario: Green is in use
- **WHEN** Vantage starts while a live Vantage-opened tab is green
- **THEN** the tab is titled Vantage and coloured with the first palette colour no live tab uses

#### Scenario: Title restored after a hand-off
- **WHEN** the user returns from an attached session or the agent view, which set the tab title themselves
- **THEN** the tab is titled Vantage and coloured as before again

#### Scenario: Quit
- **WHEN** the user quits Vantage
- **THEN** the tab's title is cleared and its colour reset, and its registration goes stale with the process
