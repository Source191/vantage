# Spec Delta

## MODIFIED Requirements

### Requirement: Resume sessions after a reboot without reviving deliberate stops
The resume-all key SHALL resume every session in the failed state, SHALL present sessions in the stopped state as a checklist with none preselected because a deliberate stop also shows as stopped, and, because resuming a listed background job starts a copy, SHALL remove each chosen job and resume it by session ID under its name, which keeps the session ID, its conversation and its row. A resume that fails after the removal SHALL be reported with the session ID and the command that resumes it.

#### Scenario: Resume after restart
- **WHEN** three sessions show failed after a reboot and the user presses resume-all
- **THEN** each job is removed and resumed by session ID, and the same three rows, with the same session IDs, return to idle or working on the next refresh

#### Scenario: Stopped sessions are opt-in
- **WHEN** two sessions show stopped when the user presses resume-all
- **THEN** they are listed with unticked boxes and only the ones the user ticks are resumed

#### Scenario: No duplicate after a resume
- **WHEN** the user resumes a stopped session named fix-123 with session ID sess-1
- **THEN** the next refresh lists exactly one row named fix-123, with session ID sess-1, and no stopped copy of it

#### Scenario: Resume fails after the job was removed
- **WHEN** the user resumes a failed session named fix-123 with session ID sess-1 and the resume command fails with "Workspace not trusted"
- **THEN** the status line names fix-123, gives the failure, and says to resume it with claude --bg --resume sess-1
