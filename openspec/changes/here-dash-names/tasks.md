# Tasks

## 1. The refusal

- [x] 1.1 `WordingShould.SayANameLooksLikeAnOption`: add `Wording.NameLooksLikeAnOption(string token)` ("--promt looks like an option, not a session name. To use it as the name, put -- before it: vantage here -- --promt"); verify the fact passes
- [ ] 1.2 `CommandLineShould.RefuseANameThatLooksLikeAnOption` (`here --promt`, `here -x`: exit 1, the sentence on the error writer, `--prompt` in the help on the output writer, no action called), with `here -- -experiment` and `here fix-tests -p "-fix the tests"` still reaching the action: check the name's token against `--` in the `here` action and refuse with `HelpAction`; verify `CommandLineShould` passes

## 2. Documentation

- [ ] 2.1 README's `vantage here` paragraph: the refusal and the `--` escape instead of the trap; verify by hand with the Debug build that `vantage here --promt` exits 1 with the sentence and `vantage here --help` exits 0
- [ ] 2.2 Run `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate here-dash-names --strict`; verify all pass

## Workflow follow-up

- Archive the change in this pull request (PR #62).
