# Spec Delta

## MODIFIED Requirements

### Requirement: Forced removal needs a typed confirmation
The force-remove key SHALL remove a worktree despite changes or unpushed commits only after the user enters the worktree's folder name to confirm (shown selected in the dialog so it can be copied and pasted; a wrong or empty answer keeps the dialog open and says what was typed), and SHALL never force-remove a locked worktree or one with a live session. A refusal that cannot be forced SHALL NOT suggest force-remove in the footer.

#### Scenario: Confirmed force
- **WHEN** the user presses force-remove and enters the exact folder name
- **THEN** the worktree is removed along with its changes

#### Scenario: Wrong answer
- **WHEN** the user enters anything else
- **THEN** nothing is removed, the dialog stays open and says what was typed, and Esc keeps the worktree

#### Scenario: Live session inside
- **WHEN** the user presses force-remove on a worktree that a live session is using
- **THEN** removal is refused and the footer names the session

#### Scenario: Refusal that cannot be forced
- **WHEN** plain remove is refused because the worktree is locked or a live session is using it
- **THEN** the footer names the reason and does not mention force-remove, whatever words the reason uses

## ADDED Requirements

### Requirement: Enter on a worktree row goes to its session by identity
Enter on a worktree row with a running session SHALL switch to the sessions view with that session selected by its session id, so two sessions sharing a name cannot be confused.

#### Scenario: Two sessions share a name
- **WHEN** two sessions are both named fix-tests and one runs in the selected worktree
- **THEN** the sessions view opens with the one whose working directory is that worktree selected

### Requirement: Counting a leftover folder's files is bounded
The worktree scan SHALL stop counting a leftover folder's files at a fixed limit, so a refresh never walks a whole dependencies tree, and a refusal for such a folder SHALL say that at least that many files would be lost.

#### Scenario: Large leftover
- **WHEN** an orphan folder holds many thousands of files under a dependencies folder
- **THEN** the refresh stops counting at the limit, and pressing remove says at least that many files would be lost

#### Scenario: Small leftover
- **WHEN** an orphan folder holds twelve files
- **THEN** pressing remove says twelve files would be lost
