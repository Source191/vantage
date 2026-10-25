# Spec Delta

## MODIFIED Requirements

### Requirement: A session can be started from the shell in the current folder
`vantage here [name] [-p prompt]` SHALL start a supervised session in the current folder on its current branch, named after the folder unless a name is given, record the recent repository, attach in the same tab, and exit 0 after a detach, 1 when the start failed and 2 for a usage error.

#### Scenario: Start here with a prompt
- **WHEN** the user runs vantage here fix-tests -p "Fix the tests" in a trusted repository
- **THEN** a background session named fix-tests starts there with that opening prompt and the shell is attached to it until Ctrl+Z

#### Scenario: The removed description option
- **WHEN** the user runs vantage here with -d or --description
- **THEN** it exits 2 with the unknown-option sentence and the usage line, and starts nothing

### Requirement: Vantage records what it created
Vantage SHALL record each worktree and branch it created and each launched repository in its own state folder, and SHALL keep every record when two Vantage processes write at the same time.

#### Scenario: Worktree recorded
- **WHEN** the wizard creates a worktree
- **THEN** the state folder records the worktree path, branch, repository and creation time

#### Scenario: Two processes record at once
- **WHEN** `vantage here` records a recent repository while the console records another
- **THEN** both records are present on the next read

#### Scenario: Existing state is kept
- **WHEN** Vantage starts with recent repositories saved by an earlier version
- **THEN** they are still listed first in the wizard

#### Scenario: Old description files are ignored
- **WHEN** the state folder holds a `descriptions` folder or a `sessions.json` written by an earlier version
- **THEN** Vantage neither reads nor deletes them

## REMOVED Requirements

### Requirement: A description is optional and travels with the session
**Reason**: The product owner does not use the description. It carried a wizard field, a command-line option, a state port and folder and a column for no benefit.
**Migration**: None. Files under `descriptions\` are left in place and ignored; delete the folder by hand if wanted.
