# Spec Delta

## ADDED Requirements

### Requirement: The first tab colours stay apart for colour-blind users
The tab palette SHALL be ordered Red, Blue, Green, Teal, Pink, Brown, Purple, Orange, Lime, Grey, Yellow, so that Vantage's own green tab and the first five session tabs given the first free colours differ from one another for people with protanopia or deuteranopia as well as for normal colour vision.

#### Scenario: Five session tabs beside Vantage's green
- **WHEN** Vantage's own tab is green and the user opens five session tabs, none closing
- **THEN** the session tabs are red, blue, teal, pink and brown

#### Scenario: A tab already open keeps its colour
- **WHEN** a session tab was coloured yellow by an earlier version and is still live as Vantage starts
- **THEN** it stays yellow, and yellow counts as in use when the next tab's colour is chosen
