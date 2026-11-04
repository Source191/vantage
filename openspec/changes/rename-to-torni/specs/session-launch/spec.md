## RENAMED Requirements

- FROM: `### Requirement: vantage here says how to detach`
- TO: `### Requirement: torni here says how to detach`

- FROM: `### Requirement: Vantage records what it created`
- TO: `### Requirement: Torni records what it created`

## MODIFIED Requirements

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
- **THEN** no "(no repository)" entry is listed; a session in the root folder itself is started with torni here from that folder

### Requirement: A session name is mandatory
The wizard SHALL require a non-blank name, SHALL accept any other name, including one holding a semicolon or a double quote, and SHALL use the name exactly as typed as the session name, the tab title and the Remote Control name. A refused name SHALL be explained in a plain sentence, in the wizard and by `torni here`, with nothing after it.

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
- **WHEN** the wizard or `torni here` refuses a blank name
- **THEN** the explanation is exactly "A session name is required.", with no parameter name or other technical text after it, and it fits on the wizard's problem line on a 120-column terminal

#### Scenario: A blank name given to vantage here
- **WHEN** `torni here ""` is run
- **THEN** it prints exactly "A session name is required." and starts nothing

### Requirement: A new worktree goes under the worktree root
When the user chooses a worktree, Torni SHALL create it at `<worktree root>\<repository>-<slug>` on a new branch named `<slug>`, creating the worktree root when it does not exist, where the slug is the name lower-cased with every run of characters other than a-z and 0-9 replaced by one hyphen and leading and trailing hyphens removed.

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

### Requirement: Gitignored local files are copied into a new worktree
When the repository contains a `.worktreeinclude` file, Torni SHALL copy the gitignored files it matches into the new worktree before starting the session. When the copy fails because a file cannot be read or written, the session SHALL NOT start, the worktree SHALL be kept and remain recorded as Torni's own, and the status line SHALL say the session could not start and give the reason.

#### Scenario: Include file present
- **WHEN** `.worktreeinclude` lists appsettings.Development.json and that file is gitignored in the main checkout
- **THEN** the file exists in the new worktree before the session starts

#### Scenario: Names with non-ASCII characters, spaces and apostrophes
- **WHEN** `.worktreeinclude` matches the gitignored files Café.local.json, my app.local.json and it's.local.json
- **THEN** all three exist in the new worktree under the same names, and the copy reports all three

#### Scenario: A file cannot be copied
- **WHEN** `.worktreeinclude` matches a file that another program holds locked, so the copy fails
- **THEN** no session starts, the worktree stays on disc and in Torni's records, and the status line says the session could not start and why

### Requirement: A session can be started from the shell in the current folder
`torni here [name] [-p prompt]` SHALL start a supervised session in the current folder on its current branch, named after the folder unless a name is given, record the recent repository, attach in the same tab, and exit 0 after a detach, 1 when the start failed or the command line could not be parsed, and 2 when the name or the opening prompt is refused.

#### Scenario: Start here with a prompt
- **WHEN** the user runs torni here fix-tests -p "Fix the tests" in a trusted repository
- **THEN** a background session named fix-tests starts there with that opening prompt and the shell is attached to it until Ctrl+Z

#### Scenario: The removed description option
- **WHEN** the user runs torni here fix-tests -d "Flaky orders suite"
- **THEN** it exits 1 naming what was not recognised, and starts nothing

#### Scenario: A refused name
- **WHEN** the user runs torni here "a;b"
- **THEN** it exits 2 with the sentence for the refused name, and starts nothing

#### Scenario: A refused prompt
- **WHEN** the user runs torni here fix-tests -p "-fix the tests" in a trusted repository
- **THEN** it exits 2 with the sentence that an opening prompt cannot start with a dash, and starts nothing

### Requirement: torni here says how to detach
Before attaching, `torni here` SHALL print one line saying that Ctrl+Z detaches and the session keeps running, and SHALL erase that line when the attach exits, whether it exited because the user detached, because it could not start, or because it was interrupted. In every one of those cases the terminal SHALL be put back in order (focus reporting off, the cursor shown, late replies discarded) before `torni here` returns.

