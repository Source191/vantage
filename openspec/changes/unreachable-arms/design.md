# Design

## Context

See proposal.md, Why. The repository has two kinds of record hierarchy. Open ones grow a case per feature (`Footer`, `Outcome`, `LaunchProblem`, `WizardProblem`, `WizardInput`); their switches end in an arm that delegates to another describer or falls back to `ToString()`, and `WordingShould`'s reflection facts use that fallback to fail when a case has no words. Two-case ones model a result (`SessionNameValidation`, and `LaunchPlanResult` once `text-value-doors` lands); their switches have no honest third arm. The repository has no `UnreachableException` yet.

## Goals / Non-Goals

**Goals:**
- No discard arm invents behaviour.
- One sentence for a blank name, checked in one place.

**Non-Goals:**
- Replacing the two-case hierarchies with something the compiler can exhaust; C# has no closed hierarchies.
- Touching the open hierarchies' fallback arms.

## Decisions

- **`UnreachableException`, not `InvalidOperationException` or a custom type.** It is the BCL's purpose-built type for exactly this, needs no message, and states intent to a reader. Alternative rejected: `is Valid valid ? … : ((Refused)validation).Problem`, a cast that is a second unreachable construct and reads worse than a named arm.
- **No shared helper.** The arm is one line; a helper would hide the one word that matters.
- **The rule names the two kinds.** Two-case hierarchies get the throwing arm; open hierarchies keep delegating or falling back, because the reflection facts depend on the fallback. The CLAUDE.md sentence says both so nobody "fixes" `Footer`'s arm.
- **The blank check moves out of the wizard.** It predates the launch facts: the dialog once needed to refuse before building a plan to know which field to focus. `WizardView.Started` now focuses the name for every `Refused`, so the pre-check adds only a second sentence. The shared sentence is the product owner's choice; the caption already says "(required)" and the preview explains what the name becomes.

## Risks / Trade-offs

- [An analyzer objects to `throw` in a switch arm] → Sonar and the IDE rules already accept throw expressions in this repository (`Parse` has one); the build is the check.
- [Focus after a blank refusal changes] → `WizardViewShould` asserts the name field is focused for `Refused(InvalidName(Missing))` before the pre-check is removed.
