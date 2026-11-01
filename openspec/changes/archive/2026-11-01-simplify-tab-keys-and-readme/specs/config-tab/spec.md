# Spec Delta

## MODIFIED Requirements

### Requirement: The Config tab lists the settings
The console SHALL have a third tab, titled Config, whose table has the columns SETTING and VALUE and a row per setting in this order: one row per repository root, the first labelled Repository roots and the others with SETTING blank, then Worktree root, Open folder with, Default base and Check for updates. Each value SHALL be shown whole where it fits and elided in the middle where it does not.

#### Scenario: Default settings
- **WHEN** `config.json` does not exist and the user shows the Config tab
- **THEN** the rows read Repository roots C:\Users\me\source\repos, Worktree root C:\Users\me\source\worktrees, Open folder with VS Code, Default base origin's default branch and Check for updates on

#### Scenario: Two roots
- **WHEN** the roots are C:\Users\me\source\repos and D:\work
- **THEN** the first row reads Repository roots C:\Users\me\source\repos and the second has SETTING blank and VALUE D:\work

#### Scenario: A custom editor
- **WHEN** the editor is the executable C:\Tools\subl.exe with arguments -n {folder}
- **THEN** the Open folder with row reads C:\Tools\subl.exe -n {folder}

### Requirement: The setup sits under its own heading
Between the settings and the setup rows the Config tab's table SHALL have a heading row reading Setup in SETTING and nothing in VALUE. The selection SHALL never rest on it: moving up or down steps over it, and a load never selects it. While a filter is set it SHALL be hidden. It SHALL count neither as a setting in the counts nor as a row in the table's title.

#### Scenario: The heading between the two parts
- **WHEN** the user shows the Config tab with the default settings
- **THEN** the row after Check for updates reads Setup, and the row after that Settings file, drawn grey

#### Scenario: Down steps over the heading
- **WHEN** Check for updates is selected and the user presses Down
- **THEN** Settings file is selected

#### Scenario: Up steps over the heading
- **WHEN** Settings file is selected and the user presses Up
- **THEN** Check for updates is selected

#### Scenario: A filter hides the heading
- **WHEN** the user filters by bell
- **THEN** the table shows only Claude bell, with no Setup heading

#### Scenario: The heading is not counted
- **WHEN** there is one root and every piece of setup is in place
- **THEN** the counts read 6 settings and the title counts 11 rows
