# Proposal

## Why

An audit of the 48 `switch` statements and expressions in the solution found them correct, but not all of them guarded: every interpreter over the effect, footer and outcome records has a reflection fact that fails when a case is missing, except `Tui\DialogEffects.Show`, where a dialog effect with no flow is only found at runtime as a "No dialog is defined for..." footer; and every switch over a closed enum (`DisplayStateKind`, `WorktreeHealthKind`, `RemovalOutcome`, `BranchAction`, `LaunchOutcomeKind`, `KeyName`) ends in a `_` or `default` arm, so a new enum member compiles silently and lands in whichever arm happened to be last. The compiler cannot help today: under `AnalysisLevel` 10.0-recommended with warnings as errors, a fully covered enum switch expression with no discard arm fails the build with CS8524 (an unnamed enum value is not covered), which forces the discard arm that hides CS8509 (a named member is not covered). A probe project built with this repository's `Directory.Build.props` confirmed both.

## What Changes

- `DialogEffects` keeps its flows in a type-keyed registry and exposes `Handles(Type)`, mirroring `EffectDispatcher`; a new `DialogEffectsShould` theory in `Core.Tests` enumerates every `IDialogEffect` implementer and fails when one has no flow. The `NoDialogFor` footer stays as the runtime fallback.
- `Directory.Build.props` adds CS8524 to `NoWarn`, with a comment stating the stance: enums in this solution hold only their named members, so a switch expression that names every member is exhaustive and a new member fails the build with CS8509.
- The six switch expressions over closed enums drop their discard arm and name every member: `Domain\Sessions.AttentionRank`, `Tui\Palette.ForState`, `Tui\WorktreesScreenDefinition.RowColour`, `Application\Actions\RemoveWorktreeHandler.RemoveAsync` (on `RemovalOutcome`), `Application\WordingOfOutcomes.Here` (on `LaunchOutcomeKind`) and `Application\Views\ListSelection.EditFilter` (on `KeyName`). The `BranchAction` switch statement in `RemoveWorktreeHandler.RemoveThroughGitAsync` becomes an expression so it gains the same check.
- An observation, not a code change: the comment on `Domain\DisplayState.From` says status wins while the process is alive, yet `busy` paired with state `done`, `blocked` or `failed` falls to Unknown and shows the raw "busy/done" in grey. Whether the CLI ever emits that pairing is checked against a live session and recorded in CLAUDE.md's CLI facts; if it does and should read as working, that is a change to the `session-listing` spec and gets its own OpenSpec change.
- CLAUDE.md's code rules name `DialogEffectsShould` beside the other completeness facts and record the enum stance.
- `Version` rises from 0.4.1 to 0.4.3 (main reached 0.4.2 while this change was open): the pull request touches `src\` and `Directory.Build.props`, and nothing a user sees changes, so it is a patch.

Out of scope: any key, footer, sentence or colour change; the two switch statements that are not pure enum switches (`Program.cs`, `HereCommandRunner`), which stay as they are; the property-pattern decision tables (`DisplayState.From`, `RemovalDecision.For`, `WorktreeHealth.From`), which the compiler can never check and the domain tests already cover; the readability nits from the audit (the two `'d'` arms in `SessionsView.Act` separated by `'C'`, `Program.cs` dispatching on the command twice, the two `Placeholder` switches in the Wording tests); moving the listing-to-state mapping into an adapter, which `consolidate-helpers` does; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Tests, a build setting and refactors with no observable change; `.openspec.yaml` declares `skip_specs: true`. If the DisplayState observation finds a pairing the spec should name, that follow-up change modifies `session-listing`, not this one.

## Impact

- `Directory.Build.props`: `NoWarn` gains CS8524.
- `Core`: `Domain\Sessions.cs`, `Application\Views\ListSelection.cs`, `Application\WordingOfOutcomes.cs`, `Application\Actions\RemoveWorktreeHandler.cs` lose a discard or default arm each; no public surface changes.
- `Tui`: `DialogEffects.cs` gains a registry and `public static bool Handles(Type)`; `Palette.cs` and `WorktreesScreenDefinition.cs` lose a discard arm each. `Show` and `IEffectContext` stay internal.
- Tests: `test\Source191.Vantage.Core.Tests\Tui\DialogEffectsShould.cs` is new (the test project already references the Tui project for `LayeringShould`). No existing test changes.
- `src\Source191.Vantage\Source191.Vantage.csproj`: `Version` 0.4.2.
- CLAUDE.md: the code rules on completeness facts and the CLI facts section (the observed status and state pairings).
- Independent of the three open changes (`consolidate-helpers`, `value-objects`, `wizard-as-reducer`); `consolidate-helpers` moves `DisplayState.From`, so the observation lands wherever the mapping lives at the time.
