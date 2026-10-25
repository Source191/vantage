# Tasks

## 1. The rule as a value

- [x] 1.1 `SessionNameShould.RefuseABlankNameAsMissing`, `RefuseASemicolonOrDoubleQuoteAsUnsafe`, `AcceptAnyOtherName` and `CheckBlankBeforeUnsafe`: implement `SessionName.Validate`, `SessionNameValidation` and `NameProblem`, and build `Parse` and `TryParse` on it, with no try and catch left in `SessionName`. Verify `SessionNameShould` passes.
- [x] 1.2 `WordingShould.SayANameIsRequired` and `SayANameCannotHoldUnsafeCharacters`: implement `Wording.Describe(NameProblem)` with today's two sentences. `Footer.InvalidName` carries a `NameProblem`. Verify `WordingShould`, including the reflection fact, passes.

## 2. The callers

- [x] 2.1 `SessionsViewShould`: the blank and unsafe rename cases assert `Footer.InvalidName(NameProblem.Missing)` and `NameProblem.UnsafeCharacters` (red first). `RenameWith` matches on `Validate`, and its `catch (ArgumentException)` and suffix split go. Verify the test passes.
- [x] 2.2 `LaunchPlanShould`: the blank, semicolon, no-repository and no-letters cases assert `LaunchProblem` values (red first). Implement `LaunchProblem`; `LaunchPlanBuilder` matches on `Validate`, its `catch (ArgumentException)` goes, and `LaunchPlanResult.Problem` is a `LaunchProblem`. `WizardProblem.Refused` carries a `LaunchProblem` and the "That cannot be launched." fallback goes; `WizardViewShould` asserts the fact. Verify both pass.
- [x] 2.3 `HereCommandShould` and `HereCommandRunnerShould`: the argument cases assert `OptionNeedsValue`, `UnknownOption`, `UnexpectedArgument` and `InvalidName`, and the runner prints exactly the sentence (red first, because of the " (Parameter 'text')" suffix). `HereCommand` returns facts and `HereCommandRunner` prints `Wording.Describe`. Verify both pass.
- [x] 2.4 `WordingShould`: one verbatim fact per `LaunchProblem` case, the usage lines, and `HaveWordsForEveryLaunchProblem`. Implement `Wording.Describe(LaunchProblem)`, `Wording.HereUsage` and `Wording.ClaimTabUsage`; `HereCommand.Usage` goes and `CommandLine` uses `Wording.ClaimTabUsage`. Verify `WordingShould` and `CommandLineShould` pass, and that `grep "catch (ArgumentException"` finds only the process registry's adapter.

## 3. Documentation and release

- [x] 3.1 CLAUDE.md code rules: user text never comes from an exception message, typed text is checked by a `Validate` or `TryParse` that returns a reason, and a failure's message is shown only as what failed. Raise `Version` as a patch, to the next one after `main`'s. Run the Release build's `vantage here "a;b"` and `vantage here ""` from a repository folder (both refuse before anything starts) and check each prints only its sentence. Verify `openspec validate` passes and the version check passes.

## Workflow follow-up

- One pull request: task groups 1 to 3, archived in the same pull request.
- Manual check: in the wizard, a name with a semicolon shows only the sentence, and a blank name shows the wizard's own sentence. `r` with a blank name shows "A session name is required." in the footer.
- Notes (version 0.4.10): the Release build's `vantage here "a;b"`, `vantage here ""` and `vantage here --bogus` each print only their sentence and exit 2; the build from `main` printed "... cannot pass them safely. (Parameter 'text')". `LaunchPlanResult` keeps its shape with a typed problem and gains a `Refused(problem)` factory. `Parse` keeps a discard arm over `SessionNameValidation`, a class hierarchy. The survey found no other exception message used as user text; the failure reports and domain guards stay as the proposal says.
