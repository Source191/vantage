# Design

## Context

See proposal.md, Why. The description enters through two doors, the wizard's field (`WizardInput.DescriptionChanged`, `WizardField.Description`) and `vantage here -d`, travels as `LaunchRequest.DescriptionText` and `LaunchPlan.Description`, is written by `LaunchService.RecordDescriptionAsync` through `ISessionDescriptions`, read back by `SessionRowEnricher` into `SessionRow.Description`, and shown by `SessionsScreenDefinition` as the DESCRIPTION column. `JsonFileStore` keeps one file per session under `descriptions\` and imports a legacy `sessions.json` dictionary on first use. The filter does not search it and nothing else reads it.

## Goals / Non-Goals

**Goals:**
- No trace of the description in code, tests, specs or the README.
- Existing state folders keep working unchanged.

**Non-Goals:**
- A replacement label or notes feature.
- Deleting old description files for the user.
- Any other wizard change; the field's removal is the only one.

## Decisions

- **Remove outright, no half measures.** The product owner's decision: the field, the option, the column, the port and the folder code all go. A hidden field or a kept column would leave the state port alive for nothing.
- **Old files are ignored, not deleted.** Vantage does not delete state it no longer reads; `descriptions\` and a leftover `sessions.json` stay where they are. The README names the folder for anyone who wants to tidy. Alternative rejected: a one-off clean-up on start, which is a destructive action for no benefit.
- **`-d` is an unknown option.** `HereCommand` already answers `LaunchProblem.UnknownOption` for anything else starting with a dash, so the removed option needs no special sentence. Alternative rejected: a transitional "the description option has been removed" message, which nobody would read twice.
- **Enter in the name goes to Where.** `WizardView`'s `NextField` becomes Name to Mode when the chooser shows, and stays in the name for a fixed directory, where Where is hidden. The hand-off check and a scripted drive confirm the dialog, since `WizardControls` loses a field and the binder two subscriptions.
- **Order.** First of the agreed set, so `text-value-doors` touches a wizard and a launch request without the dead field.
- **Version.** Minor: a `vantage here` option and a column are removed, which the repository's semver rule calls breaking while pre-1.0.

## Risks / Trade-offs

- [Someone has `-d` in a script] → exit 2 with the usage line, which names the options that remain.
- [The wizard's keyboard flow changes] → the before-and-after drive from `wizard-as-reducer` is rerun without its description Enter, and the hand-off check runs because Tui changed.
