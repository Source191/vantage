# Tasks

## 1. The backslash check at the terminal

- [x] 1.1 The product owner, from a PowerShell tab outside Claude Code, runs `wt.exe -w 0 new-tab --title 'a\\;b' pwsh.exe -NoExit` (what the adapter will send for a name `a\;b`), `wt.exe -w 0 new-tab --title 'a\b' pwsh.exe -NoExit` and, after `New-Item -ItemType Directory "$env:USERPROFILE\x;y"`, `wt.exe -w 0 new-tab --startingDirectory "$env:USERPROFILE\x\;y" pwsh.exe -NoExit`. Verify by the tabs: one tab each, titled `a\;b` and `a\b`, the third opening in `x;y`; remove the folder afterwards; record the outcomes here. If the first opens two tabs, stop and update design.md before task 2.
  Outcome (28 October 2026, Windows Terminal 1.25.2733): `'a\\;b'` opened one tab titled `a\;b`; `'a\b'` one tab titled `a\b`; the `--startingDirectory` tab opened in `x;y` (titled `pwsh.exe`, as no title was given). So `wt.exe` reads `\;` as a literal semicolon and leaves every other backslash alone, and replacing each `;` with `\;` is the whole escape.

## 2. The name rule (Domain)

- [ ] 2.1 Red: in `SessionNameShould`, `AcceptAnyOtherName` takes `fix; rm -rf` and `say "hello"` as valid, `ReadARefusedNameAsNothing` and `RefuseToBuildANameValidateRefuses` keep only blank cases, and `RefuseASemicolonOrDoubleQuoteAsUnsafe` and `CheckBlankBeforeUnsafe` go. Green: `ProblemWith` checks blank only and `NameProblem.UnsafeCharacters` is removed; the build's CS8509 and CS0117 errors name every place that used it. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build --filter-class *SessionNameShould` passes.
- [ ] 2.2 Bring the callers in line: `LaunchPlanShould.ReportAnInvalidName` keeps only the blank case and a new case launches `fix a;b`; `HereCommandShould` plans `a;b` as a launch; `SessionsViewShould`'s rename case `fix; rm -rf` now yields `RenameSession`; `WordingShould` loses `SayANameCannotHoldUnsafeCharacters` and its reflection sample uses `NameProblem.Missing`; `Wording.Describe(NameProblem)` names only `Missing`. Refactor: `SessionName`'s doc comments say a name is refused only when blank. Verify the Core suite passes.
- [ ] 2.3 Red: a `WordingShould` fact that every sentence `Wording.Describe` gives for each `WizardProblem` and `LaunchProblem` case is at most 100 characters, the wizard's problem line at 120 columns. Green: it passes with the long sentence gone (the longest left is 73). Verify the fact fails when a 101-character sentence is put in temporarily, then passes.

## 3. The terminal adapter

- [ ] 3.1 Red: in `WindowsTerminalArgumentsShould`, a launch titled `fix a;b` with starting directory `C:\repos\Northwind;Orders` gives the arguments `fix a\;b` and `C:\repos\Northwind\;Orders`, and the encoded command still decodes to the script. Green: `WindowsTerminalArguments.ForTab` passes every argument through one escape of `;` to `\;`; its comment says why, and points at the CLAUDE.md fact. Verify `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` passes.
- [ ] 3.2 Update CLAUDE.md's semicolon fact: Vantage escapes `;` in every `wt.exe` argument, so `SessionName` refuses only a blank name, with 1.1's backslash outcome. Raise `Version` by a minor step above `main`'s. Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` all pass.

## 4. At the terminal, after the code

- [ ] 4.1 The product owner installs the build (`tools\install-local.ps1`), starts a session named `fix a;b` from the wizard with "Start and open in tab" (F6), and one named `say "hi"`. Verify each opens exactly one tab with that title; record here.
- [ ] 4.2 From another Vantage, select each of those sessions and press `o`. Verify the existing tab is focused and the footer says Vantage switched to it; record here.
- [ ] 4.3 Attach to `fix a;b` with Enter and detach. Verify the Vantage tab's title is `Vantage` again and the session tab still reads `fix a;b`; then rename an idle session to `fix c;d` with `r` and verify the row shows that name on the next refresh. Record here and add the verified OSC 2 and `o` outcome to CLAUDE.md's Windows Terminal facts.
- [ ] 4.4 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal`. Verify exit 0.

## Workflow follow-up

- Archive in the pull request that completes it, after `wt-title-escaping` is merged.
