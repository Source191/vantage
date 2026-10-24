# Design

## Context

See proposal.md, Why. `SessionName.Parse` is the only rule for a session name. Two callers turn its exception into user text:
- **`SessionsView.RenameWith`:** it strips the suffix.
- **`LaunchPlanBuilder.Build`:** it doesn't, and both the wizard and `HereCommand.Plan` go through it.

`SessionName.TryParse` already catches the exception to return null. `outcomes-and-wording` put every other sentence in `Wording`, and `Footer.InvalidName(string Reason)` is the one footer case that still carries prose.

## Goals / Non-Goals

**Goals:**
- No user-facing text comes from an exception.
- Name refusals are a value that `Wording` describes.
- The words are byte-identical to today's, minus the suffix.

**Non-Goals:**
- A general validation framework or result type.
- Rewording anything.
- The wizard's other problem sentences.

## Decisions

- **`SessionName.Validate(string text)` returns `SessionNameValidation`.** It is either `Valid(SessionName Name)` or `Refused(NameProblem Problem)`, a two-case record hierarchy matched with a type pattern, like `Footer`. `NameProblem` is an enum (`Missing`, `UnsafeCharacters`): it carries no data. `Wording` switches over it naming both members, with no discard arm, as `guard-every-switch` requires, so a third problem fails the build with CS8509 until it has words. The rules move from `Parse` into `Validate`, in today's order: blank first, then semicolon or double quote.
  - Rejected, `bool TryParse(string, out SessionName?, out NameProblem?)`: two out parameters read worse in a reducer than one pattern match.
  - Rejected, a `Result<T, E>` type: it is a general abstraction with one user.
- **`Parse` and `TryParse` are built on `Validate`.**
  - `TryParse` returns the name or null, with no try and catch.
  - `Parse` uses `ArgumentException.ThrowIfNullOrWhiteSpace` for blank text, then throws `ArgumentException` naming the problem for unsafe characters. It is used only where the text is already known to be valid, such as fixtures and the slug path. The comment #27 added to `Parse` goes.
- **`Footer.InvalidName(NameProblem Problem)`.** `Wording.Describe(NameProblem)` gives the sentence. `Wording.Describe(Footer)` uses it for `InvalidName`, and the `WordingShould` reflection fact keeps covering the case. Its placeholder learns `NameProblem`.
- **`LaunchPlanBuilder` puts the sentence into `LaunchPlanResult.Problem`.** It does so by asking `Wording` for the refusal, so the wizard and `vantage here` print exactly the sentence. `Problem` stays a string, because the wizard's other problems are sentences too, until `wizard-as-reducer` replaces them.
- **Tests compare whole sentences.** `LaunchPlanShould` and `HereCommandShould` change from `ShouldContain` and `ShouldStartWith` to `ShouldBe` for the two name cases. `SessionsViewShould` asserts `Footer.InvalidName(NameProblem.Missing)`. `WordingShould` holds each sentence.

## Risks / Trade-offs

- **[A third caller of `Parse` appears and shows an exception message again]** → the CLAUDE.md Code rules say user text never comes from an exception, and that `Validate` is the way to check typed text. A reviewer can grep for `catch (ArgumentException`.
- **[The suffix was the only visible symptom, and no test caught it]** → the new `ShouldBe` assertions fail on any extra text. That is the regression guard.
