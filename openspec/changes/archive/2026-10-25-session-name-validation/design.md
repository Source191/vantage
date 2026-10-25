# Design

## Context

See proposal.md, Why. `SessionName.Parse` is the only rule for a session name. Two callers turn its exception into user text:
- **`SessionsView.RenameWith`:** it strips the suffix.
- **`LaunchPlanBuilder.Build`:** it doesn't, and both the wizard (through `WizardView`, as `WizardProblem.Refused(string)`) and `HereCommand.Plan` go through it.

`SessionName.TryParse` already catches the exception to return null. `outcomes-and-wording` put the screens' sentences in `Wording`, and `Footer.InvalidName(string Reason)` is the one footer case that still carries prose. `LaunchPlanResult.Problem` is the other string that carries prose: the plan builder's two worktree sentences and `HereCommand`'s argument sentences travel in it.

## Goals / Non-Goals

**Goals:**
- No user-facing text comes from an exception.
- Every refusal on the launch path is a value that `Wording` describes.
- The words are byte-identical to today's, minus the suffix.

**Non-Goals:**
- A general validation framework or result type.
- Rewording anything. The wizard keeps its own blank-name sentence ("Session name is required: ..."), which it gives before the plan builder runs, so "A session name is required." is heard from rename and `vantage here ""`.
- Failure reports from git, the Claude CLI or the file system.

## Decisions

- **`SessionName.Validate(string text)` returns `SessionNameValidation`.** It is either `Valid(SessionName Name)` or `Refused(NameProblem Problem)`, a two-case record hierarchy matched with a type pattern, like `Footer`. `NameProblem` is an enum (`Missing`, `UnsafeCharacters`): it carries no data. `Wording.Describe(NameProblem)` switches over it naming both members, with no discard arm, so a third problem fails the build with CS8509 until it has words. The rules move from `Parse` into `Validate`, in today's order: blank first, then semicolon or double quote.
  - Rejected, `bool TryParse(string, out SessionName?, out NameProblem?)`: two out parameters read worse in a reducer than one pattern match.
  - Rejected, a `Result<T, E>` type: it is a general abstraction with one user.
- **`Parse` and `TryParse` are built on `Validate`.**
  - `TryParse` returns the name or null, with no try and catch.
  - `Parse` uses `ArgumentException.ThrowIfNullOrWhiteSpace` for blank text, then throws `ArgumentException` naming the problem for unsafe characters. It is used only where the text is already known to be valid, such as fixtures. The comment about the footer goes.
- **`LaunchProblem` is a record hierarchy in `Application\Launch`.** Cases: `InvalidName(NameProblem Problem)`, `WorktreeNeedsRepository`, `WorktreeNeedsLetters`, `OptionNeedsValue(string Option)`, `UnknownOption(string Option)` and `UnexpectedArgument(string Argument)`. `LaunchPlanResult(LaunchPlan? Plan, LaunchProblem? Problem)` keeps its shape with a typed problem, so its callers change only what they read. `Wording.Describe(LaunchProblem)` has a discard arm, as class hierarchies do here, and a reflection fact in `WordingShould` fails when a case has no words. The `vantage here` sentences keep the usage line after them, as today.
  - Rejected, a two-case `LaunchPlanResult` like `SessionNameValidation`: every caller already reads `Plan is null`, and the result has no third state to model.
- **`WizardProblem.Refused(LaunchProblem Problem)`.** The wizard's fallback "That cannot be launched." goes: a plan builder that returns no plan always returns a problem now, so no path could show it.
- **`Footer.InvalidName(NameProblem Problem)`.** `Wording.Describe(Footer)` delegates to `Describe(NameProblem)`; the reflection fact keeps covering the case, and its placeholder learns `NameProblem`.
- **Usage lines move to `Wording`.** `Wording.HereUsage` and `Wording.ClaimTabUsage`; `CommandLine` builds its `Usage` record from `Wording.ClaimTabUsage`. `HereCommand.Usage` goes.
- **`Describe` overloads sit together.** S4136 requires every `Describe` overload in `Wording.cs` to be adjacent.
- **Tests compare facts or whole sentences.** `LaunchPlanShould` and `HereCommandShould` assert the `LaunchProblem` value. `HereCommandRunnerShould` asserts the printed line. `SessionsViewShould` asserts `Footer.InvalidName(NameProblem.Missing)`. `WordingShould` holds each sentence verbatim.

## Risks / Trade-offs

- **[A new caller shows an exception message again]** → the CLAUDE.md code rules say user text never comes from an exception, and that `Validate` or `TryParse` is the way to check typed text. A reviewer can grep for `catch (ArgumentException`, which then finds only the process registry's adapter.
- **[The suffix was the only visible symptom, and no test caught it]** → the new exact assertions fail on any extra text. That is the regression guard.
- **[The wider scope touches `vantage here`'s output]** → the sentences are byte-identical and `HereCommandShould` already pins two of them verbatim; the change only moves where they are written.
