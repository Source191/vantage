# Spec Delta

## MODIFIED Requirements

### Requirement: States are colour coded
Each state SHALL be shown as a glyph and its word in its own colour, each state with a glyph of its own: ◐ needs input amber, ● working cyan, ○ idle white, ✓ completed green, ✗ failed red, ■ stopped grey and ? unknown grey, an unknown state's word being the status and state Claude Code listed; and each row SHALL be drawn in its state's colour, none dimmed further.

#### Scenario: State colours
- **WHEN** the table shows a working row, a needs-input row and a stopped row
- **THEN** the working state reads ● working in cyan, the needs-input state ◐ needs input in amber, and the stopped row is grey

#### Scenario: Stopped and unknown apart
- **WHEN** the table shows a stopped row and a row whose listed status and state Vantage does not know, busy/paused
- **THEN** both rows are grey, one STATE cell reads ■ stopped and the other ? busy/paused
