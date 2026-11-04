## RENAMED Requirements

- FROM: `### Requirement: Vantage's own tab is recognisable`
- TO: `### Requirement: Torni's own tab is recognisable`

- FROM: `### Requirement: A dialog names itself in Vantage's tab`
- TO: `### Requirement: A dialog names itself in Torni's tab`

- FROM: `### Requirement: Vantage's tab title is always the same outside dialogs`
- TO: `### Requirement: Torni's tab title is always the same outside dialogs`

## MODIFIED Requirements

### Requirement: Open a session in its own Windows Terminal tab
Enter on a background session is the open-in-tab key. It SHALL open a new tab in the current Windows Terminal window that runs the attach command for the session, titled with the session name, with application title changes suppressed, starting in the session's working directory, coloured from the palette, and in the "Torni session" profile when Torni's Windows Terminal fragment is installed, otherwise in the default profile.

#### Scenario: Tab opened
- **WHEN** the user presses Enter on a background session
- **THEN** a new tab appears with the session's name as its title and the attach command running inside it, and the console stays on screen

#### Scenario: Tab opened with the setup profile
- **WHEN** `torni setup-terminal` has installed the fragment and the user presses Enter on a background session
- **THEN** the new tab uses the "Torni session" profile, so a bell in it plays that profile's sound

#### Scenario: Tab opened without the setup profile
- **WHEN** the fragment is not installed and the user presses Enter on a background session
- **THEN** the new tab uses Windows Terminal's default profile, as before

#### Scenario: Tab already live
- **WHEN** a Torni-opened tab for that session is still alive and the user presses Enter on it
- **THEN** no second tab is opened, the Windows Terminal tab titled with the session name is selected in the window that owns it, and the status line says Torni switched to it

#### Scenario: Live tab cannot be found
- **WHEN** a Torni-opened tab for that session is recorded as alive but no tab item with the session's name can be found in its Windows Terminal window
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
The console SHALL never run the attach command in Torni's own tab; a session is worked in through its own tab, and only `torni here` attaches in the tab it was run from.

#### Scenario: Enter leaves the console on screen
- **WHEN** the user presses Enter on a background session
- **THEN** Torni's own tab keeps showing the console, and the attach command runs only in the session's tab

### Requirement: Tab colours avoid colours in use
Torni SHALL choose the first palette colour not used by any live Torni-opened tab, and when every colour is in use SHALL reuse the least used colour and warn.

#### Scenario: First free colour
- **WHEN** the first two palette colours are used by live tabs
- **THEN** the next tab gets the third colour

#### Scenario: Palette exhausted
- **WHEN** every palette colour is in use
- **THEN** the least used colour is reused and the status line shows a warning

### Requirement: Live tabs are tracked by process
Torni SHALL record each tab it opens with the tab shell's process ID and start time, SHALL treat a record as live only while that process exists with the same start time, and SHALL remove stale records whenever it reads them. A tab opened for an attach SHALL close itself when the attach ends because the user detached or the session was stopped or removed, and SHALL stay open, showing the output, when the attach fails.

#### Scenario: Tab closed
- **WHEN** the user closes a Torni-opened tab
- **THEN** on the next read its record is removed and its colour becomes free

#### Scenario: Process ID reused
- **WHEN** a record's process ID now belongs to a process with a different start time
- **THEN** the record is treated as stale and removed

#### Scenario: Detach closes the tab
- **WHEN** the user presses Ctrl+Z inside a tab Torni opened for a session
- **THEN** the tab closes, the TAB column clears within one refresh, and Enter on the row opens a new tab

#### Scenario: Attach fails
- **WHEN** the attach command in a Torni-opened tab exits with a failure, for example because the session was removed before it started
- **THEN** the tab stays open with the attach command's output

### Requirement: Stop and delete resolve the session ID
Stop and delete SHALL act on the session's ID taken from the listing, never on its name, SHALL act on the row that was selected when the key was pressed even if the listing refreshed before the command ran, and SHALL close the session's Torni-opened tab when it has one.

#### Scenario: Stop
- **WHEN** the user presses the stop key on a background session
- **THEN** the CLI stop command runs with that session's ID and the row shows stopped on the next refresh

