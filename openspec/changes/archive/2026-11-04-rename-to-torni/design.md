# Design

## Context

The name appears about 4,800 times in 1,005 of the 1,062 files outside the archive, and in 854 paths, because every project is `Source191.Vantage.*`. It takes four case forms that map one to one: `Vantage`, `vantage`, `VANTAGE` (the environment variables) and `Source191.Vantage` (projects, namespaces, the winget identifier). No other word contains it ("advantage" does not occur), and every compound identifier (`createdByVantage`, `IVantage...` test names) reads correctly with Torni in its place. Torni is two letters shorter, so no column, tab title or header grows, but a text laid out by hand around the old name loses two characters of padding.

## Goals / Non-Goals

**Goals:**
- One mechanical pass that leaves no `Vantage` in a live file or path, except the few named below, and a build and test suite that pass unchanged in behaviour.
- The rename's own pull request passes CI, although its base still has the old project path.

**Non-Goals:**
- Any behaviour change beyond the names; any code that runs only once to migrate a machine.

## Decisions

- **A script, not hand edits.** Paths are moved with `git mv` (so history follows each file), then the text of every tracked text file (`git grep -I`) is rewritten with the three case-sensitive replacements. Binary assets are only renamed, then redrawn. Hand edits come after, for the exceptions. The alternative, an IDE rename per project, would miss the docs, scripts and workflows, which hold a third of the mentions.
- **What keeps the old name:** `openspec\changes\archive` and the released sections of `CHANGELOG.md` (history); `openspec\specs`, which the archive of this change rewrites through its deltas; this change's own artifacts; the nuget.org package `Source191.Vantage` that `install-with-winget` retires; and the old project path in the version scripts' fallback (below).
- **Delta specs generated from the main specs.** Each requirement that names the program is copied into a MODIFIED block with the name replaced, and each title that names it gets a RENAMED entry, as `rename-footer-to-status-message` did. Generating them avoids retyping 121 requirements by hand; `openspec validate` checks the result, and archiving applies it. The Purpose lines are not requirements, and archive matches a requirement's scenarios by title (so a delta cannot retitle one), so the Purpose lines and the scenario titles that name the program are reworded by hand after the archive.
- **The unarchived changes are renamed too.** Their deltas name requirements by title; once this change renames those titles, an unrenamed delta would no longer find its requirement on archive. Renaming them keeps either archive order working.
- **The base's Version at the old path.** `Read-ToolVersion` in `ToolPaths.ps1` and the release step in `pipeline.yml` read `src/Source191.Torni/Source191.Torni.csproj` at a commit, and fall back to `src/Source191.Vantage/Source191.Vantage.csproj` when it is not there. Without it this pull request's version check, and the release on its merge, would fail to read the base. The fallback is harmless afterwards and can go in any later change.
- **No migration.** Torni starts with defaults in `%LOCALAPPDATA%\Torni`. Only the owner has the old folder; moving it, deleting the old fragment and rerunning `torni setup-terminal` are one-off steps in the pull request.
- **A T from the V's parts.** `TorniMark.ps1` keeps the tile, the three nodes, their colours and the stroke width, and moves them: the teal and purple branches run level from the top corners and meet at the centre, and the white stem drops from there to the bottom node. Keeping the parts means the icon still reads as worktree branches meeting in one place, and every script that draws the mark (`New-Icon.ps1`, `New-IconPixels.ps1`, `New-Header.ps1`) picks it up unchanged.
- **Images redrawn by their scripts.** `New-Icon.ps1` and `New-IconPixels.ps1` remake the icon sizes, the `.ico` and the console's icon pixels; `New-Header.ps1` the banner, social preview and Buy Me a Coffee cover with the title, tagline and install line; `New-ReadmeScreenshot.ps1` the screenshot and animation from the scene, whose stand-in for the tool's own repository becomes `torni`.
- **Trimming the name by reading, not by script.** Each sentence in the human and agent docs that names the program is read and kept or reworded by these rules: it keeps the name when it is a command, a path, a profile, variable or package name, a title or a document's first introduction, or when the sentence sets Torni beside another program (Claude Code, Windows Terminal, git, an editor) and "it" would be ambiguous; otherwise the name becomes "it", "the console", "the tool" or goes, as the sentence reads best. The specs, code comments, the changelog and `Wording` are left: requirements need a subject, and the screen and the What's new dialog are read away from the repository.

## Risks / Trade-offs

- [The GitHub links and the README's raw image URLs point at `Source191/torni` before the repository is renamed] → They resolve once it is; GitHub redirects the old name to the new, not the reverse, so the owner renames the repository before merging.
- [A test's hand-padded expected text loses two characters with the shorter name] → The layout tests compare the whole text, so each one fails and is re-padded; none passes by accident.
- [A reworded sentence in the agent docs could lose the distinction between Torni and Claude Code] → The trimming rule keeps the name wherever another program is in the same sentence.
- [The local checkout's folder name and Claude Code's per-folder memory] → Left as they are; renaming them is optional and listed for the owner.
