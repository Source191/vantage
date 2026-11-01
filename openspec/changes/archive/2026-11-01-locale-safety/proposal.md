# Proposal

## Why

A locale review (1 November 2026) found three faults that do not depend on the language of Vantage's text, so they hit English-speaking users too. The detail pane and the Config tab show times in UTC, so a UK user in summer and anyone in Nairobi sees the wrong hour and "today" changes at UTC midnight. The Rider preset picks Rider 2025.9 over 2025.10 because the shipped tool runs in invariant globalisation mode, where `CompareOptions.NumericOrdering` is silently ignored. And a changed file with an accented name shows as git's octal escape (`"na\303\257ve.txt"`). The first two escaped the tests because the test projects run in the machine's culture, not invariant mode as the tool does, and the culture analysers (CA1304, CA1305, CA1309, CA1310, CA1311) are switched off wherever `InvariantGlobalization` is set, which includes the three library projects.

## What Changes

- The detail pane's times (Started, Prompted and Created; a commit shows only its age) and the Config tab's Last update check are shown in the user's local time zone, converted with that zone's rules for the moment shown, so a time from before a clock change keeps its own offset; "today" is the local date.
- The Rider preset ranks its installs by comparing the numbers in their paths as numbers, without depending on culture data, so 2025.10 is newer than 2025.9 in the shipped tool.
- The detail pane lists a changed file by its name as typed, whatever letters it has, by asking git not to quote such paths.
- `InvariantGlobalization` leaves the Core, Infrastructure and Tui library projects, so the culture analysers check their code (they find nothing today), and joins the three test projects, so the tests run as the tool runs. The tool project and the headless terminal keep it.
- `CLAUDE.md` gains a section of what translating Vantage, or supporting another culture, will need (on merging main, which had split `CLAUDE.md`, a bullet in its C# idioms and `.claude\rules\locale.md`; design decision 5).
- The tool's version rises to the next preview in the pull request that implements this.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `detail-pane`: its times are local, and a changed file shows by its own name.
- `config-tab`: the Last update check's time is local.
- `settings`: the Rider preset opens the newest installed Rider by version number.

## Impact

- **Code:** `Application\WordingOfDetail` and `WordingOfConfig` (convert with a time zone), `Application\Detail\IDetailSource` (`SessionDetails`, `WorktreeDetails`), `Tui\ConfigScreenDefinition` and `ConsoleScreen` (hand it the local zone), `Adapters\Processes\EditorLocator` (a culture-free number-aware order), `Adapters\Git\GitCli.ChangedFilesAsync` (`-c core.quotePath=false`).
- **Projects:** the four library and test `.csproj` files and their READMEs; `src\Source191.Vantage\Source191.Vantage.csproj` for the version.
- **Docs:** `CLAUDE.md`.
- **Users:** none of these changes how keys, state files or the command line behave.

## Out of scope

- Translating Vantage's text, or showing dates and numbers in the user's own culture: Vantage is English only, and the considerations are recorded in `CLAUDE.md` instead.
- Measuring text by terminal cells rather than characters (Chinese and Japanese characters take two cells, and cutting a string can split an emoji), and worktree slugs for names written in other scripts: both recorded in `CLAUDE.md` for when other languages are supported.
- Reading text from the az CLI, which writes Windows' legacy code page when piped and ignores `PYTHONIOENCODING`: Vantage reads only ASCII fields from it today.
- Choosing between a Toolbox Rider and a standalone one by version: their folders number builds differently (`253.x` and `2025.3`), and the order between the two stays as it is.
- Multiple Claude accounts, which is a later change.
