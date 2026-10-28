# Proposal

## Why

`SessionName` refuses a semicolon and a double quote, and the `wt-title-escaping` spike (28 October 2026, Windows Terminal 1.25.2733) showed only one of them has a cause: `wt.exe` splits its command line on every semicolon, even inside a quoted argument, and `\;` passes a literal one; a double quote reaches the title intact, and `claude --bg -n` keeps a semicolon in a name. So the rule can go if the terminal adapter escapes semicolons. The refusal sentence is also cut off in the wizard at 120 columns, which removing it settles.

## What Changes

- **The adapter escapes semicolons.** `WindowsTerminalArguments.ForTab` writes every `;` in the arguments it passes as `\;`, so the title and the starting directory reach `wt.exe` whole. A folder holding a semicolon, which opens a broken tab today, is fixed by the same step.
- **A session name may hold any character.** `NameProblem.UnsafeCharacters` and its sentence go; a name is refused only when it is blank. That applies in the wizard, `vantage here`, rename (`r`), and to names read from the CLI's listing, so a session someone started as `claude --bg -n 'a;b'` keeps its name through adopt, rename and resume.
- **The wizard's problem line fits.** With the long sentence gone, a fact checks that every sentence the wizard's one-line problem label can show fits its width on a 120-column terminal.
- **Manual checks the spike could not run**, by the product owner: what `wt.exe` does with a backslash directly before a semicolon, and, once the rule is relaxed, that `o` finds a tab titled with a semicolon and a double quote and that Vantage's own OSC 2 re-title after a hand-off keeps it.
- **Version:** a minor raise, because names that were refused are now accepted.

Out of scope: other characters in names (none is refused today besides these two), the rule for worktree slugs (unchanged, still letters and digits), and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: "A session name is mandatory" no longer refuses a semicolon or a double quote, and the tab is titled with the name exactly as typed.
- `session-attach`: "Rename without attaching" refuses only a blank new name.

## Impact

- **Core:** `Domain\SessionName.cs` (`NameProblem`, `ProblemWith`, doc comments), `Application\Wording.cs` (the UnsafeCharacters sentence), and the tests that use it (`SessionNameShould`, `LaunchPlanShould`, `HereCommandShould`, `SessionsViewShould`, `WordingShould`).
- **Infrastructure:** `Terminal\WindowsTerminal.cs` (`WindowsTerminalArguments`) and `WindowsTerminalArgumentsShould`.
- **Docs:** CLAUDE.md's Windows Terminal fact on semicolons; the session-launch and session-attach specs.
- **Release:** a minor version raise.
