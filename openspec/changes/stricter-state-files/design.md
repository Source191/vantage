# Design

## Context

See proposal.md for why. One options object, `JsonFileStore.Options` (`src\Source191.Torni.Infrastructure\State\JsonFileStore.cs:25-29`), reads and writes every state file: `JsonSerializerDefaults.Web`, `WriteIndented`, a `JsonStringEnumConverter` that refuses numbers, and the `TextValueJsonConverter<FolderPath>`, `TextValueJsonConverter<BranchName>` and `TabOwnerJsonConverter` converters. `ReadJsonAsync` (`:184-202`) turns an `IOException`, `JsonException` or `UnauthorizedAccessException` into `null`, which each reader treats as an absent file: `ReadConfigAsync` answers `TorniConfig.Missing`, `ReadAllAsync` skips the record, `ReadUpdateCheckAsync` answers `Never`, `ReadLastRunAsync` answers `Never`. The records read, and which of their members are nullable today:

| File | Record | Non-nullable (required after this change) | Nullable (optional after this change) |
|---|---|---|---|
| `config.json` | `Ports\TorniConfig` | none | all six; four have no default today |
| (inside it) | `Ports\EditorSetting` | none | `Preset`, `Executable`, `Arguments`; none has a default today |
| `tabs\<id>.json` | `State\TabFile` | `Id`, `SessionId` (the owner), `Colour`, `CreatedAt` | `ProcessId`, `ProcessStartedAt` |
| `created\<key>.json` | `Domain\CreatedWorktree` | `Path`, `Branch`, `Repository`, `CreatedAt` | none |
| `recent\<key>.json` | `JsonFileStore.RecentRepository` | `Path`, `UsedAt` | none |
| `update-check.json` | `JsonFileStore.UpdateCheckFile` | `CheckedAt` | `Newest`, `Notes` (already `= null`) |
| `last-run.json` | `JsonFileStore.LastRunFile` | none | `Version` |
| `recent.json` (legacy, read once) | `List<string>` | n/a: a root-level list | n/a |

The converters leave `HandleNull` at its default, false for a reference type, so today a JSON `null` for a `FolderPath`, `BranchName` or `TabOwner` member is assigned without the converter being asked, which is how a null reaches a member the compiler treats as never null. Enum and `DateTimeOffset` members are value types, which the serialiser already refuses to set to `null`; a missing one silently takes its default (the first palette colour, 1 January 0001).

## Goals / Non-Goals

**Goals:**
- Each state file is refused at the line that reads it when it holds `null` for a non-nullable member, leaves a non-nullable member out, or names a key twice, and is then treated exactly as a corrupt file is today.
- Every file Torni wrote, in this version or an earlier one, reads as it does today, and so does every `config.json` scenario in the settings spec.

**Non-Goals:**
- Reporting a refused file anywhere (status line, Config tab, diagnostic event): today's corrupt file is silent too, and changing that is its own change.
- Refusing unknown properties or wrong-case names; the readers of JSON from outside Torni; the elements of a list (`repositoryRoots`), which the serialiser's nullability check does not reach.

## Decisions

**Web defaults plus three options, not `JsonSerializerDefaults.Strict`.** `Strict` is these three plus `UnmappedMemberHandling = Disallow` and case-sensitive names. Refusing an unknown property would make a `config.json` written by a newer Torni (or an older one, once a property is retired) read as missing, so every setting falls back to its default without a word and the next save from the Config tab writes those defaults over the user's roots; the owner moves between local builds and released versions, so that is a daily hazard, not a theoretical one. Case-sensitive names would refuse a hand edit that spells `RepositoryRoots`, for no gain, since Torni always writes camelCase. Alternatives: `Strict` with camelCase restored (rejected for the reasons above, with the owner on 3 November 2026), or leaving the options alone (rejected: the nulls get through, as proposal.md says).

**Nullable means optional, non-nullable means required, stated by the record itself.** `RespectRequiredConstructorParameters` treats a positional record's parameter as required unless it has a default value, so each nullable member gets an explicit `= null` and each non-nullable one keeps none. One rule, readable at the declaration, and the compiler already makes Torni's own writers supply every non-nullable member. `TorniConfig` and `EditorSetting` are wholly optional, which is what the settings spec's earlier-file and hand-edit scenarios need; `TorniConfig.Missing` becomes `new()`. Alternatives: `[JsonRequired]` on the required members (an attribute per member, and Ports would reference System.Text.Json, which it does not today), or the `required` keyword (positional records cannot carry it).

**The refusal reuses the corrupt-file path.** Every new refusal surfaces as a `JsonException` from `DeserializeAsync`, which `ReadJsonAsync` already catches, so no reader changes and the settings spec's corrupt-file scenarios cover the fallback. Alternative: a reader that reports which rule a file broke (rejected with the non-goal above).

**The options stay in `JsonFileStore` alone.** `ClaudeAgentsListing` and `NuGetPackageVersions` read JSON that Claude Code and nuget.org extend over time, and keep their own lenient options; `ClaudeSettings` and the pull-request readers use the DOM, which these options do not govern.

## Risks / Trade-offs

- [An earlier version wrote a file without a member that is now required] → each required member's history is checked before the options are turned on (`git log -p` on `TabFile.cs`, `CreatedWorktree.cs` and the `RecentRepository` and `UpdateCheckFile` records): a member some released version left out becomes optional with a default instead. A tab file is short-lived (it goes stale with its shell), so the cost of getting one wrong is a tab Enter opens afresh rather than finds.
- [The serialiser treats a converter's type differently from the documentation] → the nullability check is documented for properties and constructor parameters whatever their converter, but `FolderPath`, `BranchName` and `TabOwner` all go through custom converters, so the tests write a `null` for each of them and assert the record is skipped.
- [A `config.json` naming a setting twice now resets every setting] → accepted: that file was already ambiguous, and it is treated exactly as the corrupt file the spec already describes.
- [Writing with the options throws where a non-nullable member is null] → cannot happen from Torni's own code under nullable reference types; a write that did throw would surface as the save failing, as a full disc does today.

## Migration Plan

None: the files keep their format, and no version rewrites or deletes them. Rolling back is removing the three options and the `= null` defaults.
