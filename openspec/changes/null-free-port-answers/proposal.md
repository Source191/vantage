# Proposal

## Why

Five ports answer null for "there is nothing": `IVantageConfig.ReadConfigAsync` (no config.json, or one that cannot be read), `IUpdateCheckRecords.ReadUpdateCheckAsync` (never checked) with `UpdateCheckRecord.Newest` (no release listed), `ICrashFile.ReadAsync` (no crash), `IClaudeCli.VersionAsync` (no version read) and `IPackageVersions.ListAsync` (nothing learned from nuget.org). Each null reaches Application, and two reach Domain (`ClaudeCodeSupport.Check(string?)` and `UpdateCheckRule.ShouldCheck(..., DateTimeOffset?, ...)`), where the reader has to know what it meant. The owner decided on 31 October 2026 that null stays only on the line that receives it from outside Vantage, with a named case or a null object per concept. This change was split from `null-at-the-boundary` to keep each pull request reviewable: that one covers programs, hosts, sounds, process requests and the command line; this one covers what the ports answer and the state files behind them.

## What Changes

- **`VantageConfig.Missing`:** a null object (every member absent) that `ReadConfigAsync` answers for no file or an unreadable one; `SettingsFile.From` takes a `VantageConfig`, and absent members still resolve to their defaults.
- **`UpdateCheckRecord`** becomes a closed record in `Ports`: `Never`, or `Checked(DateTimeOffset CheckedAt, NewestRelease Newest)`. `JsonFileStore` reads and writes it through a private data object with today's members, so `update-check.json` is the same JSON and files written by earlier versions still load. `NewestRelease` gains `Unreadable(string Text)` for recorded text that is not a version, so the Config tab still shows a hand-edited record's text as it does today. This removes the two lines `shared-snapshot` left in `UpdateCheck` and `SetupState.Checked`'s `string? Newest`.
- **`UpdateCheckRule.ShouldCheck`** has an overload for a record that was never written, in place of its `DateTimeOffset?`.
- **`LastCrash`** in `Ports` (`None`, `Recorded(string Text)`) answered by `ICrashFile.ReadAsync`.
- **`ClaudeVersionReport`** in `Domain` (`NotRead`, `Printed(string Text)`) answered by `IClaudeCli.VersionAsync` and taken by `ClaudeCodeSupport.Check`.
- **`VersionListing`** in `Ports` (`Listed(IReadOnlyList<string> Versions)`, `NothingLearned`) answered by `IPackageVersions.ListAsync`; nothing learned stays different from an empty list, as today.
- **`CLAUDE.md`:** "`IClaudeCli.VersionAsync` takes the leading dotted number (null on failure)" names `NotRead`.
- **Version:** one step above main when it merges (projected `0.19.0-preview.10`).
- `skip_specs: true`: no behaviour changes. The header, the update offer, the Config tab, the problem report and both state files read and write as before.

Out of scope: programs, pull-request hosts, terminal sounds, process requests, the command line, Windows Terminal tabs and `VANTAGE_LOG` (`null-at-the-boundary`, which merges first); the JSON data objects' own nullable members (`VantageConfig`, `EditorSetting`, `NuGetPackageVersions.VersionIndex`), which are the honest shape of a file or reply that may leave a member out; `IGit`'s queries (including `CurrentBranchAsync`, which `null-free-rows` keeps nullable) and `IProcessRegistry`'s reads, which stay; `DiagnosticEvent.Started.ClaudeCode`, a `Version?` whose member types `DiagnosticEventsShould` restricts; `ClaudeVersion.TryParse`, the house `TryParse` shape; the architecture rule (`null-guard-rule`); guard clauses, which stay; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Domain:** new `ClaudeVersionReport`; `NewestRelease` (a third case), `ClaudeCodeSupport`, `UpdateCheckRule`.
- **Ports:** `StatePorts` (`IVantageConfig`, `UpdateCheckRecord`, `IUpdateCheckRecords`), `VantageConfig` (`Missing`), `ICrashFile` and new `LastCrash`, `IClaudeCli`, `IPackageVersions` and new `VersionListing`.
- **Application:** `SettingsFile`, `CurrentSettings`, `UpdateCheck`, `SetupFactsReader`, `Views\ConfigRow` (`SetupState.Checked`), `WordingOfConfig`, `Reporting\ProblemReports`.
- **Adapters:** `State\JsonFileStore`, `Diagnostics\CrashFile`, `Claude\ClaudeCli`, `Packages\NuGetPackageVersions`.
- **Composition:** `Program.cs` (the version report, `RecordStart`).
- **Tests:** `SettingsFileShould`, `UpdateCheckShould`, `UpdateCheckRuleShould`, `NewestReleaseShould`, `SetupFactsReaderShould`, `WordingOfConfigShould`, `ScreenLoopShould` (its `UnwritableRecords`), `ProblemReportsShould`, `ClaudeCodeSupportShould`, `ClaudeCliShould`, `ClaudeContractShould`, `JsonFileStoreShould`, `CrashFileShould`, `NuGetPackageVersionsShould`; `InMemoryState`, `FakeCrashFile` and `FakePackageVersions` in TestDoubles.
- **Docs:** `CLAUDE.md` (one sentence); `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

Change 8 of the null programme, which merges one change after another in this order: 1 shared-snapshot (0.19.0-preview.3), 2 null-suppressions (preview.4), 3 null-free-outcomes (preview.5), 4 null-free-rows (preview.6), 5 null-free-observations (preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (preview.9), 8 null-free-port-answers (this one, preview.10), 9 null-free-sessions (preview.11), 10 null-guard-rule (no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between.
