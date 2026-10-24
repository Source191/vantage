# Proposal

## Why

User-facing English is composed in three layers: the reducers write footers (`SessionsView.SessionAction`, `Rename`, `Adopt`), the handlers return sentences (`CloseEmptyTerminalsHandler`'s summary, `ResumeSessionsHandler`, `RenameSessionHandler`, `OpenTabHandler`'s switch to a live tab, `StartSessionHandler`), and `Tui\DialogEffects` composes more (the branch question, "Branch kept."). The same fact ("has no messages yet") is worded three ways, a wording change touches three files and three test classes, and the force-removal hint bug came from one layer parsing another's sentence. `DeleteOutcome`, `LaunchOutcome` and `WorktreeRemovalOutcome` already show the intended shape.

## What Changes

- Every handler returns an outcome record (`StopOutcome`, `AdoptOutcome`, `RenameOutcome`, `CloseEmptyOutcome`, `DeleteOutcome`, `ResumeOutcome`, `OpenTabOutcome`, `RemoveOutcome`, `PruneOutcome`, `OpenShellOutcome`, `LaunchOutcome`) carrying facts, never prose.
- Reducers return a `Footer` value (a small sealed hierarchy such as `NoSelection`, `RunsInOwnTerminal(name, hasTranscript)`, `AlreadyOpenInTab(name)`, `NothingToResume(name)`, `IsWorking(name, verb)`) instead of strings.
- One `Wording` class in `Application` turns every outcome and footer value into the sentence shown today, with one test file holding every sentence.
- `TableScreen` asks `Wording` for the text at the last moment; the refusal and the confirmation dialog texts come from the same class.

Out of scope: changing any sentence (the test file is seeded from today's strings, byte for byte), localisation, the wizard's own messages, multiple Claude accounts. Depends on `effect-handlers`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Every sentence the user sees is unchanged; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Core`: `Application\Wording.cs`, `Application\Views\Footer.cs`, outcome records beside each handler; reducers and handlers lose their string literals.
- `Tui`: `TableScreen` and `DialogEffects` call `Wording`.
- Tests: `WordingShould` is new and large; handler and reducer tests assert on outcome values instead of sentences, which makes them shorter.
