# Spec Delta

## MODIFIED Requirements

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

### Requirement: A session can be started from the shell in the current folder
`vantage here [name] [-p prompt]` SHALL start a supervised session in the current folder on its current branch, named after the folder unless a name is given, record the recent repository, attach in the same tab, and exit 0 after a detach, 1 when the start failed or the command line could not be parsed, and 2 when the name or the opening prompt is refused. Before attaching it SHALL print one line saying that Ctrl+Z detaches and the session keeps running, and SHALL erase that line when the attach exits.

#### Scenario: Start here with a prompt
- **WHEN** the user runs vantage here fix-tests -p "Fix the tests" in a trusted repository
- **THEN** a background session named fix-tests starts there with that opening prompt and the shell is attached to it until Ctrl+Z

#### Scenario: The line before attaching
- **WHEN** vantage here starts a session and attaches
- **THEN** the last line printed before handing over is exactly "Ctrl+Z detaches; the session keeps running.", and it is gone from the terminal once the attach exits

#### Scenario: The removed description option
- **WHEN** the user runs vantage here fix-tests -d "Flaky orders suite"
- **THEN** it exits 1 naming what was not recognised, and starts nothing

#### Scenario: A refused name
- **WHEN** the user runs vantage here "a;b"
- **THEN** it exits 2 with the sentence for the refused name, and starts nothing

#### Scenario: A refused prompt
- **WHEN** the user runs vantage here fix-tests -p "-fix the tests" in a trusted repository
- **THEN** it exits 2 with the sentence that an opening prompt cannot start with a dash, and starts nothing

### Requirement: The session starts under the supervisor and opens where the user chose
Vantage SHALL start the session as a named background session in the chosen directory, passing the opening prompt when one was given and starting an idle session when none was. A session started from the wizard SHALL then be opened in its own Windows Terminal tab; `vantage here` SHALL attach in its own tab instead. An opening prompt that starts with a dash SHALL be refused before anything starts, in the wizard and by `vantage here`, with a sentence saying the CLI would read it as options and to start it with a word.

#### Scenario: No opening prompt
- **WHEN** the user leaves the opening prompt blank
- **THEN** the session starts idle and waits for input under the given name

#### Scenario: Start and open in a tab
- **WHEN** the user starts a session from the wizard
- **THEN** Vantage starts the session, opens it in a new Windows Terminal tab titled with the session name as the open-in-tab key does, and the sessions view stays on screen with the footer naming the tab's colour

#### Scenario: No start without a tab
- **WHEN** the user presses F6 in the wizard
- **THEN** nothing starts and the wizard stays open

#### Scenario: Started session not listed yet
- **WHEN** the user starts a session from the wizard and the supervisor has not yet listed the new session when the start returns
- **THEN** the footer reports the session as started and says to press Enter on it once it appears, and no tab is opened

#### Scenario: Prompt that starts with a dash
- **WHEN** the user types the opening prompt "- fix the tests" or "--dangerously-skip-permissions" and starts
- **THEN** nothing starts and the wizard says an opening prompt cannot start with a dash because Claude Code would read it as options

#### Scenario: Prompt with a dash inside it
- **WHEN** the user types the opening prompt "Fix the tests - all of them"
- **THEN** the session starts with that prompt

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
