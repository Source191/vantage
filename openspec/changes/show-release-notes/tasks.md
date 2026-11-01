# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, plus `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` for adapter tasks. Fixtures use Northwind wording and made-up pull request numbers.

## 1. The changelog and its checks

- [ ] 1.1 Add `CHANGELOG.md` at the root: a short preamble (Keep a Changelog, written for Vantage's users, Internal for changes they cannot see), an Unreleased section with this change's Added entries, and a 0.16.1 section saying notes start with the next release. Verify by reading it against the format in design.md and checking no em dash.
- [ ] 1.2 Move the tool-path list out of `Assert-VersionBumped.ps1` into `.github\scripts\ToolPaths.ps1`, dot-sourced. Verify `Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` gives the same answer as before on this branch and on `HEAD~1`.
- [ ] 1.3 `.github\scripts\Assert-ChangelogUpdated.ps1`: fails when a tool path changed and `CHANGELOG.md` did not, when Head's `Version` is stable and the changelog lacks its section or still has Unreleased entries, and when the file is 35,000 characters or more; passes for a change outside the tool. Verify with throwaway commits on a scratch branch for each of the four cases (exit 1, 1, 1, 0) and with `Invoke-ScriptAnalyzer` clean.
- [ ] 1.4 Run it in `check.yml` beside `Assert-VersionBumped.ps1`, and say in `CONTRIBUTING.md` (pull requests, and releasing a stable version) how to write an entry and when to turn Unreleased into a version. Verify the workflow's YAML parses (`actionlint` if present) and the check passes on this pull request.

## 2. Release notes as a Domain value

- [x] 2.1 `ReleaseNotesShould.ReadTheSectionsOfAChangelog`: a Northwind changelog with Unreleased, 0.21.0 and 0.20.0 parses into two version sections with their entries under their headings. Red, then `Domain\ReleaseNotes` with `TryParse(string?, out ReleaseNotes)`, the house shape for text from outside Vantage. Verify the fact, `NullFreeModelsShould` and `SealedTypesShould` pass.
- [x] 2.2 `ReleaseNotesShould.AnswerFalseForTextWithNoVersionSection` (the current link-only release notes, an empty string, null). Verify the fact passes.
- [x] 2.3 `ReleaseNotesShould.ChooseTheStableSectionsBetweenTwoVersionsNewestFirst`: `Between(0.19.0, 0.21.0)` gives 0.21.0 then 0.20.0, never Unreleased, 0.19.0 or a prerelease heading. Verify the fact passes.
- [x] 2.4 `ReleaseNotesShould.LeaveOutInternalEntries`. Verify the fact passes.
- [x] 2.5 `ReleaseNotesShould.DropControlCharactersFromEachLine`: an entry holding `\u001b]2;owned\u0007` keeps its visible text without ESC or BEL. Verify the fact passes.
- [ ] 2.6 `ReleaseNotesShould.SkipLinesItDoesNotUnderstand` (prose, a table row, a heading of an unknown kind). Verify the fact passes and a Stryker run scoped with `--mutate "**/Domain/ReleaseNotes.cs"` leaves no surviving mutant that changes which entries are shown.

## 3. Reading and recording the notes

- [ ] 3.1 `NuGetPackageVersionsShould.ReadTheReleaseNotesFromTheNuspec`: a stub `HttpMessageHandler` answering a nuspec for `0.21.0` at `.../source191.vantage/0.21.0/source191.vantage.nuspec` gives `PackageNotes.Read` with the `releaseNotes` text, sent with the `Vantage/<version>` user agent. Red, then `IPackageVersions.NotesAsync` and the adapter (`XmlReader`, DTD prohibited). Verify the fact passes.
- [ ] 3.2 `NuGetPackageVersionsShould.LearnNothingFromAFailedOrUnreadableNuspec` (404, timeout, malformed XML, a DTD, no `releaseNotes`). Verify the facts pass.
- [ ] 3.3 `FakePackageVersions` in TestDoubles answers notes per version. Verify the build.
- [ ] 3.4 `JsonFileStoreShould.KeepTheNewestReleasesNotes`: `update-check.json` round-trips a `Checked` record with notes, and a file written by 0.20.0 (no `notes`) reads with none. Red, then `UpdateCheckRecord.Checked.NewestNotes` and the file's optional `notes`. Verify the facts and `NullFreeModelsShould` pass.
- [ ] 3.5 `JsonFileStoreShould.RecordTheLastVersionRun`: `ILastRunVersion` reads `Never` with no `last-run.json`, and the version written. Verify the fact passes.
- [ ] 3.6 `PackagedReleaseNotesShould.ReadTheChangelogBesideTheTool` (a temporary folder as the base directory) and answer nothing when the file is missing. Red, then `IPackagedReleaseNotes` and its adapter. Verify the facts pass.
- [ ] 3.7 Pack the notes: `PackageReleaseNotes` from `CHANGELOG.md` and the file packed beside `vantage.dll`; record why in `src\Source191.Vantage\README.md` (no comment in the csproj). Verify `dotnet pack` then the nupkg's nuspec holds the changelog and `tools\net10.0\any\CHANGELOG.md` exists.

## 4. The check fetches notes and announces once

- [ ] 4.1 `UpdateCheckShould.RecordTheNotesOfANewerRelease`: running 0.19.0, finding 0.21.0, the fake answers notes, the record holds them. Verify the fact passes.
- [ ] 4.2 `UpdateCheckShould.AskForNoNotesWhenAlreadyNewest` (the fake records no notes request). Verify the fact passes.
- [ ] 4.3 `UpdateCheckShould.AskForTheNotesAgainWhenTheRecordHasNone` on the next due check, and keep offering the update meanwhile. Verify the fact passes.
- [ ] 4.4 `UpdateCheckShould.AnnounceANewlyFoundReleaseOnce`: record 0.20.0, found 0.21.0 gives one announcement, taken once; record 0.21.0, found 0.21.0 gives none; no record and found 0.21.0 gives one. Verify the facts pass.
- [ ] 4.5 `WordingShould` and `StatusTonesShould`: `StatusMessage.NewerReleaseFound` reads "Vantage 0.21.0 is available: press U to see what changed." in the Info tone. Verify the facts pass.
- [ ] 4.6 The screen shows the announcement in the status line when it next reads the update status (the test chosen in design.md's decision; a `ConsoleView` or `ScreenLoop` fact). Verify the fact passes.
- [ ] 4.7 `WordingOfScreensShould`: the header's notice is its own `TextPart` in `TextTone.Amber`, the words unchanged. Red, then the header parts and the Tui drawing. Verify the facts pass and the existing header facts still read the same text.

## 5. The What's new dialogue

- [ ] 5.1 `UpdateVantageShould.AskWithTheNotesBetweenTheRunningAndNewestVersions`: `Decide` gives `Ask(0.21.0, sections 0.21.0 and 0.20.0)`; with no notes recorded, `Ask` with no sections. Verify the facts pass.
- [ ] 5.2 `ReleasePageShould.BuildTheUrlFromTheVersionAlone`: `https://github.com/Source191/vantage/releases/tag/v0.21.0`. Verify the fact passes.
- [ ] 5.3 `OpenReleasePageHandlerShould.OpenTheReleasePageOnTheDesktop`: the fake desktop records the URL and the handler returns `ReleasePageOpenedOutcome`; a desktop that throws gives the failed outcome. Red, then `Application\Views\OpenReleasePage`, `Application\Actions\OpenReleasePageHandler`, its outcome record and its line in `Composition.Handlers`, with words in `WordingOfOutcomes` and its tone in `Wording.ToneOf`. Verify the facts, `EffectDispatcherShould` and `StatusTonesShould` pass.
- [ ] 5.4 `WordingShould`: the dialogue's titles ("Update Vantage 0.19.0 to 0.21.0", "What's new in 0.21.0"), its buttons, a section heading per version, the "notes could not be read" line, and an entry line ending with its pull request number. Verify the facts pass.
- [ ] 5.5 `Tui\WhatsNewDialog`: a read-only scrolling list and the buttons, Later focused, Esc closes, Release page dispatches `OpenReleasePage` and stays open, Update now dispatches `StartUpdate`; replace `Confirm.Ask` in `DialogEffects.ConfirmUpdate`; remove `Wording.UpdateQuestion` and its fact; call `DialogTabTitle.Apply` with a new `DialogName.WhatsNew`. Verify `DialogEffectsShould`, `WordingOfScreensShould` and a `SmokeScript` step (U, the dialogue's marker, Esc) pass with `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>` run on a stable build (a development build never offers an update or reads `last-run.json`, so pack with `dotnet pack src\Source191.Vantage -c Release -p:Version=0.99.0 -o <folder>` and install it with `dotnet tool install -g Source191.Vantage --add-source <folder> --version 0.99.0`) with a hand-written `update-check.json` naming 1.0.0 with notes in a temporary `LOCALAPPDATA`.

## 6. What's new after an update

- [ ] 6.1 `WhatsNewOnStartShould`: last run 0.19.0 and running 0.21.0 shows the 0.21.0 and 0.20.0 sections and records 0.21.0; same version shows nothing; no record shows nothing and records; a prerelease shows and records nothing; unreadable packaged notes show nothing, log a diagnostic event and record. Red, then `Application\WhatsNewOnStart`. Verify the facts and `DiagnosticEventsShould` pass.
- [ ] 6.2 `ScreenLoop` asks `WhatsNewOnStart` before the console reads a key and shows `WhatsNewDialog` with Close only; wire it in `Program.cs`. Verify a `ScreenLoopShould` fact with a recording console, and a smoke run on the stable 0.99.0 build of 5.5 with a seeded `last-run.json` in a temporary `LOCALAPPDATA` shows the dialogue once and not on a second start.
- [ ] 6.3 `DiagnosticsPrivacyShould` runs the update check and the start-up flow over a Northwind changelog and finds no note text in the log. Verify the fact passes.

## 7. GitHub Releases and documentation

- [ ] 7.1 `release.yml`: the build job writes the version's section to `release-notes.md` and uploads it; for a stable version the release job adds `--notes-file` and `--notes-start-tag v<previous stable>` beside `--generate-notes` (or appends `gh api .../releases/generate-notes` output if `gh release create --help` shows they do not combine). Verify with `gh release create --help` and a dry run of the extraction script over `CHANGELOG.md`.
- [ ] 7.2 README: Updates (the amber notice, the announcement, the What's new dialogue and its buttons, the dialogue after an update), What the check sends (the nuspec request only when a newer release is found), the state files table (`update-check.json` notes, `last-run.json`) and the key table's `U` line. Verify by reading against the spec deltas and checking no em dash.
- [ ] 7.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor preview above `main` at the time, and bring this change's Unreleased entries up to date with what was built. Verify `Assert-VersionBumped.ps1` and `Assert-ChangelogUpdated.ps1` pass against `origin/main`.

## 8. Whole-solution check

- [ ] 8.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate show-release-notes --strict` pass; then, on a stable build (a development build never offers an update or reads `last-run.json`, so pack with `dotnet pack src\Source191.Vantage -c Release -p:Version=0.99.0 -o <folder>` and install it with `dotnet tool install -g Source191.Vantage --add-source <folder> --version 0.99.0`), a manual check with a hand-written `update-check.json` naming 1.0.0 with notes: the amber notice, U's dialogue, Release page opening the browser, Later and Esc; then a seeded `last-run.json` showing What's new once.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\release-notes`, `tool-updates` and `console-layout` describe the code on `main`.
- Decide the two open questions in design.md (a start-up dialogue when a release is found, the tab title) after the quieter notice has been used for a while.
