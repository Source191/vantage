# Spec Delta

## ADDED Requirements

### Requirement: The TAB block matches the tab as it looks from Vantage
The TAB column's block SHALL be drawn in the shade Windows Terminal shows for that session's tab while another tab is selected, because Vantage's own tab is the selected one whenever the table is read. The shade SHALL be the same on the selected row as on any other, and SHALL come from one rule applied to the tab's colour.

#### Scenario: A red tab
- **WHEN** a session's Vantage tab is red and the Vantage tab is selected
- **THEN** the session's TAB block is the faded red its tab shows in the tab strip, not the bright red the tab shows when selected

#### Scenario: The selected row
- **WHEN** the row of a session with a blue tab is selected
- **THEN** its TAB block is the same faded blue on the selection bar

#### Scenario: Every palette colour
- **WHEN** each colour of the tab palette is turned into its shade
- **THEN** each matches the colour measured from Windows Terminal's tab strip for that colour, within one step in each channel
