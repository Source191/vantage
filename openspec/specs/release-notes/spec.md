# release-notes Specification

## Purpose
Tells a person who installed Vantage what each release changes, in words written for them, before they update and once after, from one changelog that also feeds the package and the GitHub Release.

## Requirements

### Requirement: The changelog is the one source of release notes
The repository SHALL keep a `CHANGELOG.md` at its root with an Unreleased section and one section per stable version, newest first, each holding entries under the headings Added, Changed, Fixed, Removed, Security and Internal. Each entry SHALL be one line written for a person using Vantage and SHALL end with its pull request number. Internal entries record changes a user cannot see.

#### Scenario: A section for a release
- **WHEN** 0.21.0 adds a key in pull request 171 and fixes a rename in pull request 169
- **THEN** the changelog has a 0.21.0 section with the key under Added ending (#171) and the fix under Fixed ending (#169)

#### Scenario: An internal change
- **WHEN** a pull request only splits files without changing what Vantage does
- **THEN** its entry is under Internal, and no notes Vantage shows include it

### Requirement: Every pull request to the packaged tool adds a changelog entry
A pull request that changes the packaged tool SHALL change the changelog's Unreleased section, or SHALL add the section for the stable version it raises `Version` to, and the pull request check SHALL fail otherwise. A pull request that raises `Version` to a stable version SHALL leave the changelog with that version's section and an empty Unreleased section.

#### Scenario: A change with no entry
- **WHEN** a pull request changes a file under `src` and leaves `CHANGELOG.md` unchanged
- **THEN** the pull request check fails and says the changelog needs an entry under Unreleased

#### Scenario: A documentation-only change
- **WHEN** a pull request changes only files outside the packaged tool
- **THEN** the changelog check passes without an entry

#### Scenario: A stable release
- **WHEN** a pull request raises `Version` to 0.21.0 and the changelog has no 0.21.0 section
- **THEN** the pull request check fails and says the Unreleased entries need moving to a 0.21.0 section

### Requirement: The package carries the release notes
The packed tool SHALL carry the changelog as the package's release notes and as a file inside the installed tool. Vantage SHALL never show the Unreleased section or Internal entries from either.

#### Scenario: Notes on nuget.org
- **WHEN** 0.21.0 is packed
- **THEN** its nuspec release notes hold the changelog, with the 0.21.0 section and the sections before it

#### Scenario: Internal entries stay hidden
- **WHEN** the 0.21.0 section has two Fixed entries and three Internal entries
- **THEN** Vantage's notes for 0.21.0 show the two Fixed entries and none of the Internal ones

#### Scenario: Notes inside the tool
- **WHEN** 0.21.0 is installed
- **THEN** the installed tool's folder holds the same notes for Vantage to read without a network request

### Requirement: A stable GitHub Release describes everything since the last stable release
The GitHub Release of a stable version SHALL carry that version's changelog section, followed by the generated list of pull requests since the previous stable release rather than since the previous preview. A prerelease's GitHub Release SHALL keep its generated notes.

#### Scenario: Stable after previews
- **WHEN** 0.21.0 is released after 0.21.0-preview.1 to 0.21.0-preview.9, which followed 0.20.0
- **THEN** the 0.21.0 GitHub Release shows its changelog section and lists every pull request merged since 0.20.0

### Requirement: The What's new dialogue lists the changes before an update
When an update is available, the update key SHALL open a What's new dialogue listing, newest first, every stable release after the running version up to the newest, each with its Added, Changed, Fixed, Removed and Security entries and their pull request numbers. Its buttons SHALL be Update now, Release page and Later, with Later focused.

#### Scenario: Two releases behind
- **WHEN** Vantage 0.19.0 runs, 0.20.0 and 0.21.0 are recorded with their notes, and the user presses U
- **THEN** the dialogue is titled for an update from 0.19.0 to 0.21.0 and lists the 0.21.0 entries, then the 0.20.0 entries, and nothing from 0.19.0 or earlier

#### Scenario: Update now
- **WHEN** the user chooses Update now
- **THEN** Vantage opens the update tab and quits, as the update key did before

#### Scenario: Release page
- **WHEN** the user chooses Release page
- **THEN** the newest release's GitHub page, `https://github.com/Source191/vantage/releases/tag/v0.21.0`, opens in the browser and the dialogue stays open

#### Scenario: Later
- **WHEN** the user presses Enter as the dialogue opens, or Esc, or chooses Later
- **THEN** the dialogue closes, nothing is opened and Vantage stays on the same view and selection

#### Scenario: The notes could not be read
- **WHEN** an update to 0.21.0 is available but no notes were recorded for it
- **THEN** the dialogue says the notes could not be read and still offers Update now, Release page and Later

### Requirement: Vantage says what changed once after an update
On the first start of a stable version higher than the stable version that last ran, Vantage SHALL show a What's new dialogue, before any key is read, listing the entries of every stable release after the version that last ran up to the running one, from the notes inside the installed tool. It SHALL then record the running version, and SHALL show nothing on a later start of the same version.

#### Scenario: First start after an update
- **WHEN** 0.19.0 last ran and Vantage 0.21.0 starts
- **THEN** a dialogue titled What's new in 0.21.0 lists the 0.21.0 and 0.20.0 entries, and closing it shows the console

#### Scenario: The same version again
- **WHEN** 0.21.0 last ran and Vantage 0.21.0 starts
- **THEN** no dialogue opens

#### Scenario: First ever start
- **WHEN** no version is recorded as having run
- **THEN** no dialogue opens and the running version is recorded

#### Scenario: A development build
- **WHEN** Vantage `0.21.0-local` starts
- **THEN** no dialogue opens and nothing is recorded

#### Scenario: A version without notes
- **WHEN** 0.19.0 last ran and the notes inside 0.21.0 cannot be read
- **THEN** no dialogue opens, the failure is recorded only in the diagnostic log, and the running version is recorded

### Requirement: Release notes are shown as text only
Vantage SHALL treat notes read from nuget.org or from the installed tool as text from outside Vantage: notes it cannot read SHALL count as no notes, control characters SHALL be removed before they are drawn, and no text from the notes SHALL be opened, run or used to build a URL. The release page's URL SHALL be built from the version alone.

#### Scenario: Escape sequences in the notes
- **WHEN** a note line holds an escape sequence that would change the terminal's title
- **THEN** the dialogue draws the line without the escape character and the terminal's title is unchanged

#### Scenario: A link in the notes
- **WHEN** a note line holds a URL
- **THEN** it is drawn as text, and Release page still opens the GitHub release page built from the version