#### Scenario: Listing refreshes between key and command
- **WHEN** the user presses the stop key and a refresh replaces the rows before the stop command runs
- **THEN** the stop command still runs with the ID of the session that was selected, and nothing fails

#### Scenario: Stop closes the tab
- **WHEN** the user presses the stop key on a session shown in a Torni-opened tab
- **THEN** the session is stopped, the tab closes, and the status line says both

### Requirement: Delete offers to remove the session's worktree
Deleting a session SHALL first end its Torni-opened tab and wait for the tab's shell to exit. When the session's working directory is a worktree Torni created, delete SHALL then offer to remove that worktree under the worktree-management safety rules. When that worktree's repository no longer exists, or git cannot inspect the worktree, the session SHALL still be deleted, the worktree SHALL be kept untouched and the status line SHALL say why, in Torni's words.

#### Scenario: Clean worktree
- **WHEN** the user deletes a session in a Torni-created worktree that has no changes and no unpushed commits
- **THEN** Torni offers removal and, if accepted, removes the worktree and its branch

#### Scenario: Worktree with changes
- **WHEN** the worktree has uncommitted changes
- **THEN** the session is deleted but the worktree is kept and the row explains why

#### Scenario: Tab closed before the worktree goes
- **WHEN** the user deletes a session that was started and opened in its own tab
- **THEN** the tab closes before the worktree is removed, the worktree is removed, and the status line says the session was deleted and its tab closed

#### Scenario: Worktree without its .git
- **WHEN** the user deletes a session whose Torni-created worktree no longer has a `.git` file and whose folder holds no files
- **THEN** the session is deleted, the folder is deleted, the stale registration is pruned from the repository, and the status line says so

#### Scenario: Folder still in use
- **WHEN** the user deletes a session while a PowerShell tab opened from the worktrees view sits in its worktree
- **THEN** the session is deleted, the status line says the worktree folder is still in use by a tab or program and names it, and pressing remove on it in the worktrees view after closing that tab deletes what is left

#### Scenario: The worktree's repository has gone
- **WHEN** the user deletes a session in the Torni-created worktree C:\Users\me\source\worktrees\Northwind.Orders-fix-123 and its repository C:\Users\me\source\repos\Northwind.Orders has been deleted since the worktree was created
- **THEN** the session is deleted, git is not asked anything, the folder and its files are kept, and the status line says the worktree was kept because its repository C:\Users\me\source\repos\Northwind.Orders no longer exists and that the Worktrees tab removes it

#### Scenario: Git fails while the worktree is inspected
- **WHEN** the user deletes a session in a Torni-created worktree and git fails while listing the repository's worktrees
- **THEN** the session is deleted, nothing is removed or deleted, and the status line says the worktree was kept because git could not inspect it, giving git's reason, with no "Failed:" line

### Requirement: Rename without attaching
The rename key SHALL ask for a new name in a dialog and then, because the CLI has no rename verb and resuming a listed background job with new flags starts a copy, SHALL remove the job and resume it by session ID under the new name, which keeps the session ID and its conversation.

#### Scenario: Rename an idle session
- **WHEN** the user presses the rename key on an idle background session and types a new name
- **THEN** the job is removed and resumed by session ID with the new name, and the row shows the new name with the same session ID on the next refresh

#### Scenario: Rename a session with an open tab
- **WHEN** the user renames an idle session whose Torni-opened tab is alive
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

### Requirement: Torni's own tab is recognisable
When the console starts in a Windows Terminal tab, Torni SHALL title that tab "Torni" and colour it green when no live Torni-opened tab is green, otherwise with the first free palette colour; SHALL count its own tab's colour as in use when colouring session tabs; and SHALL clear the title and reset the colour when it quits, so the tab shows its profile's own colour again.

#### Scenario: Green is free
- **WHEN** Torni starts and no live Torni-opened tab is green
- **THEN** the tab is titled Torni and coloured green, and the next session tab opened gets a colour other than green

#### Scenario: Green is in use
- **WHEN** Torni starts while a live Torni-opened tab is green
- **THEN** the tab is titled Torni, and coloured with the first palette colour no live tab uses

#### Scenario: Local build
- **WHEN** the installed tool's version is 0.12.0-local.20261026T101500
- **THEN** the tab is titled Torni, and the version, with its prerelease label, is shown only in the header

