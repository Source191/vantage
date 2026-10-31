## ADDED Requirements

### Requirement: The worktrees are scanned as the console opens
The console SHALL start one worktrees scan as it opens, whichever tab is shown, and that scan SHALL wait until the first sessions listing has returned, whether it succeeded or failed. After it, the worktrees SHALL be rescanned each time their tab is shown and every 15 seconds while it is shown, and never while it is hidden. Neither showing the tab nor the 15-second timer SHALL start a scan while another is running.

#### Scenario: Never opened
- **WHEN** the console has been open for ten minutes on the Sessions tab
- **THEN** the worktrees have been scanned exactly once

#### Scenario: Opened during the first scan
- **WHEN** the user presses 2 before the start-up scan has returned
- **THEN** the Worktrees tab waits for that scan, and no second scan starts

#### Scenario: A slow scan
- **WHEN** the Worktrees tab is shown and a scan of fifty repositories takes forty seconds
- **THEN** the 15-second timer starts no other scan until it returns

#### Scenario: Shown again
- **WHEN** the user leaves the Worktrees tab, runs `git worktree remove` on one of its worktrees in a shell and presses 2
- **THEN** the tab shows its earlier rows at once and drops the removed worktree when the rescan returns

#### Scenario: The sessions listing failed
- **WHEN** the first sessions listing fails as the console opens
- **THEN** the start-up scan still runs and lists the worktrees with no sessions inside them

### Requirement: A worktrees scan reuses the sessions listing
A worktrees scan SHALL take the sessions inside each worktree from the latest sessions listing the console already holds, and SHALL NOT run a Claude Code listing of its own. When no listing has succeeded yet, the scan SHALL treat every worktree as having no session.

#### Scenario: One listing per refresh
- **WHEN** the Worktrees tab is shown and its 15-second rescan runs
- **THEN** no `claude agents` process starts for the rescan, and the SESSION cells match the Sessions tab's latest listing

## MODIFIED Requirements

### Requirement: Scanning is visible
The worktrees view SHALL show a spinner with the words "Scanning worktrees" in the middle of the table while it is shown and no scan has yet returned, and SHALL show a spinner beside the status line's working text while an action started from the view runs; neither spinner SHALL appear for the rescan when the tab is shown again or for the timed rescans.

#### Scenario: First scan pending
- **WHEN** the user opens the worktrees view and the scan of the repository roots has not yet returned
- **THEN** the table area shows a spinner and "Scanning worktrees", which disappear when the first rows arrive

#### Scenario: Rescan on show
- **WHEN** the user opens the worktrees view after its first scan has returned
- **THEN** the earlier rows show at once with no spinner while the rescan runs

#### Scenario: Action running
- **WHEN** the user removes a worktree
- **THEN** the status line shows a spinner before "Removing name" until the removal reports its outcome
