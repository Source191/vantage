# Spec Delta

## MODIFIED Requirements

### Requirement: Open a session in its own Windows Terminal tab
The open-in-tab key SHALL open a new tab in the current Windows Terminal window that runs the attach command for the session, titled with the session name, with application title changes suppressed, starting in the session's working directory, coloured from the palette, and in the "Vantage session" profile when Vantage's Windows Terminal fragment is installed, otherwise in the default profile. When a Vantage-opened tab for the session is alive at the moment the key is pressed, the key SHALL instead switch to that tab; a record whose shell has gone SHALL be dropped and a new tab opened. Opening a tab SHALL drop any earlier record for the same session.

#### Scenario: Tab opened
- **WHEN** the user presses the open-in-tab key on a background session
- **THEN** a new tab appears with the session's name as its title and the attach command running inside it

#### Scenario: Tab opened with the setup profile
- **WHEN** `vantage setup-terminal` has installed the fragment and the user presses the open-in-tab key on a background session
- **THEN** the new tab uses the "Vantage session" profile, so a bell in it plays that profile's sound

#### Scenario: Tab opened without the setup profile
- **WHEN** the fragment is not installed and the user presses the open-in-tab key on a background session
- **THEN** the new tab uses Windows Terminal's default profile, as before

#### Scenario: Tab already live
- **WHEN** a Vantage-opened tab for that session is still alive
- **THEN** no second tab is opened, the Windows Terminal tab titled with the session name is selected in the window that owns it, and the footer says Vantage switched to it

#### Scenario: Live tab cannot be found
- **WHEN** a Vantage-opened tab for that session is recorded as alive but no tab item with the session's name can be found in its Windows Terminal window
- **THEN** no tab is opened and the footer says the tab is open but could not be found

#### Scenario: Tab closed by hand
- **WHEN** the user closes the session's tab with the terminal's close button and, once the tab's shell has gone, presses the open-in-tab key before the next refresh
- **THEN** the stale record is dropped, a new tab opens, and the footer says a tab was opened, not switched to

#### Scenario: Earlier record dropped
- **WHEN** a record for the session remains from a tab that has gone and the user presses the open-in-tab key
- **THEN** the new tab's record is the only one for the session and the old record's colour is free again

### Requirement: Live tabs are tracked by process
Vantage SHALL record each tab it opens with the tab shell's process ID and start time, SHALL treat a record as live only while that process exists with the same start time, and SHALL remove stale records whenever it reads them. A tab opened for an attach SHALL close itself when the attach ends because the user detached or the session was stopped or removed, and SHALL stay open, showing the output, when the attach fails.

#### Scenario: Tab closed
- **WHEN** the user closes a Vantage-opened tab
- **THEN** on the next read its record is removed and its colour becomes free

#### Scenario: Process ID reused
- **WHEN** a record's process ID now belongs to a process with a different start time
- **THEN** the record is treated as stale and removed

#### Scenario: Detach closes the tab
- **WHEN** the user presses Ctrl+Z inside a tab Vantage opened for a session
- **THEN** the tab closes, the TAB column clears within one refresh, and Enter on the row attaches in the console

#### Scenario: Attach fails
- **WHEN** the attach command in a Vantage-opened tab exits with a failure, for example because the session was removed before it started
- **THEN** the tab stays open with the attach command's output

### Requirement: Stop and delete resolve the session ID
Stop and delete SHALL act on the session's ID taken from the listing, never on its name, SHALL act on the row that was selected when the key was pressed even if the listing refreshed before the command ran, and SHALL close the session's Vantage-opened tab when it has one.

#### Scenario: Stop
- **WHEN** the user presses the stop key on a background session
- **THEN** the CLI stop command runs with that session's ID and the row shows stopped on the next refresh

#### Scenario: Listing refreshes between key and command
- **WHEN** the user presses the stop key and a refresh replaces the rows before the stop command runs
- **THEN** the stop command still runs with the ID of the session that was selected, and nothing fails

#### Scenario: Stop closes the tab
- **WHEN** the user presses the stop key on a session shown in a Vantage-opened tab
- **THEN** the session is stopped, the tab closes, and the footer says both

### Requirement: Delete offers to remove the session's worktree
Deleting a session SHALL first end its Vantage-opened tab and wait for the tab's shell to exit. When the session's working directory is a worktree Vantage created, delete SHALL then offer to remove that worktree under the worktree-management safety rules; a worktree whose `.git` has gone SHALL have its folder deleted and its stale registration pruned; and a folder git cannot delete because a tab or program still uses it SHALL be reported as in use, with the session deleted and the folder left for a later remove.

#### Scenario: Clean worktree
- **WHEN** the user deletes a session in a Vantage-created worktree that has no changes and no unpushed commits
- **THEN** Vantage offers removal and, if accepted, removes the worktree and its branch

#### Scenario: Worktree with changes
- **WHEN** the worktree has uncommitted changes
- **THEN** the session is deleted but the worktree is kept and the row explains why

#### Scenario: Tab closed before the worktree goes
- **WHEN** the user deletes a session that was started and opened in its own tab
- **THEN** the tab closes before the worktree is removed, the worktree is removed, and the footer says the session was deleted and its tab closed

#### Scenario: Worktree without its .git
- **WHEN** the user deletes a session whose Vantage-created worktree no longer has a `.git` file
- **THEN** the session is deleted, the folder is deleted, the stale registration is pruned from the repository, and the footer says so

#### Scenario: Folder still in use
- **WHEN** the user deletes a session while a PowerShell tab opened from the worktrees view sits in its worktree
- **THEN** the session is deleted, the footer says the worktree folder is still in use by a tab or program and names it, and pressing remove on it in the worktrees view after closing that tab deletes what is left

### Requirement: Rename without attaching
The rename key SHALL ask for a new name in a dialog and then, because the CLI has no rename verb and resuming a listed background job with new flags starts a copy, SHALL remove the job and resume it by session ID under the new name, which keeps the session ID and its conversation. A session shown in a Vantage-opened tab SHALL have that tab closed after the job is removed and a tab opened under the new name, in the same colour, once the session is resumed. Renaming SHALL be refused while the session is working or when it has never been sent a message, and SHALL resume the job under its old name when the resume under the new name fails. A new name the launch rules would refuse SHALL be refused before anything runs, with the same sentence `vantage here` gives for that name.

#### Scenario: Rename an idle session
- **WHEN** the user presses the rename key on an idle background session and types a new name
- **THEN** the job is removed and resumed by session ID with the new name, and the row shows the new name with the same session ID on the next refresh

#### Scenario: Rename a session with an open tab
- **WHEN** the user renames an idle session whose Vantage-opened tab is alive
- **THEN** that tab closes, one tab titled with the new name opens in the same colour, and the footer says the session was renamed and its tab moved

#### Scenario: Session is working
- **WHEN** the user presses the rename key on a working session
- **THEN** nothing happens and the footer explains that renaming restarts the session

#### Scenario: New name refused
- **WHEN** the user leaves the new name blank
- **THEN** nothing runs and the footer shows exactly "A session name is required.", with nothing after it

#### Scenario: New name with a semicolon
- **WHEN** the user renames an idle session to `fix a;b`
- **THEN** the job is resumed under the name `fix a;b`, and the row shows that name on the next refresh

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
