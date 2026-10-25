# Tasks

## 1. One blank-name check

- [x] 1.1 `WizardViewShould.StartRefusesNoRepositoryBlankNameAndPlanProblemsWithTheRightFocus`: a blank name answers `WizardProblem.Refused(new LaunchProblem.InvalidName(NameProblem.Missing))` with focus on the name (red first). Remove the blank check from `WizardView.Started`, `WizardProblem.NoName` and its `Wording` arm, and `WordingShould.AskForASessionNameInTheWizard`. Verify the Core suite passes, including the `WizardProblems` reflection fact.

## 2. Unreachable arms

- [x] 2.1 Replace the discard arms in `SessionName.Parse`, `SessionsView.RenameWith`, `LaunchPlanBuilder.Build` and `WizardView.Started` with `throw new UnreachableException()`. Verify `dotnet build` (warnings as errors) and the Core suite pass, and `grep -n "_ =>" ` on those four files shows no other discard arm.

## 3. Documentation and release

- [x] 3.1 CLAUDE.md code rules: the two-case sentence beside the closed-enum rule, naming the open hierarchies that keep their fallback; raise `Version` by a patch. Verify `openspec validate unreachable-arms` and `.github\scripts\Assert-VersionBumped.ps1` pass.

## Workflow follow-up

- P2 in `public-release`'s sequence: the refactors stream, branched from `main` after P1.
- One pull request, archived in it. After `remove-session-descriptions` and before `text-value-doors`; both neighbours touch `WizardView`, so whichever merges second takes `main` first.
- Manual check: F5 in the wizard with a blank name shows "A session name is required." and leaves the cursor in the name field.
