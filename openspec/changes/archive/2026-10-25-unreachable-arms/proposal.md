# Proposal

## Why

`session-name-validation` introduced switches over two-case results, and C# has no closed class hierarchies, so each needs a discard arm. Three of those arms invent behaviour: `SessionsView.RenameWith` answers "unchanged", `LaunchPlanBuilder.Build` answers "the name is missing", and `WizardView.Started` answers "changed nothing", for a case that cannot occur. `SessionName.Parse` has a fourth that throws an `ArgumentException` with a made-up message. An unreachable arm should say it is unreachable. Separately, the wizard checks a blank name itself before the plan builder checks it again, so one rule has two checks and two sentences: the wizard's "Session name is required: a short, human-readable label for the session." and everyone else's "A session name is required."

## What Changes

- **The four discard arms throw `UnreachableException`** (System.Diagnostics), so a case that cannot occur fails loudly instead of being handled by guesswork.
- **The convention joins CLAUDE.md** beside the closed-enum rule: a switch over a two-case record hierarchy names both cases and its discard arm throws `UnreachableException`; an open hierarchy (`Footer`, `Outcome`, `LaunchProblem`, `WizardProblem`) keeps a delegating or fallback arm, which the reflection facts rely on to spot a missing case.
- **The wizard's own blank-name check goes.** `WizardProblem.NoName` and its sentence are deleted; a blank name reaches the plan builder and comes back as `Refused(InvalidName(Missing))`, so the wizard shows "A session name is required." and focuses the name field as it did. The product owner chose the shared sentence.
- **Version:** a patch bump, because a sentence the user sees changes.

Out of scope: `LaunchPlanResult`'s nullable pair, which `text-value-doors` turns into two cases, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The "Blank name" scenario of `session-launch` says the wizard explains that a name is required, which both sentences do; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Core:** `Domain\SessionName.cs`, `Application\Views\SessionsView.cs`, `Application\Launch\LaunchPlan.cs`, `Application\Views\WizardView.cs`, `Application\Views\WizardInput.cs` (`WizardProblem.NoName`), `Application\Wording.cs`.
- **Tests:** `WizardViewShould`, `WordingShould` (the wizard's blank-name fact goes; the `WizardProblems` reflection fact adjusts itself).
- **Docs:** CLAUDE.md; `Version`.
