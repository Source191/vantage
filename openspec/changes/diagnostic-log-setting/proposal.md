# Proposal

## Why

The diagnostic log is written to a file only when the `TORNI_LOG` environment variable names one. That is set before Torni starts and is invisible from inside it, so the Config tab can only show the log as a read-only setup row and tell the user to go and set a variable. Turning the log on for a day's monitoring, or off again, should be one key on the Config tab, like Check for updates.

## What Changes

- A new `diagnosticLog` setting in `config.json`, off when absent. On the Config tab it is a **Diagnostic log** setting row after Check for updates, reading `on` or `off`. Enter turns it on or off and saves at once.
- When on, every diagnostic event and the counts are appended to one fixed file, `%LOCALAPPDATA%\Torni\torni.log`. The row's tooltip names that file's full path.
- The change applies at once, without a restart: turning it on starts writing with the next event, and turning it off stops. The in-memory log and the crash file are unchanged.
- The file is capped. When an append would take it past 10 MB, it becomes `torni.log.1` (replacing any older one) and a new `torni.log` starts, so at most about 20 MB is ever kept.
- **BREAKING:** `TORNI_LOG` is removed. Setting it does nothing, and the Diagnostic log setup row goes from the Setup section. Anyone who used it (the performance-monitoring guide, scripts) switches the setting on instead.

## Capabilities

### New Capabilities

None.

### Modified Capabilities
- `diagnostics`: the log file comes from the setting at a fixed path with a size cap, instead of `TORNI_LOG`. The periodic counts follow the setting, and the privacy rule names the log file.
- `settings`: `config.json` gains `diagnosticLog` (default off), which applies without a restart like the other settings.
- `config-tab`: Diagnostic log becomes a setting row toggled with Enter, with its file in the tooltip, and leaves the Setup section and the setup rows' hints.
- `tool-updates`: a scenario no longer says a failure is also written to the `TORNI_LOG` file; it names the log file instead.

## Impact

- **Code:**
  - Core: `Domain\Settings` and `SettingsText`, `Ports\TorniConfig` and `Application\SettingsFile` (the new property); `Views\ConfigRow`, `ConfigRows` and `ConfigView` (the row and its Enter).
  - A new `ToggleDiagnosticLog` effect with a `ToggleDiagnosticLogHandler` and outcome, and its `Wording` and `WordingOfConfig` lines.
  - `Diagnostics\PeriodicCounts`, and `DiagnosticLogSetting`, which is removed.
  - Infrastructure: `Diagnostics\DiagnosticFile` (the fixed path, the switch and the rollover).
  - The composition root: `SharedParts`, `ConsoleComposition` and the other commands' compositions.
  - `SetupFactsReader` and `SetupItem` lose the diagnostic log.
- **Tests:** the matching `*Should` classes, including `DiagnosticFileShould` against a temporary folder, and `InheritedEnvironmentShould` loses its `TORNI_LOG` case.
- **Docs:** `docs\performance-monitoring.md`, CONTRIBUTING, the README if it mentions the log, `.claude\rules\security.md` and `console-screen.md`.
- **Overlap:** #206 (`install-with-winget`) also modifies config-tab's "The Config tab shows the setup" and tool-updates' "Torni checks for a newer release at most once a day". Whichever change archives second rebases its copy of those two requirement blocks onto the first.

## Out of scope

- Multiple Claude accounts, which is a later change.
- Choosing the log file's location or size, which are fixed.
- Writing anything new to the log: the events, the counts and the privacy rules stay as they are.
- A key to open or clear the log file. The file sits in the folder `f` already opens from the Settings file row.
