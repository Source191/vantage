# Tasks

## 1. Domain

- [x] 1.1 `ReleaseVersionShould.CompareBySemanticVersionPrecedence` (`0.10.0` above `0.9.0`, `1.0.0` above `1.0.0-rc.1`, build metadata ignored) and `KnowAPrerelease` (`0.10.0-local`, `0.4.10-ci.12`): implement `ReleaseVersion` with its `TryParse`; verify the facts pass
- [x] 1.2 `UpdateCheckRuleShould.CheckWhenNeverCheckedOrADayOld`, `NotCheckWithinADay`, `NeverCheckAPrerelease`, `NeverCheckWhenTurnedOff` (with `FakeTimeProvider` instants): implement `UpdateCheckRule.ShouldCheck`; verify the facts pass
- [x] 1.3 `UpdateCheckRuleShould.TakeTheNewestRelease` (prereleases and unparsable text skipped, none found answers null) and `OfferOnlyAHigherRelease`: implement `Newest` and `Offer`; verify the facts pass
- [x] 1.4 Run Stryker from `test\Source191.Vantage.Core.Tests` with `--test-runner mtp --project Source191.Vantage.Core.csproj --mutate "**/Domain/ReleaseVersion.cs" --mutate "**/Domain/UpdateCheckRule.cs"`; verify no surviving mutant is left unexplained (97 %: the two survivors are equivalent mutants of `dash < 0`, where a leading dash fails to parse either way)

## 2. Ports and adapters

- [x] 2.1 Add `IPackageVersions` (nullable answer, documented in the interface comment) and `FakePackageVersions` in TestDoubles (an answer, a failure, a call count); verify `LayeringShould` passes
- [x] 2.2 `NuGetPackageVersionsShould.ListTheVersionsFromTheIndex` (a stub `HttpMessageHandler` asserting the lowercased id in the URL and the user agent), `AnswerNullOnNotFoundTimeoutOrMalformedJson`: implement `NuGetPackageVersions`; verify the facts pass in Infrastructure.Tests
- [x] 2.3 `JsonFileStoreShould.RoundTripTheUpdateCheckRecord` and `AnswerNoRecordWhenTheFileIsMissingOrCorrupt`: add the record to the state ports, `JsonFileStore` and `InMemoryState`; verify the facts pass
- [x] 2.4 `SettingsShould.CheckForUpdatesWhenTheFileSaysNothing` and `JsonFileStoreShould.WriteCheckForUpdates`: add `CheckForUpdates` to `VantageConfig` and `Settings`; verify the facts pass and an earlier `config.json` fixture still reads

## 3. Update check

- [x] 3.1 `UpdateCheckShould.OfferTheRecordedReleaseAtStart`, `RequestOnceAndRecordTheNewest`, `NotRequestWithinADay`, `RecordNothingWhenTheRequestFails`, `NotRequestForAPrerelease`, `NotRequestWhenTurnedOff` (sociable: the real rule and `InMemoryState`, `FakePackageVersions`, `FakeTimeProvider`): implement `UpdateCheck`; verify the facts pass
- [x] 3.2 `WordingShould.NameAnAvailableUpdateInTheHeader` (`0.9.0 (0.10.0 available, U to update)` and plain `0.9.0`): extend the header's Vantage value to read `UpdateCheck`; verify the fact passes

## 4. Key, effects and handler

- [x] 4.1 `TabScriptsShould.WaitUpdateAndRestartInTheUpdateTab` (the process id, the update command, the restart only on exit code 0, every value through `PowerShellScript.Quote`): implement `TabScripts.Update`; verify the fact passes and `PowerShellQuotingShould` still passes
- [x] 4.2 `StartUpdateHandlerShould.OpenTheUpdateTabAndReportIt` (with `FakeTerminal`): implement `StartUpdateHandler`, returning `UpdateStarted(Newest)` declared in its own file; verify the fact passes
- [x] 4.3 `WordingOfOutcomesShould.SayTheUpdateStarted`: add the outcome's words; verify the reflection fact passes
- [x] 4.4 `SessionsViewShould.AskToUpdateWhenAnUpdateIsKnown`, `SayUpToDateOtherwise`, `SayTheCheckIsOff`, and the same three in `WorktreesViewShould`: add `UpdateVantage` (an `IDialogEffect`), `StartUpdate`, `Footer.UpToDate`, `Footer.UpdateCheckOff` and the `U` cases; verify the facts pass
- [x] 4.5 `WordingShould.AskBeforeUpdating` and the footer words: add them to `Wording`; verify the reflection fact that every footer case has words passes
- [x] 4.6 Register `StartUpdateHandler` in `Composition.Handlers` and the `UpdateVantage` flow in `DialogEffects` (confirm, dispatch, then leave with `Quit`); verify `EffectDispatcherShould`, `DialogEffectsShould` and `ScreenLoopShould` pass

## 5. Screens, wiring and documentation

- [x] 5.1 The `<U> Update` hint and help line in both views, the check box in `SettingsControls` and its input in `SettingsView`; verify `SettingsViewShould` and `WordingShould`'s help facts pass and `dotnet build -c Debug` is clean
- [x] 5.2 `Program.cs`: build `NuGetPackageVersions` and `UpdateCheck`, start the check on a pool thread after the first screen is up, pass the process id to the handler; verify `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass
- [x] 5.3 Run `dotnet run --project tools\HeadlessTerminal -- check`, and a `drive` from 8000 ms with a recorded `update-check.json` naming a higher version: verify the header shows the notice, `U` opens the confirmation and Esc cancels it, and the check exits 0 (the check ran and exited 0; the `drive` with a recorded `update-check.json` is deferred to `public-release` 8.7, because a local or CI build is a prerelease and never shows the notice)
- [x] 5.4 README: the `U` key in both key tables, the header notice, `checkForUpdates` in "Where Vantage keeps things" and the settings dialog, `update-check.json` in the state table, and what the check sends; CLAUDE.md: the update runs in its own tab because a running `vantage.exe` locks its files; verify the README's keys match `Wording`'s help text
- [x] 5.5 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor after `main`'s; verify `.github\scripts\Assert-VersionBumped.ps1` passes locally

## Workflow follow-up

- P12 in `public-release`'s sequence. Merge after `header-and-help` (P6) and `settings-and-worktree-root` (P7); it needs nothing from P8 to P10, but rebase the key hints and help on `main` if they landed in between.
- The confirmed-update scenario (a real `dotnet tool update` of an installed package, and the shim not locked once Vantage has exited) is checked at the terminal in `public-release`'s final manual test, before anything is on nuget.org: a folder source (`dotnet nuget add source <folder> -n vantage-test`) holds a higher version and a recorded `update-check.json` names it; remove the source afterwards. The real request to nuget.org is checked once after the first publish.
- Archive the change in the same pull request (`/opsx:archive`), so `openspec\specs\tool-updates` describes `main`; verify `openspec validate --all` passes after archiving.
