# Spec Delta

## ADDED Requirements

### Requirement: Dialog fields and highlights are legible
In every dialog, the text of an unfocused input field and of a list's chosen row once focus has moved on SHALL have a contrast ratio of at least 4.5:1 against its background, and that background SHALL have a contrast ratio of at least 3:1 against the console's own, so the field or row still stands out.

#### Scenario: An unfocused name field
- **WHEN** the rename dialog is open and focus has moved from the name field to Cancel
- **THEN** the typed name is drawn white on a background at least 4.5:1 from it and at least 3:1 from the console's background

#### Scenario: A list's chosen row after focus moves on
- **WHEN** a repository is chosen in the new-session wizard's list and focus moves to the next field
- **THEN** the chosen row stays marked, white on a background that meets the same two ratios
