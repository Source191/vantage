# Spec Delta

## ADDED Requirements

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

## REMOVED Requirements

### Requirement: Resume sessions after a reboot without reviving deliberate stops
**Reason**: After a reboot the user ticks every stopped session one by one, and a session stopped on purpose and not wanted back is better deleted; the checklist dialog goes. Its remove-then-resume behaviour and its report of a failed resume carry over to the new requirement.
**Migration**: Replaced by "Resume every shown stopped or failed session in its own tab". Filter the table with / to resume only some, press Enter to resume one in its own tab, and delete a session that should stay stopped.

## MODIFIED Requirements

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
