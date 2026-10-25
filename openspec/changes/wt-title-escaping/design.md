# Design

## Context

See proposal.md, Why. A session name travels three ways. As the `-n` argument to `claude --bg`, through `ProcessStartInfo.ArgumentList`, so no shell sees it. As the `--title` argument to `wt.exe new-tab`, also through the argument list, but Windows Terminal re-splits its own command line on semicolons, which is why the tab's script travels Base64-encoded and why the name is the only user text left in that command line. And as the exact string `UiAutomationTabs.Select` matches against tab titles for `o`. Microsoft's documentation shows shells escaping a semicolon so that `wt` receives it as a delimiter; it does not say how to pass a literal semicolon inside an argument, so only the terminal can answer.

## Goals / Non-Goals

**Goals:**
- One verified reason for the rule, written where the rule lives.
- Enough evidence to decide whether the rule can be relaxed.

**Non-Goals:**
- Relaxing the rule in this change.
- Automating the spike; the harness drives a pseudo-console and cannot see tabs.

## Decisions

- **The spike is manual and the product owner runs it.** It opens tabs and starts a throwaway session, so it runs from a terminal outside Claude Code with the Claude environment variables cleared. The exact command lines are in tasks.md so the run is repeatable.
- **The matrix is six titles and one name.** Titles `a;b`, `a\;b`, `fix a;b`, `fix a\;b`, `say "hi"` and `say \"hi\"` through `wt.exe -w 0 new-tab --title <title> pwsh.exe -NoExit`; the observation is the title shown and whether a second command ran. The two with a space matter most: `ProcessStartInfo.ArgumentList` passes `a;b` bare but wraps a title with a space in double quotes, real session names have spaces, and whether Windows Terminal splits on a semicolon inside a quoted argument is the question the adapter needs answered. The name `a;b` through `claude --bg -n`, observed in `claude agents --json`, then stopped and removed.
- **UI Automation is tried only if a title can carry the characters.** `o` matches the exact title string, so a title that Windows Terminal rewrites would break switching even if the tab opened.
- **Findings are recorded either way.** The rule's reason is stated once, on `SessionName`, and the two other comments agree with it. Alternative rejected: trying the escape in the adapter first and seeing what breaks; the rule is user-visible, and a wrong guess ships a tab that runs a second command.
- **Relaxation is a separate change** because it changes a requirement and removes a sentence, which this spike's `skip_specs` cannot carry.

## Risks / Trade-offs

- [Windows Terminal behaves differently in another version] → the fact is recorded with the version, as the existing facts are (1.25).
- [The semicolon can be escaped but the quote cannot, or the CLI refuses one of them] → the relax change can shrink the rule to the character that is still unsafe, with its own sentence.
