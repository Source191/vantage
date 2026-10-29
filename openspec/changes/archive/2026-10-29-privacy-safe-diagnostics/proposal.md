# Proposal

## Why

Vantage's only diagnostics are an opt-in file (`VANTAGE_LOG`) that must be switched on before a problem happens, holds four lines of Vantage's own and Terminal.Gui's free text, and is not safe to share: the hand-off line writes an effect's `ToString()`, which carries session names and job ids, and Terminal.Gui can quote raw input it failed to parse, which can be typed or pasted text. Failures shown in the footer keep only the exception's message and lose the stack trace, and child processes, crashes and environment facts are not recorded at all. A problem report (the `report-a-problem` change) needs a log that is always on, useful to the maintainer and private by construction, so that nothing about the user or the code in their repositories can ever reach it.

## What Changes

- **Always-on diagnostic log:** Vantage keeps the most recent diagnostic events in memory for the life of the process, whether or not `VANTAGE_LOG` is set. `VANTAGE_LOG` still writes the same events to a file as they happen.
- **Private by construction:** an event holds only facts Vantage itself defines: which of its own keys' effects ran and the kind of outcome, counts of the child programs it ran (git, claude, gh, az, pwsh, wt and a few more) by subcommand from a fixed list and exit code, with the longest duration, plus a line for a run that fails to start or is slow, which view or dialog opened, versions, counts and exception summaries (exception type names and Vantage's own stack frames, never the message). It never holds a path, repository, branch, worktree or session name, job or session id, prompt, git or CLI output, pull-request detail, file name, user name, machine name or anything typed.
- **BREAKING (diagnostics only):** the `VANTAGE_LOG` file no longer carries free text. The hand-off line names the effect's kind only, and Terminal.Gui's diagnostics are recorded as level and exception summary, Warning and above, without their message text.
- **Failures are recorded:** an effect that fails, a view that fails to load, a dialog flow that fails, the update check and an unobserved task exception each record an exception summary, while the footer keeps showing the message as today.
- **Crash file:** an exception that ends Vantage writes the in-memory log and the crash's summary to `%LOCALAPPDATA%\Vantage\last-crash.log`, replacing any earlier one; the `report-a-problem` change reads it.
- **Guarded by tests:** a reflection fact fails the build when a diagnostic event gains a field of a type that could carry free text, and a privacy fact runs the main flows over Northwind fixtures and asserts none of their names, paths, prompts or outputs appear in the rendered log or the crash file.
- The README's Troubleshooting and "Where Vantage keeps things" sections say what is recorded and where; `Version` rises (a minor raise, one preview step above `main`'s).

Out of scope: sending anything anywhere (the log never leaves the machine in this change; `report-a-problem` is the only route out, and only by the user's choice), keystroke or mouse logging of any kind, a persistent rolling log across runs, log levels or configuration beyond `VANTAGE_LOG`, Windows Terminal's own version (it has no supported query), and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

- `diagnostics`: what Vantage records about its own behaviour, where it keeps it (memory, the `VANTAGE_LOG` file, the crash file) and the privacy rule every record obeys.

### Modified Capabilities

- `tool-updates`: the "check fails unexpectedly" scenario records the failure in the always-on diagnostic log rather than only when `VANTAGE_LOG` is set.

## Impact

- `src\Source191.Vantage.Core`: a new `Application\Diagnostics` area (the event records, the in-memory log, exception summaries, the rendered lines, the recording process runner); `HandoffLog` and `ScreenLoop` move to it; `EffectDispatcher` and the load and dialog failure paths record failures.
- `src\Source191.Vantage.Infrastructure\Diagnostics`: `FileLogger` is replaced by a file sink for rendered lines, a Terminal.Gui logger bridge that drops message text, and the crash file writer.
- `src\Source191.Vantage.Tui`: the two `catch` blocks in `TableScreen` and the dialog flows hand their exceptions to the log.
- `src\Source191.Vantage\Program.cs`: wiring, the unhandled and unobserved exception hooks.
- Tests: `Architecture\DiagnosticEventsShould`, a privacy fact, and replacements for `HandoffLogShould`; `RecordingLogger` in TestDoubles becomes a recording diagnostics fake.
- README (Troubleshooting, Where Vantage keeps things) and `src\Source191.Vantage.Infrastructure\README.md` if it describes the logger.
