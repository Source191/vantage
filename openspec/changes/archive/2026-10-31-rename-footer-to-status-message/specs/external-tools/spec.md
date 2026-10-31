## MODIFIED Requirements

### Requirement: A missing program is reported where it is needed
When no folder on the PATH holds git.exe, the console and `vantage here` SHALL print one sentence saying so and exit 1 without starting anything. When none holds gh.exe, GitHub lookups SHALL report the CLI as missing without starting a process. When none holds wt.exe or pwsh.exe, the key that needs it SHALL fail with a sentence naming the program, and nothing else SHALL change.

#### Scenario: git missing
- **WHEN** no folder on the PATH holds git.exe and the user runs vantage
- **THEN** standard error says git was not found on the PATH and the exit code is 1

#### Scenario: wt.exe missing
- **WHEN** no folder on the PATH holds wt.exe and the user presses the open-in-tab key
- **THEN** the status line reports that Windows Terminal (wt.exe) was not found on the PATH, and no tab opens

#### Scenario: pwsh.exe missing
- **WHEN** no folder on the PATH holds pwsh.exe and the user presses the open-solution key on a row with one solution
- **THEN** the status line reports that PowerShell 7 (pwsh.exe) was not found on the PATH, and nothing opens
