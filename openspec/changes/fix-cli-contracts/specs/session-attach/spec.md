## ADDED Requirements

### Requirement: A stop or removal the CLI refuses is reported and nothing follows it
When the CLI's stop or remove command exits with an error, the key that ran it SHALL report the failure with the CLI's own words, SHALL leave the session listed as it was, SHALL close no tab and remove no worktree for it, and SHALL NOT resume it, since resuming a job that is still listed starts a copy. Resume-all SHALL go on to the other sessions.

#### Scenario: Stop refused
- **WHEN** the user presses the stop key on fix-123, shown in a Vantage-opened tab, and the stop command exits with an error saying "Session 81cdb0f9 not found"
- **THEN** the tab stays open and the status line, as a failure, reads Could not stop fix-123: Session 81cdb0f9 not found

#### Scenario: Rename cannot remove the job
- **WHEN** the user renames the idle session fix-123 to fix-124 and the remove command exits with an error saying "Session 81cdb0f9 not found"
- **THEN** no resume command runs, the row keeps the name fix-123 and its tab, and the status line, as a failure, says the rename failed with that reason and that the session is still listed under its old name

#### Scenario: Resume-all cannot remove one job
- **WHEN** the user presses resume-all on the stopped sessions fix-123 and fix-124 and the remove command for fix-123's job exits with an error saying "Session 81cdb0f9 not found"
- **THEN** fix-123 is not resumed and gets no tab, fix-124 is resumed in its own tab, and the status line says one of two resumed and names fix-123 with that reason

#### Scenario: Delete cannot remove the job
- **WHEN** the user confirms deleting fix-123, which lives in a Vantage-created worktree and is shown in a Vantage-opened tab, and the remove command exits with an error saying "Session 81cdb0f9 not found"
- **THEN** the tab stays open, the worktree and its branch are untouched, the session stays listed, and the status line, as a failure, reads Could not delete fix-123: Session 81cdb0f9 not found
