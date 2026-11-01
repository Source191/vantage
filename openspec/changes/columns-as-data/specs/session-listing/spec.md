## ADDED Requirements

### Requirement: Each column keeps its own fit
How a sessions column fits SHALL be a property of that column and never decided from its header's words: NAME SHALL be cut at 28 cells with an ellipsis, AGE SHALL be right-aligned so its durations line up on their last character, REPOSITORY SHALL take the width the other columns leave, and every other column SHALL take its natural width. Rewording a header SHALL change the header alone.

#### Scenario: Ages line up at the right
- **WHEN** fix-123 started five minutes ago, templates-sync two hours and ten minutes ago and a stopped session three days and four hours ago
- **THEN** the AGE cells 5m, 2h10m and 3d4h each end at the column's right edge, and the NAME cells start at its left edge

#### Scenario: A long name
- **WHEN** a session is named northwind-orders-cosmos-change-feed-monitoring-spike
- **THEN** its NAME cell is cut to 28 cells ending with an ellipsis, the rename and open keys use the full name, and the REPOSITORY column keeps its width

#### Scenario: A reworded header
- **WHEN** the AGE header is reworded in a later version
- **THEN** that column's cells are still right-aligned and NAME is still cut at 28 cells
