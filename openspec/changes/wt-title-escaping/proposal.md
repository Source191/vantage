# Proposal

## Why

`SessionName` refuses a semicolon and a double quote, and the code gives two different reasons. The terminal adapter's comment and the sentence the user sees blame Windows Terminal, which splits its command line on semicolons and takes the tab title as an argument. The summary on `SessionName.TryParse` blames the Claude CLI. Neither has been verified at the terminal. If Windows Terminal can take an escaped semicolon and quote in a title, and the CLI accepts them in a name, the rule may not be needed at all; if not, the one true reason belongs on the type.

## What Changes

- **A spike at the terminal, run by the product owner** because it opens real tabs: titles holding `;` and `"` go to `wt.exe new-tab --title` as the adapter sends them and in escaped forms, and the tab's title and any extra command are observed; `claude --bg -n` is given the same name and `claude agents --json` shows whether the CLI kept it; if a tab can carry the characters, UI Automation's exact title match is tried through `o`.
- **The verified fact is recorded** in CLAUDE.md's "Windows Terminal facts" (and "Claude Code CLI facts" if the CLI is a cause), and `SessionName`, its `TryParse` summary and `WindowsTerminalArguments` name the same one reason.
- **A relaxed rule is its own change.** If the escapes work end to end, `relax-session-name-rule` is proposed: the adapter escapes the title, `NameProblem.UnsafeCharacters` and its sentence go, and the "A session name is mandatory" requirement changes. That is a spec change and a minor bump, so it is not folded into this spike.
- **Version:** a patch bump only if a shipped file changes (a doc comment in `src\` counts).

Out of scope: changing the rule here, the wizard's or rename's behaviour, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The spike changes documentation only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Docs:** CLAUDE.md's verified-facts sections.
- **Core:** `Domain\SessionName.cs` doc comments, if they change.
- **Infrastructure:** the comment on `Terminal\WindowsTerminal.cs`, if it changes.
- **A possible follow-up change:** `relax-session-name-rule`, proposed from this change's findings.
