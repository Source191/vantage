# Tasks

## 1. The type and its members in the application layer

- [x] 1.1 Rename the `Footer` type to `StatusMessage` (and `Footer.cs` to `StatusMessage.cs`), keeping every case and `None`, across `src\`, `test\` and `tools\`. The existing tests are the guard: `WordingShould.HaveWordsForEveryFooterCase`, `StatusTonesShould` and every reducer test must pass unchanged in what they assert. Verify with `dotnet build -c Debug` (no warnings) and the Core suite.
- [x] 1.2 Rename each view state's `Footer` property to `Message`, `IScreenDefinition.WithFooter` to `WithMessage` and `IEffectContext.ShowFooter` to `ShowMessage`, and parameters and locals named `footer` to `message` (or a clearer name where `message` is taken). Verify with the build and the whole suite.
- [x] 1.3 Rename the tests and members whose names say footer (`HaveWordsForEveryFooterCase`, `FooterCases`, `FooterTones`, `FooterTable`, `ChooseAToneForEveryFooterCase`, `GiveEachFooterItsTone`, `NameAnyOtherFooterByItsKind`, `NameTheEffectAWorkingFooterWaitsFor`, `SayNothingForNoFooter`, `SayTheConfigFooters`, `Footers`) and reword comments, so a case-insensitive search for footer under `src\`, `test\` and `tools\` finds nothing. Verify with that search, the build, the whole suite and `dotnet format --verify-no-changes`.

## 2. The words around it

- [x] 2.1 Reword `README.md` (five mentions) and `CLAUDE.md` (two) to say the status line or `StatusMessage`, and check `CONTRIBUTING.md`. Verify with a case-insensitive search for footer in the three files that finds nothing.
- [ ] 2.2 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next preview. Verify with `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` once committed.

## 3. Checks

- [ ] 3.1 Run the hand-off check, and `openspec validate rename-footer-to-status-message --strict`. Verify both pass, then archive the change and check that `openspec validate --specs --strict` passes and no main spec says footer.

## Workflow follow-up

- Open the pull request with the archive in it; the `check` job's smoke step runs the packed tool.
