# Tasks

## 1. The interface and the doors (Domain)

- [ ] 1.1 `Architecture\TextValuesShould`: every Domain record with a string `Value` implements `ITextValue<>`, has exactly one public constructor taking a string, has no static member named `Parse`, `From` or `Of`, and has no static member returning a nullable instance of the type (red for `SessionName`, `WorktreeSlug` and `JobId.Of`). Add `ITextValue<TSelf>` with `TryParse` and `Value`. Verify the fact fails for exactly those reasons.
- [ ] 1.2 `FolderPathShould` and `IdentifiersShould`: `TryParse` returns false and leaves the out value default for bad text, true with the value for good text; `JobId.TryParse(SessionId, out JobId)` replaces `Of`. Implement on `FolderPath`, `SessionId`, `JobId` and `BranchName`. Verify the Domain tests pass.
- [ ] 1.3 `SessionNameShould`: the constructor throws `ArgumentException` where `Validate` refuses and builds the name where it accepts; `TryParse` has the new shape; `Text` is `Value`; `WorktreeSlugShould` (new): `TryParse` replaces `From` and is false for text with no letters or digits. Implement; `Slug` goes. Verify the Domain tests pass and 1.1 is green.
- [ ] 1.4 `TextValuesShould.ParseEveryGoodTextAndSkipTheRest`: `Parsed<T>()` over `IEnumerable<string?>` returns the values that parse, in order. Implement beside the interface. Verify it passes.

## 2. Results without nulls (Application)

- [ ] 2.1 `LaunchPlanShould`, `HereCommandShould`, `HereCommandRunnerShould`: results are `LaunchPlanResult.Planned` or `Refused` (red first). Implement the two cases; `LaunchPlanBuilder`, `HereCommand`, `HereCommandRunner` and `WizardView.Started` switch on them with an `UnreachableException` arm. Verify the Core suite passes.
- [ ] 2.2 `WizardViewShould`: the problem is `WizardProblem.None` when the wizard opens and after a repository is confirmed, and the effect is `WizardEffect.None` for every input but Start and Cancel; `WordingShould`: `Describe(WizardProblem.None)` is the empty string (red first). Implement; the binder tests `is not WizardEffect.None`. Verify the Core suite passes and `dotnet run --project tools\HeadlessTerminal -- check` passes.

## 3. The call sites (Application and Adapters)

- [ ] 3.1 `RepositoryCatalogueShould`, `SessionRowEnricherShould`, `WorktreeInspectorShould`, `WorktreeInventoryLeftoversShould`, `LaunchServiceShould`, `LaunchPreviewShould` and the adopt, rename, resume and remove-worktree handler tests pass against the new shape: `Parsed<T>()` for the two LINQ chains, `WorktreeSlug.TryParse` in the preview and the builder, the discarded bool in the three resume handlers with its comment. Verify the Core suite passes and `grep -rn "TryParse(" src` shows no `??`, `==` or `is not` chain on a result.
- [ ] 3.2 `JsonFileStoreShould`, `ClaudeAgentsListingShould`, `GitWorktreeListingShould`, `ClaudeCliShould`: `TextValueJsonConverter<T>` replaces the folder-path and branch-name converters, the listings use the new shape, and `name.Text` is `name.Value` in `ClaudeCli`, `Wording` and `HereCommandRunner`. Verify the solution suite passes.

## 4. The fixtures

- [ ] 4.1 `SessionTestData.Job(...)` and `Terminal(...)` return the typed session and the row fixtures build on them; replace the forty `As.Job` and `As.Terminal` calls and delete `As`. Verify the Core suite passes and `grep -rn "As\." test` finds nothing.

## 5. Documentation and release

- [ ] 5.1 CLAUDE.md: the three-doors sentence replaces the `TryParse` and `Validate` wording in the code rules; raise `Version` by a patch. Verify `openspec validate text-value-doors`, `.github\scripts\Assert-VersionBumped.ps1` and `dotnet format --verify-no-changes` pass.

## Workflow follow-up

- One pull request, archived in it. After `remove-session-descriptions` and `unreachable-arms`; before `from-factory-spike`.
- Manual check: none beyond the hand-off check; the wizard binder changes two comparisons and the `wizard-as-reducer` drive can be rerun to confirm the screens.
