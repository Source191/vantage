# session-launch Specification

## Purpose
Starts a named Claude Code session in a chosen repository, either on the branch already checked out or in a new sibling worktree, so that every session Vantage starts can be found again by name.

## Requirements

### Requirement: Repository is chosen from configured roots
The wizard SHALL offer every git repository's main checkout found directly under the configured repository roots, the most recently used repositories first, and SHALL NOT offer a root folder itself or a linked worktree directly under a root.

#### Scenario: Filtering the picker
- **WHEN** the user types part of a repository name
- **THEN** the picker narrows to repositories whose names contain that text, case-insensitively

#### Scenario: Recently used first
- **WHEN** the picker opens
- **THEN** the ten most recently launched repositories appear before the rest

#### Scenario: Old sibling worktree under a root
- **WHEN** the root holds Northwind.Orders and its linked worktree Northwind.Orders-fix-123
- **THEN** the picker offers Northwind.Orders and does not offer Northwind.Orders-fix-123

#### Scenario: Roots are not choices
- **WHEN** the picker opens with the root C:\Users\me\source\repos
- **THEN** no "(no repository)" entry is listed; a session in the root folder itself is started with vantage here from that folder

### Requirement: A session name is mandatory
The wizard SHALL require a non-blank name, SHALL accept any other name, including one holding a semicolon or a double quote, and SHALL use the name exactly as typed as the session name, the tab title and the Remote Control name. A refused name SHALL be explained in a plain sentence, in the wizard and by `vantage here`, with nothing after it.

#### Scenario: Blank name
- **WHEN** the user submits a blank name
- **THEN** the wizard refuses to continue and explains that a name is required

#### Scenario: Name with a semicolon
- **WHEN** the user submits the name `fix a;b` and opens the session in a tab
- **THEN** the session is named `fix a;b`, exactly one tab opens, and it is titled `fix a;b`

#### Scenario: Name with a double quote
- **WHEN** the user submits the name `say "hi"` and opens the session in a tab
- **THEN** the session is named `say "hi"` and its tab is titled `say "hi"`

#### Scenario: The explanation is only the sentence
- **WHEN** the wizard or `vantage here` refuses a blank name
- **THEN** the explanation is exactly "A session name is required.", with no parameter name or other technical text after it, and it fits on the wizard's problem line on a 120-column terminal

#### Scenario: A blank name given to vantage here
- **WHEN** `vantage here ""` is run
- **THEN** it prints exactly "A session name is required." and starts nothing

### Requirement: A new worktree is the default launch mode
The wizard opened from the overview SHALL start on the default place setting, a new worktree unless the user has chosen the current branch, and SHALL offer the other as the alternative: a new worktree, or the repository's main checkout on whatever branch is checked out. The wizard opened on a fixed folder SHALL offer no choice and start on that folder's current branch, whatever the setting.

#### Scenario: Accepting the defaults
- **WHEN** the default place is New worktree and the user opens the wizard with `n`, chooses Northwind.Orders, names the session "Fix 123" and accepts the other defaults
- **THEN** the session starts in a new worktree Northwind.Orders-fix-123 under the worktree root, on branch fix-123

#### Scenario: Choosing the current branch
- **WHEN** the user opens the wizard with `n`, chooses Northwind.Orders and switches Where to "Current branch"
- **THEN** the session starts in the repository's main checkout and no worktree is created

#### Scenario: Current branch by default
- **WHEN** the default place is Current branch and the user opens the wizard with `n`, chooses Northwind.Orders and names the session
- **THEN** Where reads Current branch and the session starts in the main checkout unless the user switches it

#### Scenario: Default left in place for a folder that is not a repository
- **WHEN** the user opens the wizard with `n`, chooses "(no repository)", names the session and starts without changing Where while it reads New worktree
- **THEN** the wizard starts nothing and explains that a worktree needs a repository

#### Scenario: Wizard on a fixed folder
- **WHEN** the default place is New worktree and the user starts a session with `s` on a worktree row
- **THEN** the session starts in that worktree on its current branch and no new worktree is created

#### Scenario: Fixed folder without a tab
- **WHEN** the user starts a session with `s` on a worktree row, types a name and presses F5
- **THEN** the session starts in that worktree and opens in its own tab, as on Enter, because the wizard has no way to start without one

#### Scenario: Fixed folder dialog is compact
- **WHEN** the wizard opens from a worktree row
- **THEN** the dialog shows the directory, the name, the preview and the prompt from its left edge, with no empty repository column, and its hint line says Enter or F5 starts and opens in a tab

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

#### Scenario: Names with non-ASCII characters, spaces and apostrophes
- **WHEN** `.worktreeinclude` matches the gitignored files Café.local.json, my app.local.json and it's.local.json
- **THEN** all three exist in the new worktree under the same names, and the copy reports all three

### Requirement: A session can be started from the shell in the current folder
`vantage here [name] [-p prompt]` SHALL start a supervised session in the current folder on its current branch, named after the folder unless a name is given, record the recent repository, attach in the same tab, and exit 0 after a detach, 1 when the start failed or the command line could not be parsed, and 2 when the name or the opening prompt is refused.

#### Scenario: Start here with a prompt
- **WHEN** the user runs vantage here fix-tests -p "Fix the tests" in a trusted repository
- **THEN** a background session named fix-tests starts there with that opening prompt and the shell is attached to it until Ctrl+Z

#### Scenario: The removed description option
- **WHEN** the user runs vantage here fix-tests -d "Flaky orders suite"
- **THEN** it exits 1 naming what was not recognised, and starts nothing

