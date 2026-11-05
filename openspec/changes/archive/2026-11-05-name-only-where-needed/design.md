# Design

## Context

See proposal.md. The identifiers are spread over about 60 files and share their spelling with text in string literals ("torni.exe", "torni here"), so a text replacement cannot tell them apart.

## Decisions

- **Identifiers renamed by syntax token.** A throwaway Roslyn program walks every `.cs` file outside the archive and renames identifier tokens only, so string literals are untouched while code inside an interpolation hole is renamed. A bare `Torni` token after `Source191.` is the namespace and is skipped; after `TabOwner.` it becomes `Console`, after any other `.` it is the `Started` property and becomes `Tool`; elsewhere the file decides (the `TabOwner` field, the converter's constant, the tests' executable path constants, the architecture tests' assembly lists). The build and the whole suite, unchanged, are the check.
- **Sentences by reading.** Each on-screen sentence, comment and changelog line that names the program is read and kept or reworded by the docs rule: keep the name in titles, labels, commands, paths, profile and variable names, and wherever another program is in the same sentence; otherwise "it", "the console" or a rewording. A test that asserts a reworded sentence changes with it in the same commit, the test first.
- **The own-tab word is `console`.** It is a tab file value only Torni reads, so a neutral word survives renames; `TabOwnerJsonConverterShould`'s facts change first.
- **The label renamed on GitHub as well as in the form**, so the problem reports already filed keep it.

## Risks / Trade-offs

- [A reworded sentence the smoke check waits for] → `SmokeScript` takes its markers from `Wording`, so it follows; CI runs it.
- [Renaming the label before merging] → Until the merge, the form on `main` asks for a label that no longer exists, so a report filed in that window has no label; the owner can add it by hand.
