# Proposal

## Why

Five Domain types wrap text (`FolderPath`, `SessionId`, `JobId`, `BranchName`, `SessionName`) and a sixth derives from it (`WorktreeSlug`). They spell the same three doors four ways: constructors or `Parse` throw; `TryParse`, `From` and `Of` return null; `Validate` returns a reason. `TryParse` returning a nullable borrows a .NET name whose shape is a `bool` with an `out` value, and those seventeen nullable returns are the main source of nulls in Core. Results have the same smell: `LaunchPlanResult` is two nullables with an impossible state, and `WizardState.Problem` is nullable where `Footer` has a `Blank` case. The product owner asked for one convention, no nullable returns, and something that enforces it.

## What Changes

- **One interface names the non-throwing door.** `ITextValue<TSelf>` in Domain declares `static abstract bool TryParse(string? text, out TSelf value)`, with the out value marked non-null when the method returns true, and `string Value`. The five text types implement it. `WorktreeSlug.From` becomes `WorktreeSlug.TryParse` with the same shape, and `JobId.Of(SessionId)` becomes a `TryParse` overload taking a `SessionId`.
- **Constructors are the only throwing door.** `SessionName.Parse` becomes a public constructor, `SessionName.Text` becomes `Value` like the others, and `SessionName.Slug` goes in favour of `WorktreeSlug.TryParse`. No static member returns a nullable instance of its type.
- **`Validate` stays for typed text,** returning the name or the reason; `SessionName` is its only user.
- **Every call site takes the new shape:** seventeen in `src`, including a `Parsed<T>()` extension for the two LINQ chains over listings and the three resume handlers that pass a parsed name or none to the CLI port.
- **Results carry no nulls.** `LaunchPlanResult` becomes `Planned(plan)` or `Refused(problem)`; `WizardProblem.None` and `WizardEffect.None` replace the wizard's nullable problem and effect, as `Footer.Blank` does for footers.
- **One generic JSON converter** for text values replaces the folder-path and branch-name converters; the tab-owner converter stays.
- **Typed fixtures replace the test cast.** `SessionTestData` hands out `BackgroundSession` and `InteractiveSession` directly and `As.Job`, `As.Terminal` are deleted.
- **An architecture fact enforces the convention:** every Domain record with a string `Value` implements the interface, has exactly one public constructor and it takes a string, has no static member named `Parse`, `From` or `Of`, and no static member returning a nullable instance of the type.
- **CLAUDE.md** states the three doors and which text goes through each.
- **Version:** a patch bump; nothing the user sees changes.

Out of scope: a `From` factory, which `from-factory-spike` tries afterwards; a general result type; `IParsable<T>`, which nothing here consumes; and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a refactor with no behaviour change; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Domain:** `FolderPath.cs`, `Identifiers.cs`, `SessionName.cs`, `WorktreeSlug.cs`, new `ITextValue.cs` with the `Parsed<T>()` extension.
- **Application:** `Launch\LaunchPlan.cs` (`LaunchPlanResult`), `LaunchPreview.cs`, `HereCommand.cs`, `HereCommandRunner.cs`, `LaunchService.cs`, `RepositoryCatalogue.cs`, `SessionRowEnricher.cs`, `WorktreeInspector.cs`, `WorktreeInventory.cs`, `Views\WizardInput.cs`, `WizardState.cs`, `WizardView.cs`, `Wording.cs`, `Actions\AdoptSessionHandler.cs`, `RenameSessionHandler.cs`, `ResumeSessionsHandler.cs`, `RemoveWorktreeHandler.cs`.
- **Infrastructure:** `Claude\ClaudeAgentsListing.cs`, `ClaudeCli.cs`, `Git\GitWorktreeListing.cs`, `State\FolderPathJsonConverter.cs`, `IdentifierJsonConverters.cs`, `JsonFileStore.cs`.
- **Tui:** `LaunchWizardDialog.cs` (the two `None` checks).
- **Tests:** the Domain tests, `WizardViewShould`, `LaunchPlanShould`, `HereCommandShould`, `HereCommandRunnerShould`, `WordingShould`, the listing and store tests, `SessionTestData`, the forty `As` call sites, and a new `Architecture\TextValuesShould`.
- **Docs:** CLAUDE.md; `Version`.