#### Scenario: A refused name
- **WHEN** the user runs vantage here "a;b"
- **THEN** it exits 2 with the sentence for the refused name, and starts nothing

#### Scenario: A refused prompt
- **WHEN** the user runs vantage here fix-tests -p "-fix the tests" in a trusted repository
- **THEN** it exits 2 with the sentence that an opening prompt cannot start with a dash, and starts nothing

### Requirement: vantage here says how to detach
Before attaching, `vantage here` SHALL print one line saying that Ctrl+Z detaches and the session keeps running, and SHALL erase that line when the attach exits.

#### Scenario: The line before attaching
- **WHEN** vantage here starts a session and attaches
- **THEN** the last line printed before handing over is exactly "Ctrl+Z detaches; the session keeps running.", and it is gone from the terminal once the attach exits

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

### Requirement: The session starts under the supervisor
Vantage SHALL start the session as a named background session in the chosen directory, passing the opening prompt when one was given and starting an idle session when none was. An opening prompt that starts with a dash SHALL be refused before anything starts, in the wizard and by `vantage here`, with a sentence saying the CLI would read it as options and to start it with a word.

#### Scenario: No opening prompt
- **WHEN** the user leaves the opening prompt blank
- **THEN** the session starts idle and waits for input under the given name

#### Scenario: Prompt that starts with a dash
- **WHEN** the user types the opening prompt "- fix the tests" or "--dangerously-skip-permissions" and starts
- **THEN** nothing starts and the wizard says an opening prompt cannot start with a dash because Claude Code would read it as options

#### Scenario: Prompt with a dash inside it
- **WHEN** the user types the opening prompt "Fix the tests - all of them"
- **THEN** the session starts with that prompt

### Requirement: A session started from the wizard opens in its own tab
A session started from the wizard SHALL be opened in its own Windows Terminal tab as soon as the supervisor lists it, and the wizard SHALL have no way to start one without a tab; `vantage here` attaches in its own tab instead.

#### Scenario: Start and open in a tab
- **WHEN** the user starts a session from the wizard
- **THEN** Vantage starts the session, opens it in a new Windows Terminal tab titled with the session name as the open-in-tab key does, and the sessions view stays on screen with the footer naming the tab's colour

#### Scenario: No start without a tab
- **WHEN** the user presses F6 in the wizard
- **THEN** nothing starts and the wizard stays open

#### Scenario: Started session not listed yet
- **WHEN** the user starts a session from the wizard and the supervisor has not yet listed the new session when the start returns
- **THEN** the footer reports the session as started and says to press Enter on it once it appears, and no tab is opened

### Requirement: Copied local files stay inside the new worktree
The `.worktreeinclude` copy SHALL only read files inside the repository and only write files inside the new worktree; a listed path that is parent-relative, rooted or a network path SHALL be skipped, and nothing SHALL be written outside the worktree.

#### Scenario: Parent-relative path in git's listing
- **WHEN** git's ignored-file listing names ../outside.local.json and `.worktreeinclude` matches every file
- **THEN** that path is not copied and no file appears next to the worktree

#### Scenario: Rooted and network paths in git's listing
- **WHEN** git's ignored-file listing names C:/Users/me/secrets.local.json and //server/share/secrets.local.json
- **THEN** neither is copied, and only the in-repository matches reach the worktree

### Requirement: The wizard's keys move through the form in order
Tab and Enter SHALL move focus through the wizard in its reading order: from the repository list to the name, from the name to Where when it is shown and otherwise to the prompt, from Where to the base, and from the base to the prompt. Enter SHALL never close the wizard or start a session, except in a fixed folder's name field, and only F5 and the Start button SHALL start one.

#### Scenario: Tab from the list
- **WHEN** the user presses Tab on the repository list
- **THEN** focus moves to the name field, as it does on Enter

#### Scenario: Enter on Where
- **WHEN** the user presses Enter on the Where selector
- **THEN** focus moves to the worktree base and the wizard stays open

#### Scenario: Enter on the base
- **WHEN** the user presses Enter on the worktree base selector
- **THEN** focus moves to the opening prompt and the wizard stays open

#### Scenario: Enter in the prompt
- **WHEN** the user presses Enter in the opening prompt
- **THEN** the prompt gains a new line and the wizard stays open

#### Scenario: The hint names F5
- **WHEN** the user opens the wizard with n
- **THEN** the hint line names F5 as starting the session and opening it in a tab, and does not name F6

### Requirement: The wizard reads as one form
The wizard opened from the overview SHALL lay its controls out in one column in reading order: the repository filter with its list beneath, the session name, Where, the worktree base, the preview, the opening prompt, the problem line, the hint, and the buttons Start (F5) and Cancel. The dialog SHALL fit a 120 by 36 terminal with at least four repository rows visible, and Enter SHALL never start a session in it.

#### Scenario: Reading order
- **WHEN** the user opens the wizard with n
- **THEN** the filter is above the repository list, which is above the name, Where, the base, the preview and the prompt, in one column

#### Scenario: Fits the laptop screen
- **WHEN** the wizard opens on a 120 by 36 terminal with twelve repositories under the roots
- **THEN** every control and the buttons are on screen and at least four repositories are listed

#### Scenario: One start button
- **WHEN** the user opens the wizard with n
- **THEN** the buttons are Start (F5) and Cancel, and no button starts a session without opening its tab

#### Scenario: Enter in the prompt
- **WHEN** the user presses Enter in the opening prompt
- **THEN** the prompt gains a new line and nothing starts

#### Scenario: The compact form is unchanged
- **WHEN** the user presses s on a worktree row
- **THEN** the compact fixed-folder dialog opens as before, and Enter in its name starts the session and opens its tab
