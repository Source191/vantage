# Design

## Context

A session name reaches three places. As the `-n` argument to `claude --bg`, and for rename, adopt and resume as the name given to `claude --bg --resume`, both through `ProcessStartInfo.ArgumentList`, so no shell sees it. As the `--title` argument to `wt.exe new-tab`, where Windows Terminal re-splits its own command line on every semicolon, quoted or not; the tab's script already travels Base64-encoded for that reason, and `--startingDirectory` (a folder path, which Windows allows to hold a semicolon) is exposed the same way. And as the exact text `UiAutomationTabs.Select` matches against tab titles for `o`, and Vantage's OSC 2 re-title after every hand-off writes. The spike (`wt-title-escaping`) verified the split, the `\;` escape, and that a double quote passes intact; it could not try a backslash before a semicolon, `o`, or OSC 2, because the rule kept such names out.

## Goals / Non-Goals

**Goals:**
- A session name may hold a semicolon or a double quote everywhere a name is typed or read: the wizard, `vantage here`, rename, and the CLI's listing.
- Every argument Vantage hands `wt.exe` arrives whole, whatever it holds.
- No wizard problem sentence is cut off on a 120-column terminal.

**Non-Goals:**
- Refusing any other character, or changing how a worktree slug is made from a name.
- Making the wizard's problem line wrap; it stays one line, and the sentences are kept short enough for it.

## Decisions

- **Escape in the adapter, every argument.** `WindowsTerminalArguments.ForTab` passes each argument through one escape, `;` to `\;`, so the title and the starting directory are covered by the same line and no caller needs to know about it. Base64, the colour and the fixed switches hold no semicolon, so escaping them changes nothing. Alternative rejected: escaping only the title, which would leave a folder holding a semicolon opening a broken tab.
- **The backslash question is answered before the code.** A name ending in a backslash right before a semicolon (`a\;b`) becomes `a\\;b` once escaped; whether `wt.exe` reads that as a literal `\;` is unverified, so the first task is a manual check. If it fails, implementation pauses and this design is updated (refusing a backslash directly before a semicolon would be a new `NameProblem` and a spec change), rather than guessing.
- **`NameProblem` keeps one member.** `Missing` stays, `UnsafeCharacters` goes, so `SessionNameValidation`, `Footer.InvalidName`, `LaunchProblem.InvalidName` and the `Wording` switch keep their shape; CS8509 points at every switch that names the removed member. `ProblemWith` checks blank only, so `From`, `Validate` and `TryParse` relax together and a listed name with a semicolon now survives adopt, rename and resume.
- **The problem line's width is a fact, not a layout change.** The wizard dialog is 90 % of the terminal; at 120 columns its problem label is about 104 characters wide. A `WordingShould` fact checks every sentence a `WizardProblem` or `LaunchProblem` can produce is at most 100 characters; the longest left is 73.
- **The rest is checked at the terminal.** `o` on a tab titled with `;` and `"`, the OSC 2 re-title after a hand-off, and the tab count for a name with a semicolon, by the product owner, recorded in tasks.md and, once verified, in CLAUDE.md's Windows Terminal facts.

## Risks / Trade-offs

- [`wt.exe` mishandles `\\;`] → the first task finds out before any code; the fallback is a narrow refusal, decided with the product owner.
- [Windows Terminal's OSC 2 parser stops at a semicolon in the title] → the tab would show a shortened title after a hand-off and `o` would no longer match it; the manual check finds it, and the title could then be written without its semicolons only in the escape sequence.
- [A future Windows Terminal stops splitting on semicolons] → `\;` would then show as a literal backslash in titles; the nightly contract run does not cover `wt.exe`, so the CLAUDE.md fact carries the version it was checked against.
