# Tasks

## 1. The rule as a value

- [ ] 1.1 `SessionNameShould.RefuseABlankNameAsMissing`, `RefuseASemicolonOrDoubleQuoteAsUnsafe`, `AcceptAnyOtherName` and `CheckBlankBeforeUnsafe`: implement `SessionName.Validate`, `SessionNameValidation` and `NameProblem`, and build `Parse` and `TryParse` on it, with no try and catch left in `SessionName`. Verify `SessionNameShould` passes.
- [ ] 1.2 `WordingShould.SayANameIsRequired` and `SayANameCannotHoldUnsafeCharacters`: implement `Wording.Describe(NameProblem)` with today's two sentences. `Footer.InvalidName` carries a `NameProblem`. Verify `WordingShould`, including the reflection fact, passes.

## 2. The callers

- [ ] 2.1 `SessionsViewShould`: the blank and unsafe rename cases assert `Footer.InvalidName(NameProblem.Missing)` and `NameProblem.UnsafeCharacters` (red first). `RenameWith` matches on `Validate`, and its `catch (ArgumentException)` and suffix split go. Verify the test passes.
- [ ] 2.2 `LaunchPlanShould` and `HereCommandShould`: the blank and semicolon cases assert the whole sentence with `ShouldBe` (red first, because of the " (Parameter 'text')" suffix). `LaunchPlanBuilder` matches on `Validate` and takes the sentence from `Wording`, and its `catch (ArgumentException)` goes. Verify both pass, and that `grep "catch (ArgumentException"` finds only the process registry's adapter.

## 3. Documentation and release

- [ ] 3.1 CLAUDE.md Code rules: user text never comes from an exception message, and typed text is checked with a `Validate` that returns a reason. Raise `Version` as a patch. Verify `openspec validate` passes for every open change, and the version check passes.

## Workflow follow-up

- One pull request: task groups 1 to 3, archived in the same pull request. It can go before or after `value-objects` PR 16. Both touch `SessionsView.cs`, so whichever merges second takes `main` in first.
- Manual check: start the wizard and confirm a blank name and a name with a semicolon show only the sentence. Do the same for `vantage here 'a;b'` and for `r` with a blank name.