#### Scenario: The line before attaching
- **WHEN** torni here starts a session and attaches
- **THEN** the last line printed before handing over is exactly "Ctrl+Z detaches; the session keeps running.", and it is gone from the terminal once the attach exits

#### Scenario: The attach cannot start
- **WHEN** torni here has started a session and the attach command cannot be started, for example because the Claude executable was removed between the start and the attach
- **THEN** the "Ctrl+Z detaches" line is gone from the terminal, and the cursor is shown and focus reporting is off when the shell prompt returns

### Requirement: Torni records what it created
Torni SHALL record each worktree and branch it created and each launched repository in its own state folder, and SHALL keep every record when two Torni processes write at the same time. A new worktree SHALL be recorded as soon as git has created it, before its include files are copied, so a worktree that exists is never one Torni does not know it made. A list of created worktrees saved by an earlier version SHALL be read once, kept as records and removed.

#### Scenario: Worktree recorded
- **WHEN** the wizard creates a worktree
- **THEN** the state folder records the worktree path, branch, repository and creation time

#### Scenario: Recorded before the includes are copied
- **WHEN** the wizard creates a worktree in a repository with a `.worktreeinclude` file
- **THEN** the worktree is recorded before any include file is copied, and a failed copy leaves it recorded

#### Scenario: Two processes record at once
- **WHEN** `torni here` records a recent repository while the console records another recent repository and a created worktree
- **THEN** all three records are present on the next read

#### Scenario: Two processes record created worktrees at once
- **WHEN** `torni here` records the worktree it created at C:\Users\me\source\worktrees\Northwind.Orders-fix-123 while the console records the one it created at C:\Users\me\source\worktrees\Northwind.Billing-invoice-pdf
- **THEN** both worktrees are recorded as created by Torni on the next read

#### Scenario: A worktree is forgotten while another is recorded
- **WHEN** the console removes the worktree Northwind.Orders-fix-123 and forgets it while `torni here` records the worktree Northwind.Billing-invoice-pdf it has just created
- **THEN** Northwind.Billing-invoice-pdf is recorded as created by Torni, Northwind.Orders-fix-123 is not, and every other created worktree is still recorded

#### Scenario: The same worktree recorded again
- **WHEN** a worktree already recorded at C:\Users\me\source\worktrees\Northwind.Orders-fix-123 is recorded again under the spelling c:\users\me\source\worktrees\northwind.orders-fix-123
- **THEN** one record holds that worktree, with the newer branch and creation time

#### Scenario: Existing state is kept
- **WHEN** Torni starts with recent repositories saved by an earlier version
- **THEN** the recent repositories are still listed first in the wizard

#### Scenario: Old state files are ignored
- **WHEN** the state folder holds a `descriptions` folder, a `sessions.json` or a `created.json` written by an earlier version
- **THEN** Torni neither reads nor deletes them

### Requirement: The session starts under the supervisor
Torni SHALL start the session as a named background session in the chosen directory, passing the opening prompt when one was given and starting an idle session when none was. An opening prompt that starts with a dash SHALL be refused before anything starts, in the wizard and by `torni here`, with a sentence saying the CLI would read it as options and to start it with a word.

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
A session started from the wizard SHALL be opened in its own Windows Terminal tab as soon as the supervisor lists it, and the wizard SHALL have no way to start one without a tab; `torni here` attaches in its own tab instead.

#### Scenario: Start and open in a tab
- **WHEN** the user starts a session from the wizard
- **THEN** Torni starts the session, opens it in a new Windows Terminal tab titled with the session name as the open-in-tab key does, and the sessions view stays on screen with the status line naming the tab's colour

#### Scenario: No start without a tab
- **WHEN** the user presses F6 in the wizard
- **THEN** nothing starts and the wizard stays open

#### Scenario: Started session not listed yet
- **WHEN** the user starts a session from the wizard and the supervisor has not yet listed the new session when the start returns
- **THEN** the status line reports the session as started and says to press Enter on it once it appears, and no tab is opened
