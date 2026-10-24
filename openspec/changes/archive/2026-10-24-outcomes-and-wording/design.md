# Design

## Context

See proposal.md. Counting today's code, the reducers hold about twenty footer sentences, the handlers about twenty-five, and the screens about fifteen including dialog titles and questions. Several differ only in punctuation.

## Goals / Non-Goals

**Goals:** one file owns every sentence; handlers and reducers are tested on facts; the sentences shown are byte-identical to today's.

**Non-Goals:** resource files or localisation, rewording anything, moving the wizard dialog's messages (that dialog is rewritten in `wizard-as-reducer`).

## Decisions

- **Outcomes are records of facts.** For example `CloseEmptyOutcome(IReadOnlyList<string> Closed, IReadOnlyList<string> Ended, IReadOnlyList<SkippedSession> Skipped)` replaces `Summary(...)`; `RenameOutcome(string OldName, string NewName, RenameResult Result, string? Detail)` with `Result` in `Renamed`, `FailedAndRestored`, `FailedAndUnsupervised`. The `BranchToConfirm` pattern already in `DeleteOutcome` is kept and reused by `RemoveOutcome`.
- **Footers are a sealed record hierarchy, not an enum,** because several carry a name or a count. `Footer.None` is the empty footer. `SessionsViewState.Footer` and `WorktreesViewState.Footer` change type; `FooterText` on the worktrees state (which falls back to the selected row's health reason) becomes `Footer.HealthOf(row)` plus the standing hint value.
- **`Wording` is one static class with one `Describe` overload per outcome and footer type,** plus the dialog texts (`DeleteSessionQuestion(name)`, `CloseEmptyQuestion(rows)`, `DeleteBranchQuestion(branch, createdByVantage)`, `ForceRemoveWarning`, help texts). A switch expression over the sealed hierarchy keeps it exhaustive. Alternative rejected: `ToString` overrides on the outcome records; that hides presentation in Core types and spreads the sentences across twenty files again.
- **Seeding the test file.** Before any code moves, `WordingShould` is written with every current sentence copied verbatim from the reducers, handlers, screens and their existing tests, one `[Fact]` per sentence with the inputs that produce it. The existing tests are then rewritten to assert on outcomes; the sentences they lose are already in `WordingShould`. The hand-off check and the README key tables are the manual acceptance.
- **`TableScreen` calls `Wording` in two places only:** when a handler's outcome arrives (footer text) and when a dialog effect needs its title and question. The reducers' `Footer` value is rendered in `Redraw`.

## Risks / Trade-offs

- [A sentence slips through with a changed space or full stop] → the seed test file is built from the current strings before anything moves, and `WordingShould` is reviewed against the README's wording.
- [`Wording` becomes a thousand-line file] → sixty sentences at a few lines each is about three hundred lines; if it grows, split by view (`SessionsWording`, `WorktreesWording`) behind the same entry point.
- [Outcome records multiply the types in Core] → each sits in the same file as its handler, and the compiler's exhaustiveness check on the `Wording` switch is the payback.
