# Spec Delta

## ADDED Requirements

### Requirement: The wizard reads as one form
The wizard opened from the overview SHALL lay its controls out in one column in reading order: the repository filter with its list beneath, the session name, Where, the worktree base, the preview, the opening prompt, the problem line, the hint, and the buttons Start, Start and open in tab, and Cancel. The dialog SHALL fit a 120 by 36 terminal with at least four repository rows visible, and Enter SHALL never start a session in it.

#### Scenario: Reading order
- **WHEN** the user opens the wizard with n
- **THEN** the filter is above the repository list, which is above the name, Where, the base, the preview and the prompt, in one column

#### Scenario: Fits the laptop screen
- **WHEN** the wizard opens on a 120 by 36 terminal with twelve repositories under the roots
- **THEN** every control and the buttons are on screen and at least four repositories are listed

#### Scenario: Enter in the prompt
- **WHEN** the user presses Enter in the opening prompt
- **THEN** the prompt gains a new line and nothing starts

#### Scenario: The compact form is unchanged
- **WHEN** the user presses s on a worktree row
- **THEN** the compact fixed-folder dialog opens as before, and Enter in its name starts the session and opens its tab
