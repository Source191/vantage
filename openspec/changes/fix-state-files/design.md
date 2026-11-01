# Design

## Context

`JsonFileStore` (`src\Source191.Vantage.Infrastructure\State\JsonFileStore.cs`) is the one adapter behind `ITabRegistry`, `IRecentRepositories`, `ICreatedWorktrees`, `IVantageConfig` and `IUpdateCheckRecords`. Tabs and recent repositories are already one file each under `tabs\` and `recent\` (`ReadAllAsync` enumerates `*.json`; `RecentFile` keys a path by `FileKey(path.ToUpperInvariant())`, the first 16 hex digits of its SHA-256), and `ImportLegacyRecentAsync` (line 115) turns the pre-0.3.0 `recent.json` into records once. Created worktrees are still one list in `created.json` (lines 68 to 82). Every write goes through `WriteJsonAsync` (line 198), which truncates in place, and every read through `ReadJsonAsync` (line 169), which opens with `FileShare.ReadWrite | FileShare.Delete` and answers null for `IOException` and `JsonException`. `ClaudeSettings.EnableTerminalBellAsync` (`Claude\ClaudeSettings.cs:61-64`) is the house's existing write-beside-and-move. `FolderPath` compares ordinal-ignore-case (`Domain\FolderPath.cs:50`), which is the rule `RecordCreatedWorktreeAsync`'s `created.Path != worktree.Path` applies today. See proposal.md for the findings.

## Goals / Non-Goals

**Goals:**
- One write path and one read path for every state file, each tolerant of a crash, a second Vantage process and a file Vantage may not read.
- `ICreatedWorktrees` and `InMemoryState` unchanged, so no Application code or Core test moves.

**Non-Goals:**
- A cross-process lock: one file per record is the house's answer, and a lock would be the first in the codebase.
- Changing `TryDelete` (line 210), which catches `IOException` alone; a denied delete still throws to the key that asked.
- Removing `.tmp` files a crash leaves: `ReadAllAsync` never reads them, and they are a few hundred bytes.

## Decisions

### A unique temporary name per write, moved over the target
`WriteJsonAsync` writes to `<file>.<guid:N>.tmp` beside the target with `FileMode.CreateNew`, then `File.Move(temporary, file, overwrite: true)`; if the serialise or the move throws, the temporary is deleted and the exception rethrown. The name is unique because a tab file has two writers (the console writes it pending, the tab's `claim-tab` writes it claimed), and a fixed suffix as `ClaudeSettings` uses would let one truncate the other's temporary; it ends `.tmp`, never `.json`, so `ReadAllAsync` never reads a half-written record. Alternative: `ClaudeSettings`'s fixed `.vantage-tmp` suffix for consistency; rejected for the two-writer case, and `ClaudeSettings` is left as it is because `settings.json` has one writer.

### The existing read sharing stays, and the existing fact proves the move
Readers keep `FileShare.ReadWrite | FileShare.Delete`, which is what lets a move replace a file another Vantage process has open. `WriteARecordWhileAnotherProcessHasItOpenForReading` already holds a record open that way and writes it, so it becomes the proof the moment the implementation changes: if it fails, the approach is wrong and nothing else in this change lands first. A holder without delete sharing (an editor) now makes the write throw rather than half-succeed; `SaveSettingsHandler` reports it as its failed outcome.

### `created\` mirrors `recent\`, imported in all three methods
`CreatedFile(FolderPath path)` is `created\<FileKey(path.Value.ToUpperInvariant())>.json`, so recording the same folder under another spelling replaces one record, as `FolderPath` equality does today. `ImportLegacyCreatedAsync` reads the old `created.json` list, writes each record to its file (`WriteJsonAsync` once per record) and deletes the list, and runs at the start of read, record and forget alike: a forget that skipped it would leave the forgotten worktree in the list for the next read to revive. Two processes importing at once write the same keys and `TryDelete` tolerates the race. Alternative: import on read and record only, as recent does; rejected because recent has no forget.

### An integer enum is a corrupt file; `SettingsFile` guards the door anyway
`new JsonStringEnumConverter(allowIntegerValues: false)` makes `"defaultBase": 7` a `JsonException`, which `ReadConfigAsync` already turns into `VantageConfig.Missing`: the whole file reads as every default, exactly as an unknown name such as `"defaultBase": "Trunk"` does today. This is deliberately unlike `EditorPreset`, which falls back on its own because `EditorSetting.Preset` is text that `SettingsFile.Editor` parses; a per-property fallback for the base and place would mean holding them as text in `VantageConfig` and is out of scope. `SettingsFile.From` still maps an undefined `DefaultBase` or `DefaultPlace` to `Settings.BaseWhenUnset` and `Settings.PlaceWhenUnset` with `Enum.IsDefined`, as line 41 does for the preset, so no undefined value reaches `Settings.Resolve` from any `VantageConfig`, including one a test builds. Alternative: throw `ArgumentOutOfRangeException` in `Settings.Resolve` too, as the review suggested; not taken, because `SettingsFile` is the only caller that reads from outside and the brief asks for the file door alone; the owner may add it.

### `UnauthorizedAccessException` is unreadable, tested by a deny ACE
`ReadJsonAsync` catches `IOException`, `JsonException` and `UnauthorizedAccessException` in one `when` filter, as `ClaudeSettings.cs:46` does. The fact denies the current user `FileSystemRights.ReadData` alone on the file, through `FileSystemAclExtensions`: denying `Read` would also deny `ReadAttributes`, make `File.Exists` answer false and skip the catch, and a directory named `config.json` never reaches it for the same reason. Delete is not denied, so `Dispose` still removes the folder. Deny entries bind administrators too, so the fact holds on the CI runner.

## Risks / Trade-offs

- [`File.Move(overwrite: true)` is refused over a file held with delete sharing] → `WriteARecordWhileAnotherProcessHasItOpenForReading` fails at once in task 1.1; the change stops there and is re-planned.
- [The move's failure over a holder without delete sharing is `UnauthorizedAccessException` (ERROR_ACCESS_DENIED), not `IOException`] → either reaches `SaveSettingsHandler`'s failed outcome; the fact in task 1.2 records which, and the temporary is deleted in both cases.
- [An older Vantage, run after this one, does not see `created\`] → its removals ask about every branch, the safe direction; `created.json` is not kept in parallel because two homes for one fact is what C-04 is.
- [A crash between writing the temporary and the move leaves a `.tmp` file] → never read, never listed; accepted.
- [The ACL calls are Windows-only and the test project targets plain `net10.0`, so CA1416 fails the build] → `JsonFileStoreShould` carries `[SupportedOSPlatform("windows")]`, as every HeadlessTerminal type does; the tool runs nowhere else.
- [The deny-ACE fact behaves differently on a non-NTFS temp folder] → `Path.GetTempPath()` is on the system drive on every supported machine; the fact is skipped with a reason if the ACL cannot be set.
