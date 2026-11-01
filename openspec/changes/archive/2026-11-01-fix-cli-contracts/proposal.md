# Proposal

## Why

The Claude CLI adapter reads claude's exit code and words for `--version`, `--bg` and `--bg --resume` but not for `agents`, `stop` or `rm`, and the port says nothing about which methods fail how, where `IGit` states its convention. So a `claude agents` that fails (the background service down, a future CLI change) prints its reason to standard error and nothing to standard output, and the Sessions tab's banner reads "listing failed: The input does not contain any JSON tokens" instead of claude's sentence. A failed `claude stop` is reported as "Stopped", and a failed `claude rm` is silent: `r`, `R` and `d` then carry on as if the job had gone, and `r` and `R` run `--bg --resume` on a job that is still listed, which the CLI facts say starts a copy. Beside it, `ResumeResult` is a bool and a message where its sibling `StartResult` is a closed hierarchy, and `vantage here` leaves the return hint on screen and the terminal unrecovered (focus reporting on, cursor hidden) when the attach throws, because neither clean-up is in a `finally` (code review findings I-06, D-11 and A-14).

## What Changes

- The Sessions tab's banner shows claude's own sentence when the listing exits with an error, never a JSON parser's message; a listing that exits 0 but is not JSON still fails as it does now.
- `x` on a session claude refuses to stop reports "Could not stop fix-123: <claude's reason>" as a failure, and the session's tab stays open.
- `r` on a session whose job claude refuses to remove resumes nothing, so no copy is started; the status line says the rename failed with claude's reason and that the session is still listed under its old name, and its tab stays open.
- `R` skips a session whose job claude refuses to remove, resumes the others, and names the skipped one with claude's reason in the notes.
- `d` on a session whose job claude refuses to remove reports "Could not delete fix-123: <claude's reason>" as a failure, closes no tab and touches no worktree.
- `vantage here` erases its "Ctrl+Z detaches" line and puts the terminal back in order when the attach cannot start or is interrupted, as it does after a detach; the failure still propagates as it does today.
- Internal, no behaviour change: `ResumeResult` becomes a closed hierarchy (`Continued`, `Failed`) matched by case in the adopt, rename and resume-all handlers, and `TerminalEffects` runs `AttachToSession` through a typed method instead of a `switch` with a silent default.

Out of scope: a result type instead of an exception for stop and rm (the port follows `IGit`'s convention); retrying a failed stop or rm; reporting a failed `claude agents` anywhere but the banner and the start's existing failure message; changing what `vantage here` prints or exits with when the attach throws; the `claude.exe` lookup and the words for a missing CLI (`external-tools`); and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: the listing's failure banner names the CLI's own reason when the CLI exits with an error.
- `session-attach`: a stop or removal the CLI refuses is reported in the CLI's words and nothing is resumed, closed or removed after it (`x`, `r`, `R`, `d`).
- `session-launch`: `vantage here` erases the detach hint and recovers the terminal when the attach fails as well as when it exits.

## Impact

- `Ports\IClaudeCli` (the convention in its summary and per method) and a new `Ports\ClaudeCliException` beside `GitException`; `Ports\ResumeResult` as a hierarchy.
- `Adapters\Claude\ClaudeCli`: one `RunOrThrowAsync` for `agents`, `stop` and `rm`, as `GitCli` has; `ResumeAsync` answering the two cases.
- `Application\Actions\StopSessionHandler`, `RenameSessionHandler`, `ResumeSessionsHandler`, `DeleteSessionHandler` (a failed outcome case each, in `StopSessionOutcome`, `RenameSessionOutcome`, `ResumeNote` and `DeleteSessionOutcome`) and `AdoptSessionHandler` (matching `ResumeResult` by case); `Application\Actions\TerminalEffects` and `Application\Launch\HereCommandRunner` (`finally` blocks).
- `Application\WordingOfOutcomes` and the facts in `WordingOfOutcomesShould` and `StatusTonesShould`; `ClaudeCliShould`, the five handler tests, `TerminalEffectsShould`, `HereCommandRunnerShould`, and every test that constructs a `StopSessionOutcome`.
- `CLAUDE.md`'s "One home per fact" gains `IClaudeCli`'s convention beside `IGit`'s; `Version` raised in the implementing pull request (patch: fixes and text) and one line under Fixed in `CHANGELOG.md`'s Unreleased section, which `Assert-ChangelogUpdated.ps1` (PR #180) requires of every pull request that changes the packaged tool.
