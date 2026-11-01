# Design

## Context

See proposal.md for why. Claude Code loads `CLAUDE.md` and every `.claude\rules\*.md` without `paths` frontmatter at launch; a rule with `paths` loads when Claude uses Read, Write or Edit (or a Bash read such as `cat`) on a matching file, with patterns relative to the folder that holds `.claude`. A subfolder's `CLAUDE.md` also loads on demand, but under `src\` it would count as a change to the packaged tool for `Assert-VersionBumped.ps1`.

## Goals / Non-Goals

**Goals:**
- The always-loaded file under about 25,000 characters, under headings, with every rule kept.
- Each platform fact loaded when the code that depends on it is opened.

**Non-Goals:**
- Rewording verified facts, changing their dates or versions, or deciding anything new.
- Supporting other coding agents (see proposal.md).

## Decisions

**Move, do not cut.** The owner asked for a shorter file "without losing any of its benefit". Text is moved to a rule file or tightened; a sentence is removed only when it repeats another one in the same file or restates `CONTRIBUTING.md` word for word (the CI paragraph moves to `checks.md` rather than being deleted, because a few of its facts, such as no `setup-dotnet`, are not in `CONTRIBUTING.md`).

**What stays always loaded.** Anything that changes how code anywhere is written or reviewed: layers, idioms, switches, reducers and effects, the new-key recipe, one home per fact, text values, null, wording, security, diagnostics privacy, the commands and when to run each check, the release rules, and the Claude Code CLI facts (the adapters, the actions and the contract tests all rest on them). The effect markers and the folder keys stay because effects and handlers are written in `Application`, which no screen rule covers.

**What moves.** The console's layout (`ConsoleScreen`, tabs, status line, detail pane, Config tab, worktree scanning), the Terminal.Gui facts, the Windows Terminal and process facts, the check details and the CI paragraph. Each rule file's `paths` lists the folders and the named types its facts are about, in source and tests (`**/TabShade*.cs` matches both), so a session editing `Domain\TabShade.cs` sees the Windows Terminal measurement behind it.

**Pointers.** `CLAUDE.md` opens with one line per rule file, saying what it holds, so a session that reasons about tabs from `Application` without opening a matching file still knows to read it. The checks keep their bullet names ("Hand-off check", "Smoke check of the packed tool") in `CLAUDE.md`, because `check.yml` cites them.

## Risks / Trade-offs

- [A session needs a moved fact without opening a matching file] → the pointers at the top of `CLAUDE.md`, and `paths` that include the domain and application types each fact is about, not only the adapter folders.
- [A rule file's frontmatter fails to parse] → Claude Code then loads it at launch as if it had no `paths`, which costs context but loses nothing; the task that writes the files checks each with `claude --debug` or by reading it back.
- [Facts drift between the files] → each fact has one home; `CONTRIBUTING.md` and `openspec\config.yaml` name where the verified facts live.
