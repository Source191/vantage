# session-attach Specification

## Purpose
Lets the user work inside a session from the console, in a Windows Terminal tab of its own that Enter opens or switches to, and stop, delete or bring back sessions, including after a reboot.

## Requirements

### Requirement: Open a session in its own Windows Terminal tab
Enter on a background session is the open-in-tab key. It SHALL open a new tab in the current Windows Terminal window that runs the attach command for the session, titled with the session name, with application title changes suppressed, starting in the session's working directory, coloured from the palette, and in the "Vantage session" profile when Vantage's Windows Terminal fragment is installed, otherwise in the default profile.

#### Scenario: Tab opened
- **WHEN** the user presses Enter on a background session
- **THEN** a new tab appears with the session's name as its title and the attach command running inside it, and the console stays on screen

#### Scenario: Tab opened with the setup profile
- **WHEN** `vantage setup-terminal` has installed the fragment and the user presses Enter on a background session
- **THEN** the new tab uses the "Vantage session" profile, so a bell in it plays that profile's sound

#### Scenario: Tab opened without the setup profile
- **WHEN** the fragment is not installed and the user presses Enter on a background session
- **THEN** the new tab uses Windows Terminal's default profile, as before

#### Scenario: Tab already live
- **WHEN** a Vantage-opened tab for that session is still alive and the user presses Enter on it
- **THEN** no second tab is opened, the Windows Terminal tab titled with the session name is selected in the window that owns it, and the status line says Vantage switched to it

#### Scenario: Live tab cannot be found
- **WHEN** a Vantage-opened tab for that session is recorded as alive but no tab item with the session's name can be found in its Windows Terminal window
- **THEN** no tab is opened and the status line says the tab is open but could not be found

#### Scenario: Tab closed by hand
- **WHEN** the user closes the session's tab with the terminal's close button and, once the tab's shell has gone, presses Enter on the session before the next refresh
- **THEN** the stale record is dropped, a new tab opens, and the status line says a tab was opened, not switched to

#### Scenario: Earlier record dropped
- **WHEN** a record for the session remains from a tab that has gone and the user presses Enter on the session
- **THEN** the new tab's record is the only one for the session and the old record's colour is free again

#### Scenario: Empty session
- **WHEN** the user presses Enter on a running background session that has never been sent a message
- **THEN** its tab shows that session's own prompt, not an agent list, and Ctrl+Z in it closes the tab as for any session

#### Scenario: o is not a key
- **WHEN** the user presses o on a background session
- **THEN** nothing happens and no tab is opened

### Requirement: The console never attaches in its own tab
The console SHALL never run the attach command in Vantage's own tab; a session is worked in through its own tab, and only `vantage here` attaches in the tab it was run from.

#### Scenario: Enter leaves the console on screen
- **WHEN** the user presses Enter on a background session
- **THEN** Vantage's own tab keeps showing the console, and the attach command runs only in the session's tab

### Requirement: Tab colours avoid colours in use
Vantage SHALL choose the first palette colour not used by any live Vantage-opened tab, and when every colour is in use SHALL reuse the least used colour and warn.

#### Scenario: First free colour
- **WHEN** the first two palette colours are used by live tabs
- **THEN** the next tab gets the third colour

#### Scenario: Palette exhausted
- **WHEN** every palette colour is in use
- **THEN** the least used colour is reused and the status line shows a warning

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
- **THEN** the tab closes, the TAB column clears within one refresh, and Enter on the row opens a new tab

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
- **THEN** the session is stopped, the tab closes, and the status line says both

### Requirement: Delete offers to remove the session's worktree
Deleting a session SHALL first end its Vantage-opened tab and wait for the tab's shell to exit. When the session's working directory is a worktree Vantage created, delete SHALL then offer to remove that worktree under the worktree-management safety rules.

#### Scenario: Clean worktree
- **WHEN** the user deletes a session in a Vantage-created worktree that has no changes and no unpushed commits
- **THEN** Vantage offers removal and, if accepted, removes the worktree and its branch

#### Scenario: Worktree with changes
- **WHEN** the worktree has uncommitted changes
- **THEN** the session is deleted but the worktree is kept and the row explains why

#### Scenario: Tab closed before the worktree goes
- **WHEN** the user deletes a session that was started and opened in its own tab
- **THEN** the tab closes before the worktree is removed, the worktree is removed, and the status line says the session was deleted and its tab closed

