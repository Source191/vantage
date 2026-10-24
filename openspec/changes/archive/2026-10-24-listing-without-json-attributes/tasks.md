# Tasks

## 1. Listing parser without attributes

- [x] 1.1 In `ClaudeAgentsListing`, build `Options` from `JsonSerializerDefaults.Web`, delete the ten `JsonPropertyName` attributes from `Row` and the unused `System.Text.Json.Serialization` using; verify `dotnet build -c Debug` is clean and `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` passes, in particular every `ClaudeAgentsListingShould` fact that reads a multi-word key (`startedAt`, `sessionId`, `waitingFor`)
- [x] 1.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` from 0.4.4 to 0.4.5; verify `dotnet format --verify-no-changes` passes and `grep -rn JsonPropertyName src test` finds nothing

## Workflow follow-up

- Commit once, then archive the change in the pull request that completes it.
