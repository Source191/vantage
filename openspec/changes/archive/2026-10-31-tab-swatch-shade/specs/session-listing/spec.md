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

## MODIFIED Requirements

### Requirement: The TAB column is a swatch
The sessions table's TAB column SHALL show a two-cell block in the colour of the Vantage tab showing the session while that tab is alive, in the shade that tab shows from Vantage's own tab, with a cell of space before the next column, on the highlighted row as on every other; SHALL show a hollow grey block for a session running as plain claude in its own terminal; and nothing otherwise. The help and the column's tooltip SHALL say what each means.

#### Scenario: A session open in a Vantage tab
- **WHEN** fix-123 is shown in a Vantage tab coloured yellow
- **THEN** its TAB cell shows a two-cell yellow block, then a space before the state

#### Scenario: A terminal session
- **WHEN** study was started as plain claude in a tab of its own
- **THEN** its TAB cell is a hollow block drawn in grey

#### Scenario: No tab
- **WHEN** a background session has no live Vantage tab
- **THEN** its TAB cell is blank

#### Scenario: The help names the blocks
- **WHEN** the help is shown on the sessions view
- **THEN** TAB's line says a block in the tab's colour marks a session a Vantage tab shows and a hollow block a plain claude in its own terminal

#### Scenario: The highlighted row keeps the colour
- **WHEN** fix-123, shown in a Vantage tab coloured red, is the selected row
- **THEN** its TAB block is the same shade of red as on any other row, not the highlight's colour