#### Scenario: Worktree without its .git
- **WHEN** the user deletes a session whose Vantage-created worktree no longer has a `.git` file
- **THEN** the session is deleted, the folder is deleted, the stale registration is pruned from the repository, and the status line says so

#### Scenario: Folder still in use
- **WHEN** the user deletes a session while a PowerShell tab opened from the worktrees view sits in its worktree
- **THEN** the session is deleted, the status line says the worktree folder is still in use by a tab or program and names it, and pressing remove on it in the worktrees view after closing that tab deletes what is left

### Requirement: Rename without attaching
The rename key SHALL ask for a new name in a dialog and then, because the CLI has no rename verb and resuming a listed background job with new flags starts a copy, SHALL remove the job and resume it by session ID under the new name, which keeps the session ID and its conversation.

#### Scenario: Rename an idle session
- **WHEN** the user presses the rename key on an idle background session and types a new name
- **THEN** the job is removed and resumed by session ID with the new name, and the row shows the new name with the same session ID on the next refresh

#### Scenario: Rename a session with an open tab
- **WHEN** the user renames an idle session whose Vantage-opened tab is alive
- **THEN** that tab closes, one tab titled with the new name opens in the same colour, and the status line says the session was renamed and its tab moved

#### Scenario: Session is working
- **WHEN** the user presses the rename key on a working session
- **THEN** nothing happens and the status line explains that renaming restarts the session

#### Scenario: New name refused
- **WHEN** the user leaves the new name blank
- **THEN** nothing runs and the status line shows exactly "A session name is required.", with nothing after it

#### Scenario: New name with a semicolon
- **WHEN** the user renames an idle session to `fix a;b`
- **THEN** the job is resumed under the name `fix a;b`, and the row shows that name on the next refresh

### Requirement: Terminal sessions can be adopted
The adopt key on an interactive session that is idle or waiting and has at least one message SHALL end its process and resume its transcript under the supervisor with its name, after which the row is a background session and the old tab is closed; it SHALL be refused while the session is working or before its first message, and a failed resume SHALL leave the old tab's shell at its prompt in the session's folder.

#### Scenario: Adopt an idle terminal session
- **WHEN** the user presses the adopt key on an idle interactive session that has messages
- **THEN** its process ends, the transcript is resumed as a background session with the same name, its old tab closes, and the status line says the session was moved and its tab closed

#### Scenario: Adoption fails
- **WHEN** the resume under the supervisor fails
- **THEN** the old tab stays open at a shell prompt in the session's folder and the status line says why the resume failed

#### Scenario: Process id reused
- **WHEN** the user presses the adopt key on an interactive session whose listed process id now belongs to a process that started after the session did
- **THEN** nothing is ended or resumed, and the status line says the session's process is no longer its own and to refresh

### Requirement: Empty terminal sessions can be closed
For an interactive session that has never been sent a message, the delete key SHALL end its claude process and then end the Windows Terminal tab's shell with exit code 0 so the tab closes; the close-empty key SHALL do the same for every such session after a confirmation naming them; a session not inside a Windows Terminal tab is only ended.

#### Scenario: Close every empty tab
- **WHEN** two interactive sessions have never been sent a message and the user presses the close-empty key and confirms
- **THEN** both claude processes end, both tabs close, and the status line names them

#### Scenario: Nothing to close
- **WHEN** no interactive session is without messages
- **THEN** the status line says so and nothing is ended

#### Scenario: A tab whose process id was reused
- **WHEN** the user confirms close-empty while one of the named sessions' process ids now belongs to a process that started after that session did
- **THEN** that process and its tab are left alone, the other sessions are closed, and the status line names the one left alone

### Requirement: Sessions with nothing to resume are refused
Open-in-tab, rename and resume SHALL be refused for a session that has no process and no transcript, because Claude writes a session's transcript only on its first message, and the status line SHALL say that delete removes it.

#### Scenario: Stub session
- **WHEN** the user presses Enter on a failed session whose transcript does not exist
- **THEN** nothing happens, no tab opens, and the status line says it was never sent a message and that delete removes it

### Requirement: Vantage's own tab is recognisable
When the console starts in a Windows Terminal tab, Vantage SHALL title that tab "Vantage" and colour it green when no live Vantage-opened tab is green, otherwise with the first free palette colour; SHALL count its own tab's colour as in use when colouring session tabs; and SHALL clear the title and reset the colour when it quits, so the tab shows its profile's own colour again.

#### Scenario: Green is free
- **WHEN** Vantage starts and no live Vantage-opened tab is green
- **THEN** the tab is titled Vantage and coloured green, and the next session tab opened gets a colour other than green

