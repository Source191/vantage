# Proposal

## Why

Three findings from the 0.15.0 test of attach and quit share one area of the code: every attach prints a two-line return hint that piles up in the scrollback (five copies in the screenshots), quitting leaves the Vantage tab green, and Claude Code 2.1.310 attaches an empty session at its own prompt rather than the agent list, which makes the hint's second sentence, the agent-view key and five passages of documentation wrong. A fourth, `vantage here -d x` blaming `x` instead of `-d`, is a check in the same command line that runs too late to be heard.

## What Changes

- **One short return hint, erased on return.** Before an attach the console prints "Ctrl+Z returns to Vantage." on one line; when the attach returns, that line is erased, so the scrollback is left as it was. The sentence about the agent list goes.
- **Quit restores the tab's colour.** Quitting resets palette entry 264 (OSC 104) as well as writing DECAC, so the tab shows its profile's colour again. DECAC alone only re-points the tab colour at an entry that still holds Vantage's green.
- **The CLI facts move to 2.1.310.** An empty session attaches at its own prompt and Ctrl+Z returns from it. The `--prompt` description, the wizard's prompt caption, four README passages and CLAUDE.md's facts say so.
- **BREAKING: the `a` key goes.** The native agent view existed for an empty session's agent list, which no longer appears from Vantage, and the owner found no other use for it. `claude agents` in any shell is the replacement.
- **`vantage here -d x` names `-d`.** The "looks like an option" check runs on the parse result before the library reports its own errors, so standard error names `-d` first and then `x`.
- **Version:** a minor raise, because a key is removed (pre-1.0, so no major).

Out of scope: what happens to a session's tab on delete, rename, stop or detach (the session-tabs-and-delete change), the header and tooltip work (header-tooltips-and-table-fit), and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: "Drill into a session in the same tab" gains the one-line hint that is erased on return and the empty-session prompt; "The native agent view is one key away" is removed; "Vantage's own tab is recognisable" says quit restores the profile's colour and no longer mentions the agent view.
- `command-line`: "A name that looks like an option needs --" also covers a mistyped option followed by a value.

## Impact

- **Core:** `Application\Wording.cs` (ReturnHint), `Application\Actions\TerminalEffects.cs` (the hint through the console port, the agent-view case gone), `Ports\ITerminalConsole.cs` (show and remove the hint), `Ports\IClaudeCli.cs` (OpenAgentViewAsync gone), `Application\Views\SessionsView.cs` (OpenAgentView and the `a` case), `Application\WordingOfScreens.cs` (SessionsKeys), `Application\CommandLine.cs` (the parse-time check and the `--prompt` description).
- **Infrastructure:** `Terminal\TabEscapes.cs` and `Terminal\TerminalConsole.cs` (the quit sequence, the hint lines), `Claude\ClaudeCli.cs`.
- **Tui:** `WizardControls.cs` (the prompt caption).
- **Tests:** TerminalEffectsShould, ScreenLoopShould, SessionsViewShould, WordingShould, WordingOfKeysAndColumnsShould, ClaudeCliShould, TabEscapesShould, TerminalConsoleShould, CommandLineShould; the FakeTerminalConsole double.
- **Docs:** README (the keys table, "Attach in this tab", "The native agent view", "Opening prompt", the `vantage here` options, the "grey list" and "Claude Code's own screen" troubleshooting entries, the roadmap line on pinning), CLAUDE.md's CLI facts and their date and version, the two specs.
- **Release:** a minor version raise; the README's manual checks for attach and quit, since CI cannot see tab colours or scrollback.

## Order

Pull request 1 of six for the 0.15.0 test findings, which merge one after another in this order, each raising the version one preview step: 1 attach-and-command-line-clean-up, 2 header-tooltips-and-table-fit, 3 pull-request-lookups, 4 session-tabs-and-delete, 5 dialog-fixes, 6 dependabot-version-bump. The redesign changes follow (the WHERE column, the slim header with the key bar and the Sessions and Worktrees tabs, the one-form wizard, the detail pane), and 0.17.0 with the preview suffix dropped is the release to publish once the six have merged.
