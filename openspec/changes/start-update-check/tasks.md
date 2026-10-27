## 1. The console starts the check

- [x] 1.1 `ScreenLoopShould.StartTheUpdateCheckWhenTheConsoleOpens`: with an `InMemoryState` record of 0.10.0 checked an hour ago and a 0.9.0 `UpdateCheck`, a loop that quits on its first screen leaves `UpdateCheckTask` complete and `Status` as `Available(0.9.0, 0.10.0)`, with no request made; red on the missing constructor parameter and property, then `ScreenLoop` takes the `UpdateCheck` and starts `RunAsync` with `Task.Run` before the first screen, keeping the task as `UpdateCheckTask`; the existing `ScreenLoopShould` facts pass with a check that is off
- [x] 1.2 `ScreenLoopShould.LogAFailedUpdateCheckAndKeepRunning`: records that throw `UnauthorizedAccessException` on write and a record a day old leave the exit code 0, `Status` as up to date and one `RecordingLogger` line naming the failure; red, then a catch around the started check writes the new `HandoffLog.UpdateCheckFailed` line and swallows the fault; `LogNothingWhenTheUpdateCheckIsCancelled` shows a cancelled check writes nothing
- [x] 1.3 Refactor: `UpdateCheck`'s summary says `ScreenLoop` starts it; `Program.cs` passes `updates` to the `ScreenLoop`; `dotnet build -c Debug` is clean and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build` passes

## 2. Version and documents

- [x] 2.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to 0.16.1; `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`
- [x] 2.2 `CLAUDE.md`'s `U` fact says the console's `ScreenLoop` starts the check on a pool thread as it opens; the README's update section says the check runs in the background as Vantage opens (it said "after the first screen is up")
- [x] 2.3 Full loop: `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes`, `openspec validate start-update-check`

## Workflow follow-up

- Manual check (test plan section 13): pack 0.16.1 as a plain release version into a folder, install it with `tools\install-local.ps1 -FromArtifact`, and with the U test kit in place the header reads `Vantage: 0.16.1 (0.99.0 available, U to update)`; then `U`. Afterwards delete the local 0.16.1 from `%USERPROFILE%\.nuget\packages\source191.vantage` so the real release is not shadowed.
- Archive the change in the pull request that completes it.
