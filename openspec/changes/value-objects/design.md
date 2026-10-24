# Design

## Context

See proposal.md. `RepositoryPath` already exists with `Name` and `Parent` but is used only at the edges; `SessionName` and `WorktreeSlug` show the validation style to follow. `Session.Kind` plus a nullable `JobId` is the invariant the reducers guard by hand in `SessionAction`, `Rename` and `Adopt`.

## Goals / Non-Goals

**Goals:** one definition of path equality; no `!` on `JobId`; the compiler tells the reducers which session kind they hold; on-disc JSON unchanged.

**Non-Goals:** a general "strongly typed id" library, wrapping free text, changing any CLI argument.

## Decisions

- **`FolderPath` normalises at construction**: `Path.GetFullPath`, trailing separators trimmed, forward slashes converted (git prints them). Equality is `OrdinalIgnoreCase` on the normalised value, which is what every one of the six helpers does today. `IsUnder(FolderPath other)` replaces the prefix check in `WorktreeInventory.LiveSessionIn`. `RepositoryPath` is removed and its two members move to `FolderPath`. Alternative rejected: keeping strings and a static `Paths.Same` helper; that removes the duplication but not the chance of forgetting to call it.
- **`SessionId` and `JobId` validate what the CLI facts in CLAUDE.md say**: a job id is the first eight characters of the session id, hex; a session id is a non-blank string (the CLI's format is not documented, so no stricter rule). `JobId.Of(SessionId)` derives one from the other. `BranchName` is non-blank and contains no whitespace.
- **The session hierarchy** is `abstract record Session(SessionId Id, string Name, FolderPath WorkingDirectory, DateTimeOffset StartedAt, DisplayState State)` with `sealed record BackgroundSession(..., JobId JobId, int? ProcessId)` and `sealed record InteractiveSession(..., int ProcessId)`. A stopped background session has a job id and no process; an interactive session always has a process (a listing row without pid and without id does not occur, per the CLI facts; `ClaudeAgentsListing` maps a row with neither to a stopped `BackgroundSession` with a derived job id and a test records that decision). `SessionsView.SessionAction` becomes a type pattern: `InteractiveSession` rows get the own-terminal footer; `BackgroundSession` rows proceed. `Sessions.OrderedByAttention` is unchanged.
- **`TabRegistration` owner.** `OwnTab` registers Vantage's own tab under the sentinel key `"vantage"` in `TabRegistration.SessionId`. With `SessionId` typed, the field becomes a `TabOwner` union (`ForSession(SessionId)` or `Vantage`) that serialises to the same two strings, so `SessionRowEnricher.LiveTabSessionsAsync` and `OwnTab` stop sharing a magic string.
- **JSON converters** for `FolderPath`, `SessionId`, `JobId`, `BranchName` and `TabOwner` write the raw string, registered once on `JsonFileStore`'s options; `JsonFileStoreShould.ReadFilesWrittenByTheCurrentVersion` reads fixture files captured before this change.
- **Order of work**: `FolderPath` first because it touches the most files but changes no logic; then the ids; then the `Session` split, which is the one that simplifies reducer code.

## Risks / Trade-offs

- [A large mechanical diff across fixtures] → one commit per value object, each with the build green; fixtures use small factory methods (`Paths.Repo("Northwind.Orders")`) so later edits are local.
- [`GetFullPath` on a relative string in a test fixture resolves against the test's working directory] → fixtures use absolute `C:\Users\me\...` paths as CLAUDE.md requires; `FolderPathShould.RejectRelativePaths` makes a relative path an argument error.
- [The listing yields a row that fits neither subtype] → `ClaudeAgentsListingShould.MapARowWithNeitherPidNorIdToAStoppedBackgroundSession` records the fallback and the CLI facts in CLAUDE.md are updated if the CLI changes.
