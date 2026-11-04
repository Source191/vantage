# Tasks

## 1. The setting

- [ ] 1.1 Red: `SettingsShould` and `SettingsFileShould` facts that `diagnosticLog` reads false when absent, true when `true`, every default for a value that is not a boolean, and is written back as `diagnosticLog`. Green: add it to `Domain\Settings`, `SettingsText`, `Ports\TorniConfig` and `Application\SettingsFile`. Verify the new facts and the existing settings facts pass.

## 2. The switch and the file

- [ ] 2.1 Red: `DiagnosticLogSwitchShould` facts that a new switch is off, that it follows the function it is pointed at on every read, and that it can be pointed again from another thread. Green: add `Application\Diagnostics\DiagnosticLogSwitch` over a `SharedSnapshot`. Verify the facts pass.
- [ ] 2.2 Red: `DiagnosticFileShould` facts (temporary folder): nothing is written while the switch is off; lines are appended while it is on; switching off then on writes only the lines recorded while on; a locked file is skipped without throwing. Green: give `DiagnosticFile` the switch. Verify the facts pass.
- [ ] 2.3 Red: `DiagnosticFileShould` facts for the rollover: a line that would take the file past 10,485,760 bytes moves it to `torni.log.1` and starts a new file with that line; an existing `torni.log.1` is replaced; a failed move leaves the line in the old file. Green: add the size check and move to `DiagnosticFile`. Verify the facts pass.
- [ ] 2.4 Red: `PeriodicCountsShould` facts that the timer ticks with no setting passed, and that its lines reach a sink every 10 minutes. Green: remove `DiagnosticLogSetting` from `PeriodicCounts`. Then remove `DiagnosticLogSetting` and its tests, and `NullFreeModelsShould`'s `Kept` entry for it. Verify `PeriodicCountsShould`, `NullFreeModelsShould` (including `KeepNoStaleEntry`) and the build pass.

## 3. The toggle

- [ ] 3.1 Red: `ToggleDiagnosticLogHandlerShould` asserting that the handler flips `diagnosticLog` through `CurrentSettings`, saves `config.json` (`TestSettings`, `InMemoryState`), and returns `ToggleDiagnosticLogOutcome(On, File)`. Green: add the `ToggleDiagnosticLog` effect, the handler, the outcome record in its own file and its line in `Application\Effects\Composition.Handlers`. Verify the handler test, `EffectDispatcherShould` and `HandlersShould` pass.
- [ ] 3.2 Red: `WordingOfOutcomesShould` facts for both outcomes (on names the file; off says the log is off), and the tone in `StatusTonesShould`. Green: the words in `WordingOfOutcomes` and the tone in `Wording.ToneOf`. Verify both test classes pass.

## 4. The Config tab

- [ ] 4.1 Red: `ConfigRowsShould` facts that a `ConfigRow.DiagnosticLog` row with the file follows Check for updates, and that the Setup rows no longer hold a diagnostic log. `ConfigViewShould` facts that Enter on it raises `ToggleDiagnosticLog` and that Down from it steps over the heading to Settings file. Green: the row in `ConfigRow` and `ConfigRows.For`, and the Enter case in `ConfigView`; remove `SetupItem.DiagnosticLog` and `SetupFacts.DiagnosticLog` and `SetupFactsReader`'s reading of them. Verify both test classes, `SetupFactsReaderShould` and `ConsoleLoadsShould` pass.
- [ ] 4.2 Red: `WordingOfConfigShould` facts for the row's label (Diagnostic log), VALUE (on or off, no path) and tooltip (what it does, the full path, Enter turns it on or off), within the 110-character description limit. Green: `WordingOfConfig`. Verify `WordingOfConfigShould` and `KeyHelpParityShould` pass.

## 5. Wiring and documentation

- [ ] 5.1 Wire it (untested composition): `SharedParts` always adds the `DiagnosticFile` for `%LOCALAPPDATA%\Torni\torni.log` with a switch and no longer reads `TORNI_LOG`; `ConsoleComposition` points the switch at `CurrentSettings` before `RecordStart` and gives `ConfigRows` the path; `HereComposition` loads the settings once and points the switch at them before `RecordStart`. Remove `InheritedEnvironmentShould`'s `TORNI_LOG` case. Verify `dotnet build -c Debug`, the whole solution's tests and the hand-off check.
- [ ] 5.2 Documentation: `docs\performance-monitoring.md` (turn the setting on instead of `$env:TORNI_LOG`, and the file's place and rollover), CONTRIBUTING's Performance line, `.claude\rules\security.md` (Diagnostics) and `console-screen.md` if they name `TORNI_LOG`. Verify `git grep TORNI_LOG` outside the archive, `CHANGELOG.md` and this change finds nothing.
- [ ] 5.3 Raise `Version` as CONTRIBUTING's Versions says and add `CHANGELOG.md` lines: Added (the Diagnostic log setting) and Changed (`TORNI_LOG` removed, use the setting), each ending with the pull request number. Verify `Assert-VersionBumped.ps1` and `Assert-ChangelogUpdated.ps1` against `main`.
- [ ] 5.4 Manual (owner): in a local build, turn Diagnostic log on with Enter, see the tooltip name `torni.log` and the status line say so, stop a session, and find the line in `%LOCALAPPDATA%\Torni\torni.log`; turn it off and check no further line is added. Verify by eye and by the file.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs` describes the code on `main`. At archive, retitle the two "No log variable" scenarios in `diagnostics` (OpenSpec keeps a modified scenario's title) and rebase the two requirement blocks shared with `install-with-winget` (#206) onto whichever archived first.
