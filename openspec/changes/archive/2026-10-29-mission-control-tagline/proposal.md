# Proposal

## Why

The tagline "A terminal console for Claude Code sessions and git worktrees on Windows" says what Vantage is built from, not what it does for its user: it carries many Claude Code sessions so the user need not keep them in their head, in one tab, and they are still there after a reboot. The owner has chosen a new tagline, and it should read the same everywhere Vantage introduces itself.

## What Changes

- The tagline becomes "Mission control for Claude Code sessions: every session in one tab, still there after a reboot." in the package `Description` (nuget.org), the `vantage --help` description (above the support line), the header images (cover, README banner, social preview, redrawn by `tools\New-Header.ps1`, whose tagline now takes any number of lines) and the README banner's alt text.
- The README's opening line and paragraph become one introductory paragraph: mission control in one Windows Terminal tab, sessions in a fresh worktree, attach or open in a named, coloured tab, the bell, the background supervisor keeping sessions listed after the terminal closes or a reboot, and removing a session with its worktree unless work would be lost.
- The GitHub repository's About description takes the tagline (set with `gh repo edit`, outside the code).

Out of scope: `openspec\config.yaml` and `CLAUDE.md`, whose opening lines describe the project for its developers rather than introduce it; uploading the new cover and social preview, which the owner does by hand; the version, already raised to `0.17.0-preview.3` by `support-links` in the same pull request; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Wording only: the command-line spec requires the help to describe Vantage, not in which words; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `src\Source191.Vantage\Source191.Vantage.csproj` (`Description`), `src\Source191.Vantage.Core\Application\CommandLine.cs` (root description).
- `tools\New-Header.ps1` and `assets\header\*.png`.
- `README.md`: the banner alt text and the opening paragraph.