#### Scenario: Green is in use
- **WHEN** Vantage starts while a live Vantage-opened tab is green
- **THEN** the tab is titled Vantage, and coloured with the first palette colour no live tab uses

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.12.0-local.20261026T101500
- **THEN** the tab is titled Vantage, and the version, with its prerelease label, is shown only in the header

#### Scenario: A dialog is open
- **WHEN** the user presses the help key
- **THEN** the tab reads Vantage (Keys) while the help is shown, and Vantage again once it is closed

#### Scenario: Each dialog has its own word
- **WHEN** the new-session wizard, rename, settings, delete session, delete branch, close empty, force remove, open solution or update dialog is open
- **THEN** the tab reads Vantage (New session), Vantage (Rename), Vantage (Settings), Vantage (Delete session), Vantage (Delete branch), Vantage (Close empty), Vantage (Force remove), Vantage (Open solution) or Vantage (Update) respectively

#### Scenario: Title restored after a hand-off
- **WHEN** the user opens a session's tab with Enter, works in it while Claude sets that tab's title, and selects the Vantage tab again
- **THEN** the Vantage tab is still titled Vantage and coloured as before, because the attach ran in the other tab

#### Scenario: Quit
- **WHEN** the user quits Vantage
- **THEN** the tab's title is cleared, the tab shows its profile's colour again because the palette entry Vantage coloured is reset rather than only re-pointed, and its registration goes stale with the process

