# session-launch Specification

## Purpose
Starts a named Claude Code session in a chosen repository, either on the branch already checked out or in a new sibling worktree, so that every session Vantage starts can be found again by name.

## Requirements

### Requirement: Repository is chosen from configured roots
The wizard SHALL offer every git repository found directly under the configured repository roots, the most recently used repositories first, and a "no repository" option that starts in the roots folder.

#### Scenario: Filtering the picker
- **WHEN** the user types part of a repository name
- **THEN** the picker narrows to repositories whose names contain that text, case-insensitively

#### Scenario: Recently used first
- **WHEN** the picker opens
- **THEN** the five most recently launched repositories appear before the rest

### Requirement: A session name is mandatory
The wizard SHALL require a non-blank name, SHALL reject a name containing a semicolon or a double quote, and SHALL use the name as the session name, the tab title and the Remote Control name.

#### Scenario: Blank name
- **WHEN** the user submits a blank name
- **THEN** the wizard refuses to continue and explains that a name is required

#### Scenario: Name with a semicolon
- **WHEN** the user submits a name containing a semicolon
- **THEN** the wizard refuses it because Windows Terminal treats semicolons as command separators

### Requirement: A description is optional and travels with the session
The wizard SHALL accept an optional free-text description of what the session is for, and Vantage SHALL store it against the session's ID in its own state so that the listing and the notification toast can show it.

#### Scenario: Description given
- **WHEN** the user enters "Replace the login page" as the description
- **THEN** the description is stored against the new session's ID and appears in the listing's peek text for that session

#### Scenario: Description left blank
- **WHEN** the user leaves the description blank
- **THEN** the session starts normally and the listing shows no description for it

### Requirement: The current branch is the default launch mode
The wizard SHALL default to starting the session in the repository's main checkout on whatever branch is checked out, and SHALL offer a new worktree as the alternative.

#### Scenario: Accepting the defaults
- **WHEN** the user accepts the wizard's defaults
- **THEN** the session starts in the repository's main checkout and no worktree is created

### Requirement: A new worktree is a sibling of the repository
When the user chooses a worktree, Vantage SHALL create it at `<repository>-<slug>` beside the repository on a new branch named `<slug>`, where the slug is the name lower-cased with every run of characters other than a-z and 0-9 replaced by one hyphen and leading and trailing hyphens removed.

#### Scenario: Slug derivation
- **WHEN** the session name is "Fix #123: flaky tests!"
- **THEN** the worktree is created at Northwind.Orders-fix-123-flaky-tests beside Northwind.Orders on branch fix-123-flaky-tests

#### Scenario: Name yields an empty slug
- **WHEN** the session name contains no letters or digits
- **THEN** the wizard refuses the worktree option and explains why

#### Scenario: Worktree already exists
- **WHEN** a folder already exists at the sibling path
- **THEN** the wizard offers to start the session in the existing worktree instead of creating one

### Requirement: Worktree base branch is configurable
The worktree SHALL branch from the remote default branch by default, or from the repository's current HEAD when the user chooses that in the wizard.

#### Scenario: Default base
- **WHEN** the user does not change the base
- **THEN** the new branch starts at origin's default branch

### Requirement: Gitignored local files are copied into a new worktree
When the repository contains a `.worktreeinclude` file, Vantage SHALL copy the gitignored files it matches into the new worktree before starting the session.

#### Scenario: Include file present
- **WHEN** `.worktreeinclude` lists appsettings.Development.json and that file is gitignored in the main checkout
- **THEN** the file exists in the new worktree before the session starts

### Requirement: A session can be started from the shell in the current folder
`vantage here [name] [-p prompt] [-d description]` SHALL start a supervised session in the current folder on its current branch, named after the folder unless a name is given, record the description and the recent repository, attach in the same tab, and exit 0 after a detach, 1 when the start failed and 2 for a usage error.

#### Scenario: Start here with a prompt
- **WHEN** the user runs vantage here fix-tests -p "Fix the tests" in a trusted repository
- **THEN** a background session named fix-tests starts there with that opening prompt and the shell is attached to it until Ctrl+Z

### Requirement: Untrusted workspaces are handled
When starting the session fails because the directory's workspace is not trusted, the wizard SHALL say so and offer to open a plain Claude Code tab in that directory so the trust dialog can be accepted, then retry.

#### Scenario: Workspace not trusted
- **WHEN** the CLI exits with a message that the workspace is not trusted
- **THEN** the wizard offers to open a plain Claude Code tab there and retries the start after the user confirms

### Requirement: Vantage records what it created
Vantage SHALL record each worktree and branch it created, and each launched repository, in its own state folder.

#### Scenario: Worktree recorded
- **WHEN** the wizard creates a worktree
- **THEN** the state folder records the worktree path, branch, repository and creation time

### Requirement: The session starts under the supervisor and opens where the user chose
Vantage SHALL start the session as a named background session in the chosen directory, passing the opening prompt when one was given and starting an idle session when none was, and SHALL then open the session in its own Windows Terminal tab or return to the list as the user chose.

#### Scenario: No opening prompt
- **WHEN** the user leaves the opening prompt blank
- **THEN** the session starts idle and waits for input under the given name

#### Scenario: Start and open in a tab
- **WHEN** the user chooses "start and open in tab"
- **THEN** Vantage starts the session, opens it in a new Windows Terminal tab titled with the session name as the open-in-tab key does, and the sessions view stays on screen with the footer naming the tab's colour

#### Scenario: Started session not listed yet
- **WHEN** the user chooses "start and open in tab" and the supervisor has not yet listed the new session when the start returns
- **THEN** the footer reports the session as started and says to open it with the open-in-tab key once it appears, and no tab is opened
