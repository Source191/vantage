# Design

## Context

See proposal.md for why. As of main at `356588b`, with `shared-snapshot` applied first:
- `JsonFileStore.ReadJsonAsync<T>` answers null for a missing, locked, half-written or corrupt file, and `ReadConfigAsync` and `ReadUpdateCheckAsync` pass that null on. Its options are `JsonSerializerDefaults.Web` (camelCase) with a string enum converter, so `update-check.json` reads `{"checkedAt":"...","newest":"0.10.0"}` or `"newest":null`.
- `SettingsFile.From(VantageConfig? config, ...)` reads every member through `config?.`; `CurrentSettings.LoadAsync` is its one production caller.
- `UpdateCheck.RunAsync` meets `UpdateCheckRecord?` and `string? Newest` on two lines (left there by `shared-snapshot`), and calls `UpdateCheckRule.ShouldCheck(running, enabled, record?.CheckedAt, now)`. `SetupFactsReader` answers `SetupState.Never` for null and `SetupState.Checked(record.CheckedAt, record.Newest)` otherwise; `WordingOfConfig` shows ", newest {text}" when the text is not null, whatever the text is.
- `ProblemReports.BuildAsync` reads `string? crash` and adds the "Last crash" block when it is not null.
- `ClaudeCli.VersionAsync` answers `string?`; `Program.cs` hands the text to `ClaudeCodeSupport.Check(string?)` (null is `ClaudeCodeVersion.Unknown`) and to `RecordStart`, which parses it into `Started.ClaudeCode` (`Version?`).
- `NuGetPackageVersions.ListAsync` answers null for a failed, slow or unreadable reply and `index?.Versions` otherwise; `UpdateCheck` returns early on null and records nothing.

## Goals / Non-Goals

**Goals:**
- No port in this change answers null; each answer's absence is a named case or a null object, translated in the adapter on the line that meets the outside value.
- `config.json` and `update-check.json` are read and written as the same JSON; a file written by the previous version loads, which literal-JSON facts in `JsonFileStoreShould` prove.

**Non-Goals:**
- The data objects' own nullable members stay (recorded for `null-guard-rule`'s allow-list): `VantageConfig`, `EditorSetting`, the private update-check data object, `NuGetPackageVersions.VersionIndex`.
- `Started.ClaudeCode` stays `Version?`: diagnostic events keep their restricted member types.

## Decisions

**`VantageConfig.Missing` is a null object, not a named case.** Every reader already treats an absent member as "use the default", so a record with every member absent behaves exactly as null did, and no reader needs to ask which it has. `JsonFileStore.ReadConfigAsync` answers `await ReadJsonAsync<VantageConfig>(ConfigFile) ?? VantageConfig.Missing` (the private reader keeps its nullable answer, the adapter's own line that meets the file), `InMemoryState` starts with it, and `SettingsFile.From(VantageConfig, FolderPath)` drops its `?.`. `SettingsFileShould` proves `From(VantageConfig.Missing, home)` equals what `From(null, home)` gave.

**`UpdateCheckRecord` becomes `Never` or `Checked(DateTimeOffset CheckedAt, NewestRelease Newest)`.** A named case, because "never checked" changes what `UpdateCheck` does (it checks at once). `JsonFileStore` keeps a private data object `UpdateCheckFile(DateTimeOffset CheckedAt, string? Newest)`, whose camelCase JSON is today's file: reading maps a missing or corrupt file to `Never` and a file to `Checked(at, NewestRelease.Recorded(text))` (`None` for null text); writing maps the three `NewestRelease` cases to the version's text, null, or the unreadable text, in one switch naming every case with an `UnreachableException` discard arm. `IUpdateCheckRecords.WriteUpdateCheckAsync` takes a `Checked`, since nothing writes "never".

**`NewestRelease` gains `Unreadable(string Text)`** (owner accepted, 31 October 2026), and `Recorded(string text)` answers it for text that is not a version (it answered `None` in `shared-snapshot`). Vantage only ever writes a version or null, so this is reached only by a hand-edited file; today the Config tab shows such text as it is, and the case keeps that. `UpdateCheckRule.Offer` still offers only for `Found`, so the offer is unchanged. `NewestReleaseShould`'s fact for text that is not a version expects `Unreadable` with the text.

**`SetupState.Checked(DateTimeOffset When, NewestRelease Newest)`.** `WordingOfConfig` switches on the case: `Found` gives ", newest {version}", `Unreadable` gives ", newest {text}", `None` gives nothing. A `Found` that Vantage wrote reads back to the same text, since it was written from `ReleaseVersion.ToString()`; `WordingOfConfigShould`'s facts pin the words.

**`UpdateCheckRule.ShouldCheck` has two overloads:** `(running, enabled, lastCheckedAt, now)` with a `DateTimeOffset` and `(running, enabled)` for a record that was never written, answering what null answered (true for a release build with the check on). `UpdateCheck` switches on the record once. The seven `ShouldCheck` facts keep their answers.

**`LastCrash` (`None`, `Recorded(string Text)`).** `CrashFile.ReadAsync` translates "no file" on its own line; `ProblemReports` adds the block for `Recorded` only and `ProblemReport`'s "has a crash" flag reads the same case. `FakeCrashFile` answers from its `Written` text.

**`ClaudeVersionReport` (`NotRead`, `Printed(string Text)`) lives in `Domain`,** because `ClaudeCodeSupport.Check` in `Domain` takes it and `IClaudeCli` in `Ports` answers it. `ClaudeCli.VersionAsync` answers `NotRead` on every path that answered null. `Program.cs` hands the report to `ClaudeCodeSupport.Check` and to `RecordStart`, which parses `Printed` text through `ClaudeVersion.TryParse` as now. Alternative: answer `ClaudeCodeVersion` from the adapter (that would move the minimum-version rule into an adapter).

**`VersionListing` (`Listed(IReadOnlyList<string> Versions)`, `NothingLearned`).** `NothingLearned` and an empty `Listed` stay different, as null and empty were: an empty list records "checked, nothing found", nothing learned records nothing. `NuGetPackageVersions` answers `NothingLearned` on each path that answered null, including a reply whose `versions` member is missing. `FakePackageVersions` takes a `VersionListing`.

## Risks / Trade-offs

- [An old `update-check.json` or `config.json` stops loading] → `JsonFileStoreShould` gains facts that write today's exact JSON text, read it, and compare; the round-trip facts stay.
- [`NewestRelease` grows a case after `shared-snapshot` named two] → every switch over it names all its cases, so CS8509 or the `UnreachableException` arm shows each place to update; the offer's single `is Found` pattern needs none.
- [The header's version text changes] → `ClaudeCodeSupportShould` keeps its expected `ClaudeCodeVersion` values; `Printed` carries the CLI's text unchanged.
