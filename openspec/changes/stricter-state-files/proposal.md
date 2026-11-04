# Proposal

## Why

`JsonFileStore` reads Torni's own state files (`config.json`, the `tabs`, `recent` and `created` records, `update-check.json`, `last-run.json`) with `JsonSerializerDefaults.Web` and nothing stricter (`src\Source191.Torni.Infrastructure\State\JsonFileStore.cs:25-29`). So a damaged or hand-edited file that holds `null` where a value must be, or leaves a required value out, still deserialises: a tab file with no `colour` comes back in the first palette colour, and a created-worktree or recent-repository record with `"branch": null`, `"path": null` or no such key at all comes back with a null `BranchName` or `FolderPath` in a member the compiler believes can never be null, and fails later, far from the file, as a `NullReferenceException`. Nullable reference types cannot see this, because the null arrives through reflection, and since the guard-nothing-inside decision (#207) no parameter guard stands behind it either. A key written twice is also read without complaint, the last one winning. The .NET 9 and 10 serialiser can refuse all three at the line that reads the file, which is where CLAUDE.md says a null from outside belongs.

`JsonSerializerDefaults.Strict` was considered and set aside (design.md): it also refuses unknown properties and matches names case-sensitively, so a `config.json` from a newer or older Torni, or one with a typo, would read as missing, every setting would fall back to its default without a word, and the next save from the Config tab would write those defaults over the user's roots.

## What Changes

- **Three serialiser options on the state files:** `JsonFileStore`'s options keep `JsonSerializerDefaults.Web` (camelCase, names matched ignoring case, unknown properties skipped) and add `RespectNullableAnnotations = true`, `RespectRequiredConstructorParameters = true` and `AllowDuplicateProperties = false`. A file that breaks any of them throws `JsonException`, which `ReadJsonAsync` already reads as an absent file (`JsonFileStore.cs:198`), so each file falls back exactly as a corrupt one does today: `config.json` to every default, a record to being skipped, `update-check.json` to never checked, `last-run.json` to never run.
- **Required means non-nullable:** every state record's nullable member becomes optional with an explicit `= null` default, and every non-nullable member stays required. `TorniConfig` (`src\Source191.Torni.Core\Ports\TorniConfig.cs:9`, whose first four members have no default today) and `EditorSetting` gain `= null` on each member, so a `config.json` from an earlier version or edited by hand (`{"editor": {"preset": "Rider"}}`) still reads as the settings spec says; `TabFile`'s `ProcessId` and `ProcessStartedAt`, `UpdateCheckFile`'s `Newest` and `LastRunFile`'s `Version` the same. `CreatedWorktree`, `RecentRepository` and the rest of `TabFile` are all required.
- **Unknown properties still skipped:** a property no version of Torni knows, or one a newer version added, is ignored as it is today, so moving between a local build and a released one never resets the settings.
- **Out of reach, recorded:** the options do not check collection elements (`"repositoryRoots": ["D:\\work", null]` still reads, and `Parsed<FolderPath>()` already drops the null) or the legacy `recent.json`, whose root is a list.
- **What each file gains:** every member of `config.json` is optional and may be `null` (which reads as missing, as today), so the only new refusal there is a property given twice; the tab, created-worktree and recent-repository records refuse a missing or `null` required value and a key given twice, and are skipped; `update-check.json` refuses a missing `checkedAt` (today it reads as the year 1, so the check simply runs again) and a key given twice; `last-run.json` a key given twice.
- **Specs:** an added settings requirement says how a hand-edited `config.json` is read (a property given twice reads as corrupt, an unknown property is ignored, a name matches whatever its letter case); an added session-launch requirement says a created-worktree or recent-repository record that leaves out a value, holds `null` for one or names a key twice is skipped while every other record is still read. Tab registrations have no requirement of their own, so their rule lives in design.md and the tests.
- **Version and changelog:** `Version` one preview step above `main` and one line under Unreleased in `CHANGELOG.md`, under Changed, because a hand-edited file that used to half-read is now refused whole.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings`: an added requirement, "A hand-edited settings file is read leniently but not twice", beside "Settings live in one file with defaults", which stays as it is.
- `session-launch`: an added requirement, "A damaged record is skipped", beside "Torni records what it created", which stays as it is.

## Impact

- **Code:** `src\Source191.Torni.Infrastructure\State\JsonFileStore.cs` (the options and the `RecentRepository`, `UpdateCheckFile` and `LastRunFile` records), `State\TabFile.cs`, `src\Source191.Torni.Core\Ports\TorniConfig.cs` and `Ports\EditorSetting.cs`. No port, handler, reducer or screen changes; `ClaudeAgentsListing`, `NuGetPackageVersions`, `ClaudeSettings` and the pull-request readers are untouched.
- **Tests:** `test\Source191.Torni.Infrastructure.Tests\JsonFileStoreShould.cs` gains facts per file for a `null` value, a missing required value, a key given twice and an unknown property; its existing facts for earlier files (`ReadAnEarlierConfigWithoutTheNewProperties`, `ReadAConfigWithoutABaseAsHavingNone`, `ReadAConfigWithoutAPlaceAsHavingNone`, `ReadAnUpdateCheckFileWithoutNotesAsHavingNone`, `ReadATabFileHoldingAProcessWithoutItsStartTimeAsPending`) must pass unchanged.
- **Users:** a state file damaged by hand in one of these ways now reads as absent instead of half-read; a `config.json` naming a setting twice now reads as corrupt, so every setting falls back to its default. Files Torni wrote itself read as before.

## Out of scope

- `JsonSerializerDefaults.Strict`, refusing unknown properties and case-sensitive names (design.md says why).
- Telling the user that `config.json` could not be read, or naming its unknown keys on the Config tab's Settings file row: a separate change if wanted.
- The `claude agents` listing, nuget.org's responses, Claude's `settings.json` and the `gh` and `az` output, which come from outside Torni and grow fields.
- Multiple Claude accounts, which is a later change.
