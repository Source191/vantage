# Proposal

## Why

`SessionName.Parse` throws an `ArgumentException`, and two callers catch it and show its message to the user. So user-facing text depends on an exception message, and .NET appends " (Parameter 'text')" to that message whenever a parameter name is given.

- **The rename footer:** `SessionsView` strips that suffix by splitting on " (Parameter". This workaround is the sign of the smell.
- **The wizard and `vantage here`:** `LaunchPlanBuilder` shows the message unstripped, and both use it. A name with a semicolon in the wizard reads "A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely. (Parameter 'text')", and `vantage here` prints the same suffix. The tests only check that the text contains or starts with the expected words, so they miss it.

A survey of every other place an exception's message or a hand-built sentence reaches the user found one neighbour of the same smell: the launch path carries its refusals as prose. `LaunchPlanBuilder` writes two worktree sentences itself, `HereCommand` writes three argument sentences and its usage line, and `CommandLine` holds the claim-tab usage line. All of them sit outside `Wording`, which the code rules make the only home of user text. `wizard-as-reducer` kept the plan builder's string, wrapped in `WizardProblem.Refused(string)`, so that deferral is now due.

## What Changes

- **Validate before parsing:** `SessionName.Validate(text)` returns either the name or a reason it is refused, as a value: `NameProblem.Missing` or `NameProblem.UnsafeCharacters`. No exception is involved.
- **Launch refusals are facts:** `LaunchPlanResult` carries a `LaunchProblem` instead of a string. Its cases are `InvalidName(NameProblem)`, `WorktreeNeedsRepository` and `WorktreeNeedsLetters` from the plan builder, and `OptionNeedsValue(option)`, `UnknownOption(option)` and `UnexpectedArgument(argument)` from `vantage here`. `WizardProblem.Refused` carries a `LaunchProblem`, and `HereCommandRunner` prints its words.
- **Callers act on the reason:** the rename reducer, `LaunchPlanBuilder` and `SessionName.TryParse` all use `Validate`. `Footer.InvalidName` carries the reason, not a sentence.
- **`Wording` owns the sentences:** the name sentences, the worktree sentences, the `vantage here` argument sentences and both usage lines move there, byte-identical to today's words minus the suffix.
- **`Parse` is for trusted input only:** it stays, throws on a name that fails validation and guards blank text with `ArgumentException.ThrowIfNullOrWhiteSpace`. Its message never reaches the screen.
- **Exact sentences in tests:** every path asserts the fact or the full sentence, so a suffix cannot come back.
- **The rule is written down:** CLAUDE.md says user text never comes from an exception message, typed text is checked by a `Validate` or `TryParse` that returns a reason, and a failure's message is shown only as what failed.
- **Patch release:** a user-visible fix, so `Version` is raised as a patch.

Out of scope:
- **Failure reports:** `GitException` in `LaunchService`, the Claude CLI's messages in the handlers' outcomes, `LoadFailed` in the banner and the screen's catch-all `Footer.Failed`. They say what another tool or the file system refused. No check beforehand can replace them without a second git call and a race.
- **Domain guards:** `FolderPath`, `JobId`, `BranchName` and `SiblingWorktree.For` throw on bad input, but each has a `TryParse` for text from outside, and no code feeds them typed text.
- **Program.cs messages:** "Claude Code was not found" and the like are wiring, printed before any screen exists.
- **A general result type** and any rewrite of the command-line parser.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: "A session name is mandatory" explains a refused name in a plain sentence, in the wizard and in `vantage here`.
- `session-attach`: "Rename without attaching" refuses a name the launch rules would refuse, with the same sentence.

## Impact

- **Core:**
  - `Domain\SessionName.cs` gets `Validate`, `SessionNameValidation` and `NameProblem`.
  - `Application\Launch\LaunchPlan.cs` gets `LaunchProblem`; `LaunchPlanResult` carries it.
  - `Application\Launch\HereCommand.cs` and `HereCommandRunner.cs` return and print facts.
  - `Application\CommandLine.cs` takes its usage line from `Wording`.
  - `Application\Views\Footer.cs`: `InvalidName` carries a `NameProblem`.
  - `Application\Views\WizardInput.cs`: `WizardProblem.Refused` carries a `LaunchProblem`.
  - `Application\Wording.cs` holds every sentence above.
  - `Application\Views\SessionsView.cs` and `Application\Launch\LaunchPlan.cs` lose their `catch (ArgumentException)`.
- **Tests:**
  - `SessionNameShould` covers `Validate`.
  - `WordingShould` gets one verbatim fact per sentence and a reflection fact for `LaunchProblem`.
  - `SessionsViewShould`, `LaunchPlanShould`, `HereCommandShould`, `HereCommandRunnerShould`, `CommandLineShould` and `WizardViewShould` assert facts or whole sentences.
- **Docs:** CLAUDE.md's code rules.
- **Release:** `Version` gets a patch bump.
