# Spec Delta

## MODIFIED Requirements

### Requirement: The package carries the release notes
Every release's zip SHALL hold the changelog as a file beside the executable, and Vantage SHALL read its own release notes from it. Notes for a newer release SHALL be read from the changelog at that release's tag on GitHub. Vantage SHALL never show the Unreleased section or Internal entries from either.

#### Scenario: Notes on nuget.org
- **WHEN** 0.21.0 is the newest release, whichever channel the running Vantage came from
- **THEN** the notes Vantage records for it are read from the changelog at tag v0.21.0, with the 0.21.0 section and the sections before it

#### Scenario: Internal entries stay hidden
- **WHEN** the 0.21.0 section has two Fixed entries and three Internal entries
- **THEN** Vantage's notes for 0.21.0 show the two Fixed entries and none of the Internal ones

#### Scenario: Notes inside the tool
- **WHEN** 0.21.0 is installed
- **THEN** the installed folder holds the same notes for Vantage to read without a network request

### Requirement: A stable GitHub Release describes everything since the last stable release
The GitHub Release of a stable version SHALL carry that version's changelog section, followed by the generated list of pull requests since the previous stable release rather than since the previous preview, and the version's zip. A prerelease's GitHub Release SHALL keep its generated notes and carry its zip.

#### Scenario: Stable after previews
- **WHEN** 0.21.0 is released after 0.21.0-preview.1 to 0.21.0-preview.9, which followed 0.20.0
- **THEN** the 0.21.0 GitHub Release shows its changelog section, lists every pull request merged since 0.20.0 and carries the 0.21.0 zip

### Requirement: Release notes are shown as text only
Vantage SHALL treat notes read from GitHub or from the installed folder as text from outside Vantage: notes it cannot read SHALL count as no notes, control characters SHALL be removed before they are drawn, and no text from the notes SHALL be opened, run or used to build a URL. The release page's URL SHALL be built from the version alone.

#### Scenario: Escape sequences in the notes
- **WHEN** a note line holds an escape sequence that would change the terminal's title
- **THEN** the dialogue draws the line without the escape character and the terminal's title is unchanged

#### Scenario: A link in the notes
- **WHEN** a note line holds a URL
- **THEN** it is drawn as text, and Release page still opens the GitHub release page built from the version
