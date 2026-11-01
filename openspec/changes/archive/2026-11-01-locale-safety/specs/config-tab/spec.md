## ADDED Requirements

### Requirement: The last update check's time is local
The Last update check row SHALL show when nuget.org was last asked in the user's local time zone, using the offset that zone had at that moment, and SHALL say today only when that moment falls on the current local date.

#### Scenario: A check this morning in summer
- **WHEN** the local time zone is the UK's, it is 15 July and nuget.org was asked at 08:12 UTC that day and named 0.18.0
- **THEN** the row reads 09:12 today, newest 0.18.0

#### Scenario: A check before the clocks went back
- **WHEN** the local time zone is the UK's, it is 1 November and nuget.org was last asked at 08:12 UTC on 24 October
- **THEN** the row reads 24 October 2026 09:12
