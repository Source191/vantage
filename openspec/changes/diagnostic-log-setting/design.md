# Design

## Context

`SharedParts.ForCurrentUser` reads `TORNI_LOG` once through `DiagnosticLogSetting.Read`. It builds the one `DiagnosticLog` with a `DiagnosticFile` sink only when the variable names a file, and hands the setting to `PeriodicCounts` (whose timer runs only for a file) and to `SetupFactsReader` (the read-only Diagnostic log setup row). The log exists before any command has read `config.json`: the console loads `CurrentSettings` afterwards in `ConsoleComposition`, and `torni here` (`HereComposition`) loads no settings at all. Check for updates is the pattern to copy: a `checkForUpdates` property in `config.json`, `Settings` and `SettingsFile`, a `ConfigRow.CheckForUpdates(On)` row, Enter raising `ToggleUpdateCheck`, `ToggleUpdateCheckHandler` saving through `CurrentSettings`, and a `ToggleUpdateCheckOutcome`.

## Goals / Non-Goals

**Goals:**
- One switch, saved in `config.json`, that the console turns on and off with Enter and that applies from the next line.
- A fixed file whose full path the row's tooltip shows, and which can never grow past two files of about 10 MB.

**Non-Goals:**
- Making the location or size configurable.
- Any change to what is recorded, the in-memory log, the crash file or the problem report.

## Decisions

### The file sink is always present and asks the setting on every line
`SharedParts` always adds a `DiagnosticFile` for `%LOCALAPPDATA%\Torni\torni.log`. That is the state folder it already resolves, so the path is held once there and handed to whatever needs to show it. The sink is given a switch that it reads on every `Write`, and writes nothing while the switch is off. Until a command has read `config.json` the switch is off, so nothing is written before then; the start line is recorded after the settings are read in both commands, so it is never lost.
- **The console** points the switch at `CurrentSettings` as soon as they are loaded. Every later line follows the saved value, so Enter applies from the next event.
- **`torni here`** loads the settings once as it starts and points the switch at them. A toggle made in a console meanwhile applies to `here` at its next start, because the processes don't share memory.
- **Alternative: rebuild the log's sinks when the setting changes.** This needs a mutable sink list shared between threads, where the switch is one read per line. Rejected.
- **Alternative: read the setting once as Torni starts.** This is simpler, but you chose "at once", and every other setting applies without a restart. Rejected.

The switch is an `Application` type (for example `DiagnosticLogSwitch`, holding a `SharedSnapshot<Func<bool>>` that starts at "off"), so its default and its rebinding are tested in Core. `DiagnosticFile` stays the only thing that touches the file.

### The rollover lives in the file sink
Before each append, `DiagnosticFile` compares the file's length plus the line's UTF-8 byte count with 10,485,760. When it would pass that, it moves `torni.log` to `torni.log.1` (`File.Move` with overwrite), then appends. As today, every `IOException` and `UnauthorizedAccessException` is swallowed, so a log never takes Torni down; a failed move just means the line goes to the old file.
- **Alternative: a size check on a timer.** This could overshoot between checks and adds a timer. Rejected.
- **Alternative: date-stamped files.** These need a retention rule and several files to look through. The one rollover you chose keeps it to two names.

### Periodic counts always tick
`PeriodicCounts` loses its `DiagnosticLogSetting` parameter and always runs its 10-minute timer; its writes go through the log's sinks, so they reach the file only while the switch is on. That gives the "Turned on part way" scenario for free: the next mark after turning it on is written, and earlier marks are not.

### The row carries its file for the tooltip
`ConfigRow.DiagnosticLog(bool On, string File)` sits after `CheckForUpdates` in `ConfigRows.For`, which is given the path. `Wording.ConfigDescription` names the file in the tooltip, while the VALUE reads only on or off. Enter raises a new `ToggleDiagnosticLog` effect. Its `ToggleDiagnosticLogHandler` (in `Application\Actions`, its own `...HandlerShould`) saves through `CurrentSettings`, as `ToggleUpdateCheckHandler` does, and returns a `ToggleDiagnosticLogOutcome(On, File)`. `WordingOfOutcomes` turns that into a sentence naming the file when on, with a success tone.

### TORNI_LOG goes entirely
`DiagnosticLogSetting`, `SharedParts.LogSetting`, `SetupItem.DiagnosticLog` and `SetupFacts.DiagnosticLog` are removed, with their wording and tests. So are `NullFreeModelsShould`'s `Kept` entry for `DiagnosticLogSetting.Read` and `InheritedEnvironmentShould`'s `TORNI_LOG` case. Nothing reads the variable any more, so a value left in someone's environment is simply ignored.

## Risks / Trade-offs

- **[Two Torni processes write the same file]** A console and a `torni here` can both have the log on. → Each line is one `File.AppendAllText`, so lines interleave whole, as they would today if `TORNI_LOG` named one file for both. If both hit the size at once, one move may fail and is swallowed, so the line goes to whichever file exists.
- **[Breaking for anyone using TORNI_LOG]** → The changelog says so under Changed, and `docs\performance-monitoring.md` switches to the setting. The tool is pre-1.0, where a minor version may break.
- **[A user forgets the log is on]** → It can't grow past about 20 MB, and the row reads on.

## Migration Plan

1. Land the change with a minor version bump and a changelog entry under Changed naming the removal of `TORNI_LOG`.
2. Nothing in `config.json` needs migrating: `diagnosticLog` is off when absent.
- **Rollback:** revert the pull request. A `diagnosticLog` left in `config.json` is then ignored by the older version's reader, as an unknown property already is.
