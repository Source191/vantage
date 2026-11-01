# Proposal

## Why

The owner asked on 1 November 2026 for two small simplifications. The 1, 2 and 3 keys duplicate Left and Right, which are more intuitive for three tabs, and every extra key is one more line in the help and the README. The README has grown to 445 lines and 22 sections, an overwhelming read for someone who only wants to install Vantage and start a session; the `?` help, the tooltips and the key bar already describe every key and column inside the tool. Ctrl+Z, Claude Code's own detach key, is mentioned three times in the README and the owner does not think it is useful to the person using Vantage.

## What Changes

- **Tab keys:** 1, 2 and 3 no longer select a tab, and the help no longer lists them. Left and Right, a click on a tab's title, and Esc back to Sessions are unchanged. **BREAKING** for anyone who used the digits; nothing stable has shipped them yet (they arrived in the Unreleased section, #117).
- **Smoke check:** the headless smoke script reaches each tab with Left and Right instead of the digits.
- **README:** cut to a short getting-started guide: the pitch, prerequisites, install, a quick start, the everyday keys, one short notifications section (`vantage setup-terminal`), and links to the in-tool help, the changelog, security and contributing. Everything else is deleted rather than moved, as the owner chose: the per-tab key and column tables, the wizard, detail pane, pull request, updates, reporting, file locations, troubleshooting and roadmap sections. The README still states `2.1.309 or later`, which `DocumentedMinimumShould` reads.
- **Ctrl+Z:** removed from the README only. `vantage here` keeps printing "Ctrl+Z detaches; the session keeps running." and its `--help` keeps the same words, because that is the only way back to the shell from `here`.
- **CONTRIBUTING.md:** its link to the README's Reporting a problem section, which is gone, points at pressing `!` instead; its rule that the README describes any behaviour a user will notice becomes one that the README covers getting started and the `?` help covers the rest.
- **Changelog:** the Unreleased Added line for #117 drops "1, 2 and 3".
- **Version:** 0.20.0-preview.14, because files under `src\` change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `console-layout`: the tab requirement drops the 1, 2 and 3 keys; scenarios that pressed a digit press Right or Left instead; the help scenario lists only ← and → for tabs.
- `config-tab`: two scenarios that pressed 3 show the Config tab with Right instead.
- `settings`: one scenario that pressed 3 reaches the Config tab with Right.
- `worktree-management`: two scenarios that pressed 2 show the Worktrees tab with Right instead.
- `needs-input-notification`: the README's notifications section names `vantage setup-terminal` as the step that sets Claude Code's bell; the requirement that the README explains bell styles, sound files and the manual Windows Terminal steps is removed.

## Impact

- **Code:** `Application\Views\TabKeys.cs` and `SelectTab.cs` (summaries), `Application\WordingOfScreens.cs` (`TabKeyHelp`); `tools\HeadlessTerminal\SmokeScript.cs`.
- **Tests:** `TabKeysShould`, `SessionsViewShould`, `WorktreesViewShould`, `ConfigViewShould`, `SmokeScriptShould`, and any help test that lists the tab keys.
- **Docs:** `README.md`, `CONTRIBUTING.md`, `CHANGELOG.md`, `.claude\rules\console-screen.md`.
- **Users:** the digits stop switching tabs; the README is about a quarter of its length.

## Out of scope

- Ctrl+Z in the product: the `vantage here` hint and its `--help` stay.
- A separate user guide in `docs\`: the owner chose to delete the detail rather than move it.
- Multiple Claude accounts, which is a later change.