#### Scenario: A dialog is open
- **WHEN** the user presses the help key
- **THEN** the tab reads Torni (Keys) while the help is shown, and Torni again once it is closed

#### Scenario: Each dialog has its own word
- **WHEN** the new-session wizard, rename, settings, delete session, delete branch, close empty, force remove, open solution or update dialog is open
- **THEN** the tab reads Torni (New session), Torni (Rename), Torni (Settings), Torni (Delete session), Torni (Delete branch), Torni (Close empty), Torni (Force remove), Torni (Open solution) or Torni (Update) respectively

#### Scenario: Title restored after a hand-off
- **WHEN** the user opens a session's tab with Enter, works in it while Claude sets that tab's title, and selects the Torni tab again
- **THEN** the Torni tab is still titled Torni and coloured as before, because the attach ran in the other tab

#### Scenario: Quit
- **WHEN** the user quits Torni
- **THEN** the tab's title is cleared, the tab shows its profile's colour again because the palette entry Torni coloured is reset rather than only re-pointed, and its registration goes stale with the process

### Requirement: A tab runs its values as literal text
Every value Torni places in the script of a tab it opens (an executable path, a session's job id, a registration id, a folder or repository name) SHALL reach the program as the same text, whatever characters it holds. No value, including one containing a straight or curly single quote, SHALL end the literal it sits in or be run as a command.

#### Scenario: Path with a curly apostrophe
- **WHEN** the Claude executable lives under a profile folder whose name contains a curly apostrophe, such as `C:\Users\O’Neill`
- **THEN** the session's tab runs that executable from that exact path and runs nothing else from the path's text

#### Scenario: Value that looks like a command
- **WHEN** a value in a tab's script holds a quote followed by PowerShell, such as `x'; Remove-Item C:\temp; '`
- **THEN** the tab prints or passes that text unchanged and does not run the command

### Requirement: The open-in-tab key switches to a live tab
When a Torni-opened tab for the session is alive at the moment the open-in-tab key is pressed, the key SHALL switch to that tab instead of opening another; a record whose shell has gone SHALL be dropped and a new tab opened. Opening a tab SHALL drop any earlier record for the same session.

#### Scenario: Pressed twice
- **WHEN** the user presses the open-in-tab key on a background session, and presses it again on the same row while the tab it opened is still alive
- **THEN** the second press opens no tab and selects the tab the first press opened

### Requirement: Delete clears a worktree whose .git has gone
When the session's Torni-created worktree no longer has its `.git`, delete SHALL treat its folder as a leftover: a folder that holds no files SHALL be deleted and its stale registration pruned from the repository, and a folder that still holds files SHALL be kept, with the status line saying how many untracked files would be lost and that force-remove on the Worktrees tab deletes it. A worktree that git does not list SHALL be judged by its folder on disc, never taken to be gone.

#### Scenario: The worktree is no longer listed
- **WHEN** the user deletes a session whose Torni-created worktree has lost its `.git` file and whose folder holds no files
- **THEN** the folder is gone and the repository no longer lists the worktree

#### Scenario: The folder still holds work
- **WHEN** the user deletes a session whose Torni-created worktree has lost its `.git` file and whose folder still holds twelve files
- **THEN** the session is deleted, the folder and its files are kept, and the status line says twelve untracked files would be lost and that force-remove on the Worktrees tab deletes it

#### Scenario: Git spells the worktree's path differently
- **WHEN** the user deletes a session whose Torni-created worktree is listed by git under a different spelling of its path, such as the real folder behind a junction, and the folder holds uncommitted work
- **THEN** the session is deleted and the folder and its work are kept

### Requirement: Delete leaves a folder in use for a later remove
A worktree folder git cannot delete because a tab or program still uses it SHALL be reported as in use, with the session deleted and the folder left for a later remove.

#### Scenario: A program holds the folder
- **WHEN** the user deletes a session in a Torni-created worktree while a program still uses that folder, so git cannot delete it
- **THEN** the session is deleted, the status line says the folder is still in use, and the worktree is still listed in the worktrees view

### Requirement: Rename moves the session's tab
A session shown in a Torni-opened tab SHALL have that tab closed after the job is removed, and a tab opened under the new name, in the same colour, once the session is resumed.

#### Scenario: A blue tab renamed
- **WHEN** the user renames the idle session fix-123, shown in a live blue Torni-opened tab, to fix-124
- **THEN** the tab titled fix-123 closes and a blue tab titled fix-124 opens

### Requirement: A new name is checked before anything runs
A new name the launch rules would refuse SHALL be refused before anything runs, with the same sentence `torni here` gives for that name.

#### Scenario: The same words as vantage here
- **WHEN** the user leaves the new name blank in the rename dialog
- **THEN** no job is removed or resumed, and the status line shows the sentence `torni here` gives for a blank name

### Requirement: A dialog names itself in Torni's tab
While a dialog is open Torni's tab SHALL read "Torni" followed by the dialog's short name in brackets, and closing the dialog SHALL put "Torni" back; no dialog's own title SHALL reach the tab.

#### Scenario: The folder picker
- **WHEN** the user presses F3 in the repository roots dialog
- **THEN** the tab reads Torni (Browse) while the picker is open, never the picker's own title, and Torni (Settings) again once it closes

### Requirement: Torni's tab title is always the same outside dialogs
Outside a dialog, every title Torni writes for its own tab SHALL be the same text.

#### Scenario: Start and dialog
- **WHEN** Torni writes its tab title as it starts and after a dialog closes
- **THEN** the title is Torni each time

### Requirement: Resume every shown stopped or failed session in its own tab
The resume-all key SHALL, without a dialog, resume every background session in the stopped or failed state that the table shows, in the table's order, and SHALL open a Torni tab for each one the CLI resumes. It SHALL remove each job before resuming it by session ID, since resuming a listed job starts a copy. A resume that fails after the removal SHALL get no tab and SHALL be reported with the command that resumes it. With none shown, the status line SHALL say there is nothing to resume.

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

### Requirement: The first tab colours stay apart for colour-blind users
The tab palette SHALL be ordered Red, Blue, Green, Teal, Pink, Brown, Purple, Orange, Lime, Grey, Yellow, so that Torni's own green tab and the first five session tabs given the first free colours differ from one another for people with protanopia or deuteranopia as well as for normal colour vision.

#### Scenario: Five session tabs beside Vantage's green
- **WHEN** Torni's own tab is green and the user opens five session tabs, none closing
- **THEN** the session tabs are red, blue, teal, pink and brown

#### Scenario: A tab already open keeps its colour
- **WHEN** a session tab was coloured yellow by an earlier version and is still live as Torni starts
- **THEN** it stays yellow, and yellow counts as in use when the next tab's colour is chosen

### Requirement: A stop or removal the CLI refuses is reported and nothing follows it
When the CLI's stop or remove command exits with an error, the key that ran it SHALL report the failure with the CLI's own words, SHALL leave the session listed as it was, SHALL close no tab and remove no worktree for it, and SHALL NOT resume it, since resuming a job that is still listed starts a copy. Resume-all SHALL go on to the other sessions.

#### Scenario: Stop refused
- **WHEN** the user presses the stop key on fix-123, shown in a Torni-opened tab, and the stop command exits with an error saying "Session 81cdb0f9 not found"
- **THEN** the tab stays open and the status line, as a failure, reads Could not stop fix-123: Session 81cdb0f9 not found

#### Scenario: Rename cannot remove the job
- **WHEN** the user renames the idle session fix-123 to fix-124 and the remove command exits with an error saying "Session 81cdb0f9 not found"
- **THEN** no resume command runs, the row keeps the name fix-123 and its tab, and the status line, as a failure, says the rename failed with that reason and that the session is still listed under its old name

#### Scenario: Resume-all cannot remove one job
- **WHEN** the user presses resume-all on the stopped sessions fix-123 and fix-124 and the remove command for fix-123's job exits with an error saying "Session 81cdb0f9 not found"
- **THEN** fix-123 is not resumed and gets no tab, fix-124 is resumed in its own tab, and the status line says one of two resumed and names fix-123 with that reason

#### Scenario: Delete cannot remove the job
- **WHEN** the user confirms deleting fix-123, which lives in a Torni-created worktree and is shown in a Torni-opened tab, and the remove command exits with an error saying "Session 81cdb0f9 not found"
- **THEN** the tab stays open, the worktree and its branch are untouched, the session stays listed, and the status line, as a failure, reads Could not delete fix-123: Session 81cdb0f9 not found
