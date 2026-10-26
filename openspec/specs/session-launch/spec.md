# session-launch Specification

## Purpose
Starts a named Claude Code session in a chosen repository, either on the branch already checked out or in a new sibling worktree, so that every session Vantage starts can be found again by name.

## Requirements

### Requirement: Repository is chosen from configured roots
The wizard SHALL offer every git repository's main checkout found directly under the configured repository roots, the most recently used repositories first, and a "no repository" option that starts in the roots folder. A linked worktree directly under a root SHALL NOT be offered as a repository.

#### Scenario: Filtering the picker
- **WHEN** the user types part of a repository name
- **THEN** the picker narrows to repositories whose names contain that text, case-insensitively

#### Scenario: Recently used first
- **WHEN** the picker opens
- **THEN** the ten most recently launched repositories appear before the rest

#### Scenario: Old sibling worktree under a root
- **WHEN** the root holds Northwind.Orders and its linked worktree Northwind.Orders-fix-123
- **THEN** the picker offers Northwind.Orders and does not offer Northwind.Orders-fix-123

### Requirement: A session name is mandatory
The wizard SHALL require a non-blank name, SHALL reject a name containing a semicolon or a double quote, and SHALL use the name as the session name, the tab title and the Remote Control name. A refused name SHALL be explained in a plain sentence, in the wizard and by `vantage here`, with nothing after it.

#### Scenario: Blank name
- **WHEN** the user submits a blank name
- **THEN** the wizard refuses to continue and explains that a name is required

#### Scenario: Name with a semicolon
- **WHEN** the user submits a name containing a semicolon
- **THEN** the wizard refuses it because Windows Terminal treats semicolons as command separators

#### Scenario: The explanation is only the sentence
- **WHEN** the wizard or `vantage here` refuses a name containing a semicolon or a double quote
- **THEN** the explanation is exactly "A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely.", with no parameter name or other technical text after it

#### Scenario: A blank name given to vantage here
- **WHEN** `vantage here ""` is run
- **THEN** it prints exactly "A session name is required." and starts nothing

### Requirement: A new worktree is the default launch mode
The wizard opened from the overview SHALL default to starting the session in a new worktree, and SHALL offer the repository's main checkout on whatever branch is checked out as the alternative. The wizard opened on a fixed folder SHALL offer no choice and start on that folder's current branch.

#### Scenario: Accepting the defaults
- **WHEN** the user opens the wizard with `n`, chooses Northwind.Orders, names the session "Fix 123" and accepts the other defaults
- **THEN** the session starts in a new worktree Northwind.Orders-fix-123 under the worktree root, on branch fix-123

#### Scenario: Choosing the current branch
- **WHEN** the user opens the wizard with `n`, chooses Northwind.Orders and switches Where to "Current branch"
- **THEN** the session starts in the repository's main checkout and no worktree is created

#### Scenario: Default left in place for a folder that is not a repository
- **WHEN** the user opens the wizard with `n`, chooses "(no repository)", names the session and starts without changing Where
- **THEN** the wizard starts nothing and explains that a worktree needs a repository

#### Scenario: Wizard on a fixed folder
- **WHEN** the user starts a session with `s` on a worktree row
- **THEN** the session starts in that worktree on its current branch and no new worktree is created

### Requirement: A new worktree goes under the worktree root
When the user chooses a worktree, Vantage SHALL create it at `<worktree root>\<repository>-<slug>` on a new branch named `<slug>`, creating the worktree root when it does not exist, where the slug is the name lower-cased with every run of characters other than a-z and 0-9 replaced by one hyphen and leading and trailing hyphens removed.

#### Scenario: Slug derivation
- **WHEN** the session name is "Fix #123: flaky tests!" and the worktree root is C:\Users\me\source\worktrees
- **THEN** the worktree is created at C:\Users\me\source\worktrees\Northwind.Orders-fix-123-flaky-tests on branch fix-123-flaky-tests

#### Scenario: Worktree root does not exist yet
- **WHEN** the worktree root C:\Users\me\source\worktrees does not exist and the user starts a session in a new worktree
- **THEN** the folder is created and the worktree is made inside it

#### Scenario: Name yields an empty slug
- **WHEN** the session name contains no letters or digits
- **THEN** the wizard refuses the worktree option and explains why

#### Scenario: Worktree already exists
- **WHEN** a folder already exists at the worktree path under the worktree root
- **THEN** the wizard offers to start the session in the existing worktree instead of creating one

#### Scenario: Preview names the place
- **WHEN** the user types the name fix-123 with the worktree option chosen
- **THEN** the wizard's preview names the folder Northwind.Orders-fix-123 under the worktree root and the branch fix-123

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
`vantage here [name] [-p prompt]` SHALL start a supervised session in the current folder on its current branch, named after the folder unless a name is given, record the recent repository, attach in the same tab, and exit 0 after a detach, 1 when the start failed and 2 for a usage error.

#### Scenario: Start here with a prompt
- **WHEN** the user runs vantage here fix-tests -p "Fix the tests" in a trusted repository
- **THEN** a background session named fix-tests starts there with that opening prompt and the shell is attached to it until Ctrl+Z

#### Scenario: The removed description option
- **WHEN** the user runs vantage here with -d or --description
- **THEN** it exits 2 with the unknown-option sentence and the usage line, and starts nothing

### Requirement: Untrusted workspaces are handled
When starting the session fails because the directory's workspace is not trusted, the wizard SHALL say so and offer to open a plain Claude Code tab in that directory so the trust dialog can be accepted, then retry.

#### Scenario: Workspace not trusted
- **WHEN** the CLI exits with a message that the workspace is not trusted
- **THEN** the wizard offers to open a plain Claude Code tab there and retries the start after the user confirms

#### Scenario: Folder name with an apostrophe
- **WHEN** the untrusted folder is named with a straight or curly apostrophe, such as `Bob's tools`
- **THEN** the trust tab shows its message with the folder name exactly as it is and starts Claude Code there

### Requirement: Vantage records what it created
Vantage SHALL record each worktree and branch it created and each launched repository in its own state folder, and SHALL keep every record when two Vantage processes write at the same time.

#### Scenario: Worktree recorded
- **WHEN** the wizard creates a worktree
- **THEN** the state folder records the worktree path, branch, repository and creation time

#### Scenario: Two processes record at once
- **WHEN** `vantage here` records a recent repository while the console records another recent repository and a created worktree
- **THEN** all three records are present on the next read

#### Scenario: Existing state is kept
- **WHEN** Vantage starts with recent repositories and created worktrees saved by an earlier version
- **THEN** the recent repositories are still listed first in the wizard and the created worktrees still decide the branch-deletion rules

#### Scenario: Old description files are ignored
- **WHEN** the state folder holds a `descriptions` folder or a `sessions.json` written by an earlier version
- **THEN** Vantage neither reads nor deletes them

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

### Requirement: Copied local files stay inside the new worktree
The `.worktreeinclude` copy SHALL only read files inside the repository and only write files inside the new worktree; a listed path that is parent-relative, rooted or a network path SHALL be skipped, and nothing SHALL be written outside the worktree.

#### Scenario: Parent-relative path in git's listing
- **WHEN** git's ignored-file listing names ../outside.local.json and `.worktreeinclude` matches every file
- **THEN** that path is not copied and no file appears next to the worktree

#### Scenario: Rooted and network paths in git's listing
- **WHEN** git's ignored-file listing names C:/Users/me/secrets.local.json and //server/share/secrets.local.json
- **THEN** neither is copied, and only the in-repository matches reach the worktree