### Requirement: A tab runs its values as literal text
Every value Vantage places in the script of a tab it opens (an executable path, a session's job id, a registration id, a folder or repository name) SHALL reach the program as the same text, whatever characters it holds. No value, including one containing a straight or curly single quote, SHALL end the literal it sits in or be run as a command.

#### Scenario: Path with a curly apostrophe
- **WHEN** the Claude executable lives under a profile folder whose name contains a curly apostrophe, such as `C:\Users\O’Neill`
- **THEN** the session's tab runs that executable from that exact path and runs nothing else from the path's text

#### Scenario: Value that looks like a command
- **WHEN** a value in a tab's script holds a quote followed by PowerShell, such as `x'; Remove-Item C:\temp; '`
- **THEN** the tab prints or passes that text unchanged and does not run the command

### Requirement: The open-in-tab key switches to a live tab
When a Vantage-opened tab for the session is alive at the moment the open-in-tab key is pressed, the key SHALL switch to that tab instead of opening another; a record whose shell has gone SHALL be dropped and a new tab opened. Opening a tab SHALL drop any earlier record for the same session.

#### Scenario: Pressed twice
- **WHEN** the user presses the open-in-tab key on a background session, and presses it again on the same row while the tab it opened is still alive
- **THEN** the second press opens no tab and selects the tab the first press opened

### Requirement: Delete clears a worktree whose .git has gone
When the session's Vantage-created worktree no longer has its `.git`, delete SHALL delete the worktree's folder and prune its stale registration from the repository.

#### Scenario: The worktree is no longer listed
- **WHEN** the user deletes a session whose Vantage-created worktree has lost its `.git` file
- **THEN** the folder is gone and the repository no longer lists the worktree

### Requirement: Delete leaves a folder in use for a later remove
A worktree folder git cannot delete because a tab or program still uses it SHALL be reported as in use, with the session deleted and the folder left for a later remove.

#### Scenario: A program holds the folder
- **WHEN** the user deletes a session in a Vantage-created worktree while a program still uses that folder, so git cannot delete it
- **THEN** the session is deleted, the status line says the folder is still in use, and the worktree is still listed in the worktrees view

### Requirement: Rename moves the session's tab
A session shown in a Vantage-opened tab SHALL have that tab closed after the job is removed, and a tab opened under the new name, in the same colour, once the session is resumed.

#### Scenario: A blue tab renamed
- **WHEN** the user renames the idle session fix-123, shown in a live blue Vantage-opened tab, to fix-124
- **THEN** the tab titled fix-123 closes and a blue tab titled fix-124 opens

### Requirement: Rename is refused while working or before the first message
Renaming SHALL be refused while the session is working or when it has never been sent a message.

#### Scenario: No message yet
- **WHEN** the user presses the rename key on a background session that has never been sent a message
- **THEN** nothing runs and the status line says why the session cannot be renamed

### Requirement: A failed rename keeps the old name
When the resume under the new name fails, rename SHALL resume the job under its old name.

#### Scenario: Resume under the new name fails
- **WHEN** the job has been removed and the resume under the new name fails
- **THEN** the job is resumed under its old name and the status line says the rename failed and why

### Requirement: A new name is checked before anything runs
A new name the launch rules would refuse SHALL be refused before anything runs, with the same sentence `vantage here` gives for that name.

#### Scenario: The same words as vantage here
- **WHEN** the user leaves the new name blank in the rename dialog
- **THEN** no job is removed or resumed, and the status line shows the sentence `vantage here` gives for a blank name

### Requirement: Adopt ends only the session's own process
The adopt key SHALL end the process only while it is still the session's, that is while the process with the listed id started no later than the session did; when that id now belongs to another process, nothing SHALL be ended or resumed and the status line SHALL say the process is no longer the session's and to refresh.

#### Scenario: The session's own process
- **WHEN** the user presses the adopt key on an idle interactive session with messages whose listed process started before the session's listed start
- **THEN** that process is ended and the transcript is resumed under the supervisor

### Requirement: Closing empty sessions ends only their own processes
The delete and close-empty keys SHALL end a process only while it is still the session's (it started no later than the session did); a session whose listed process id now belongs to another process SHALL be left alone and named in the status line.

#### Scenario: Every process id reused
- **WHEN** the user confirms close-empty while every named session's process id now belongs to a process that started after that session did
- **THEN** no process or tab is ended and the status line names every session left alone

### Requirement: A dialog names itself in Vantage's tab
While a dialog is open Vantage's tab SHALL read "Vantage" followed by the dialog's short name in brackets, and closing the dialog SHALL put "Vantage" back; no dialog's own title SHALL reach the tab.

#### Scenario: The folder picker
- **WHEN** the user presses F3 in the repository roots dialog
- **THEN** the tab reads Vantage (Browse) while the picker is open, never the picker's own title, and Vantage (Settings) again once it closes

### Requirement: Vantage's tab title is always the same outside dialogs
Outside a dialog, every title Vantage writes for its own tab SHALL be the same text.

#### Scenario: Start and dialog
- **WHEN** Vantage writes its tab title as it starts and after a dialog closes
- **THEN** the title is Vantage each time

### Requirement: Resume every shown stopped or failed session in its own tab
The resume-all key SHALL, without a dialog, resume every background session in the stopped or failed state that the table shows, in the table's order, and SHALL open a Vantage tab for each one the CLI resumes. It SHALL remove each job before resuming it by session ID, since resuming a listed job starts a copy. A resume that fails after the removal SHALL get no tab and SHALL be reported with the command that resumes it. With none shown, the status line SHALL say there is nothing to resume.

#### Scenario: Resume after restart
- **WHEN** three sessions show failed after a reboot and the user presses resume-all
- **THEN** each job is removed and resumed by session ID, a tab opens for each, and the same three rows, with the same session IDs, return to idle or working on the next refresh

#### Scenario: Stopped sessions resume too
- **WHEN** two sessions show stopped and one shows failed when the user presses resume-all
- **THEN** no dialog opens, all three are resumed in the order the table shows them, each in its own tab, and the status line says three of three resumed in their own tabs

#### Scenario: The filter narrows the resume
- **WHEN** four sessions show stopped, a filter shows only two of them, and the user presses resume-all
- **THEN** only the two shown are resumed and given tabs, and the other two stay stopped

#### Scenario: A resume the CLI refuses
- **WHEN** the user presses resume-all on two stopped sessions and the CLI refuses to resume one of them
- **THEN** the other resumes in its own tab, no tab opens for the refused one, and the status line says one of two resumed and names the refused one with the CLI's reason

#### Scenario: No duplicate after a resume
- **WHEN** the user resumes a stopped session named fix-123 with session ID sess-1
- **THEN** the next refresh lists exactly one row named fix-123, with session ID sess-1, and no stopped copy of it

#### Scenario: Resume fails after the job was removed
- **WHEN** the user resumes a failed session named fix-123 with session ID sess-1 and the resume command fails with "Workspace not trusted"
- **THEN** no tab opens for it, and the status line names fix-123, gives the failure, and says to resume it with claude --bg --resume sess-1

#### Scenario: A session never sent a message
- **WHEN** the user presses resume-all while a stopped session named repos-f6 has no transcript
- **THEN** its job is neither removed nor resumed, no tab opens for it, and the status line names repos-f6 and says there is nothing to resume

#### Scenario: Nothing to resume
- **WHEN** no shown session is stopped or failed and the user presses resume-all
- **THEN** nothing is resumed, no tab opens, and the status line says there is nothing to resume
