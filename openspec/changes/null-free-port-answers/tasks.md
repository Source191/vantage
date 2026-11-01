# Tasks

Each task is one red-green-refactor cycle with a commit per step, or a refactor step where it says so; the test is named before the code. A port change and its callers land in one task so the build stays green. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, plus `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` for adapter tasks. Re-read each file against main before changing it.

## 1. Domain

- [x] 1.1 `NewestReleaseShould.ReadTextThatIsNotAVersionAsUnreadable` (replacing `shared-snapshot`'s fact that read it as `None`): red, then `NewestRelease.Unreadable(string Text)` and `Recorded` answering it green; every switch over `NewestRelease` names the new case. Verify `NewestReleaseShould` and `UpdateCheckRuleShould` pass, `Offer` still answering `UpToDate` for unreadable text.
- [x] 1.2 `UpdateCheckRuleShould`'s `ShouldCheck` facts that passed `lastCheckedAt: null` call the overload without it and keep their answers: red, then `UpdateCheckRule.ShouldCheck(ToolVersion, bool)` and the `DateTimeOffset` overload green. Verify all seven `ShouldCheck` facts pass with the same answers.
- [x] 1.3 `ClaudeCodeSupportShould`'s facts pass `new ClaudeVersionReport.NotRead()` where they passed null and `Printed(text)` elsewhere, expecting the same `ClaudeCodeVersion`s: red, then `Domain\ClaudeVersionReport.cs` and `ClaudeCodeSupport.Check(ClaudeVersionReport)` green. Verify `ClaudeCodeSupportShould` passes unchanged in its expected values.

## 2. Ports and adapters

- [x] 2.1 `ClaudeCliShould`'s five facts that expected null expect `new ClaudeVersionReport.NotRead()`, and the version fact `Printed("2.1.310")`: red, then `IClaudeCli.VersionAsync` answering `ClaudeVersionReport` and `ClaudeCli` translating on each failing path green; `Program.cs` hands the report to `ClaudeCodeSupport.Check` and `RecordStart` (refactor step for the composition root). Verify `ClaudeCliShould` passes and `ClaudeContractShould` compiles (it runs only with `VANTAGE_CLAUDE_CONTRACT=1`).
- [x] 2.2 `CrashFileShould.ReadNoCrashAsNone` and `ReadACrashAsRecorded`; `ProblemReportsShould`'s facts with and without a crash keep their lines and flag: red, then `Ports\LastCrash.cs`, `ICrashFile.ReadAsync` answering it, `CrashFile`, `FakeCrashFile` and `ProblemReports` switching once green. Verify both classes pass with the same report text.
- [x] 2.3 `NuGetPackageVersionsShould`'s facts for a failed status, a timeout, unreadable JSON and a reply without `versions` expect `NothingLearned`, and a good reply `Listed`: red, then `Ports\VersionListing.cs`, `IPackageVersions.ListAsync` answering it, `NuGetPackageVersions` and `FakePackageVersions(VersionListing)` green; `UpdateCheck` returns early for `NothingLearned` as it did for null. Verify `NuGetPackageVersionsShould`, `UpdateCheckShould` and `ScreenLoopShould` pass.
- [ ] 2.4 `JsonFileStoreShould.ReadAConfigFileWrittenByThePreviousVersion` (today's exact JSON text) and `ReadAMissingOrCorruptConfigAsMissing` (expecting `VantageConfig.Missing`); `SettingsFileShould.ResolveAMissingConfigToTheDefaults` (equal to what null gave): red, then `VantageConfig.Missing`, `IVantageConfig.ReadConfigAsync` answering `VantageConfig`, `JsonFileStore`, `InMemoryState` and `SettingsFile.From(VantageConfig, FolderPath)` green. Verify `JsonFileStoreShould`, `SettingsFileShould` and `CurrentSettingsShould` pass, and the config round-trip fact is unchanged.
- [ ] 2.5 `JsonFileStoreShould.ReadAnUpdateCheckFileWrittenByThePreviousVersion` (literal `{"checkedAt":...,"newest":"0.10.0"}` and `"newest":null`), `WriteTheUpdateCheckFileAsBefore` (the written text equals today's), and `ReadAMissingOrCorruptUpdateCheckAsNever`: red, then `UpdateCheckRecord` (`Never`, `Checked(DateTimeOffset, NewestRelease)`), `IUpdateCheckRecords` (writing a `Checked`), `JsonFileStore`'s private `UpdateCheckFile` and its two mappings (one switch over every `NewestRelease` case with an `UnreachableException` discard arm), and `InMemoryState` green. Verify `JsonFileStoreShould` passes, including the existing round-trip fact.

## 3. Application

- [ ] 3.1 `UpdateCheckShould`'s facts read and write `UpdateCheckRecord.Checked` with `NewestRelease` values (the record written as `"0.10.0"` and as none) and `ScreenLoopShould`'s `UnwritableRecords` answers a `Checked`: red on the types, then `UpdateCheck` switching once on the record (`Never` checks through the overload; `Checked` takes its `Newest` into the snapshot) and writing `Checked(now, newest)` green, so the two port-null lines and the text switch `shared-snapshot` put there go. Verify `UpdateCheckShould` and `ScreenLoopShould` pass and `UpdateCheck.cs` has no `?.` and no `string?`.
- [ ] 3.2 `SetupFactsReaderShould`'s update-check facts expect `SetupState.Never` and `SetupState.Checked(when, NewestRelease)`; `WordingOfConfigShould`'s facts expect ", newest 0.18.0" for `Found`, ", newest {text}" for `Unreadable` and nothing for `None`: red, then `SetupState.Checked(DateTimeOffset, NewestRelease)`, `SetupFactsReader` and `WordingOfConfig` switching on every case green. Verify both classes pass with today's words.

## 4. Docs, version and the whole build

- [ ] 4.1 `CLAUDE.md`: "(null on failure)" after `IClaudeCli.VersionAsync` becomes "(`NotRead` on failure)". Verify `Select-String -Path CLAUDE.md -Pattern 'null on failure'` finds nothing.
- [ ] 4.2 Confine-check: `Get-ChildItem src -Recurse -Filter *.cs | Select-String -CaseSensitive -Pattern '(ReadConfigAsync|ReadUpdateCheckAsync|VersionAsync|ListAsync)\b.*\?>|Task<string\?> ReadAsync|string\? Newest|DateTimeOffset\? lastCheckedAt|Check\(string\? '` finds only `JsonFileStore`'s private reader and its private `UpdateCheckFile`. Verify by running it.
- [ ] 4.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one step above main's (projected `0.19.0-preview.10`). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 4.4 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate null-free-port-answers` all pass; start a locally installed build (`tools\install-local.ps1`) against the real `%LOCALAPPDATA%\Vantage` once and confirm the header, the Config tab's update-check row and the settings read as before.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
