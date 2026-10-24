# Proposal

## Why

`SessionName.Parse` throws an `ArgumentException`, and two callers catch it and show its message to the user. So user-facing text depends on an exception message, and .NET appends " (Parameter 'text')" to that message whenever a parameter name is given.

- **The rename footer:** `SessionsView` strips that suffix by splitting on " (Parameter". This workaround is the sign of the smell.
- **The wizard and `vantage here`:** `LaunchPlanBuilder` shows the message unstripped, and both use it. A blank name in the wizard should therefore read "A session name is required. (Parameter 'text')", and `vantage here` prints the same suffix after the semicolon refusal. The tests only check that the text contains or starts with the expected words, so they miss it.

## What Changes

- **Validate before parsing:** `SessionName.Validate(text)` returns either the name or a reason it is refused, as a value: `NameProblem.Missing` or `NameProblem.UnsafeCharacters`. No exception is involved.
- **Callers act on the reason:** the rename reducer, `LaunchPlanBuilder` (and so the wizard and `vantage here`) and `SessionName.TryParse` all use it. `Footer.InvalidName` carries the reason, not a sentence.
- **`Wording` owns the sentences:** "A session name is required." and "A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely." are today's words, minus the suffix.
- **`Parse` is for trusted input only:** it stays, throws on a name that fails validation and guards blank text with `ArgumentException.ThrowIfNullOrWhiteSpace`. Its message never reaches the screen.
- **Exact sentences in tests:** every path asserts the full sentence, so a suffix cannot come back.
- **Patch release:** a user-visible fix, so `Version` is raised as a patch.

Out of scope:
- **The wizard's other messages:** for example "A worktree needs a repository...". They are the wizard's own text, which `wizard-as-reducer` rewrites.
- **Other exceptions shown to the user:** load failures in the banner and `Footer.Failed`. Their messages come from the CLI, git or the file system, and say what failed.
- **Multiple Claude accounts:** a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: "A session name is mandatory" explains a refused name in a plain sentence, in the wizard and in `vantage here`.
- `session-attach`: "Rename without attaching" refuses a name the wizard would refuse, with the same sentence.

## Impact

- **Core:**
  - `Domain\SessionName.cs` gets `Validate` and `NameProblem`.
  - `Application\Views\Footer.cs`: `InvalidName` carries a `NameProblem`.
  - `Application\Wording.cs` holds the two sentences.
  - `Application\Views\SessionsView.cs` and `Application\Launch\LaunchPlan.cs` lose their `catch (ArgumentException)`.
- **Tests:**
  - `SessionNameShould` covers `Validate`.
  - `WordingShould` gets the two sentences.
  - `SessionsViewShould`, `LaunchPlanShould` and `HereCommandShould` assert the full sentence.
- **Release:** `Version` gets a patch bump.
- **Order:** after `value-objects` PR 15 (#27), which adds the `SessionName` guard comment this change replaces.
