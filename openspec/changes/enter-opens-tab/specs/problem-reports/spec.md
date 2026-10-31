# Spec Delta

## MODIFIED Requirements

### Requirement: The report holds only diagnostics
The diagnostics text SHALL hold only the environment (the Vantage, Claude Code, Windows and .NET versions, the terminal's size, the editor preset, whether update checks are on and the number of repository roots), the recorded diagnostic events, the child program counts and the last crash when there is one, rendered as the diagnostic log renders them. The title SHALL be the only text the user typed, and it SHALL NOT be recorded in the diagnostic log.

#### Scenario: Preview after some work
- **WHEN** the user has opened "Fix flaky tests" in C:\Users\me\source\worktrees\Northwind.Orders-fix-123 in its tab and a worktree remove has failed, then presses `!`
- **THEN** the preview shows the versions, the open-in-tab effect, the failed remove with its exception summary and the counts, and neither "Fix flaky tests", "Northwind.Orders" nor "C:\Users\me" appears in it

#### Scenario: The title is not logged
- **WHEN** the user types the title "Attach froze after Ctrl+Z" and opens the report
- **THEN** the diagnostic log records that a report was opened and does not contain the title
