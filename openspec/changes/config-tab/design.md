# Design

## Context

After tabs-and-key-bar the console is one Terminal.Gui application (`Tui\ConsoleScreen`) hosting a `TableBody<TState, TRow>` per `IScreenDefinition`, each over a reducer composed on `ListSelection<TRow>`; `ConsoleState` holds both views' states and the active `ConsoleTab`, and `ConsoleView.Apply` handles the `ISwitchesTab` effects. The settings are edited in `SettingsDialog`, a binder over the `SettingsView.Reduce` reducer (`SettingsState`, `SettingsInput`, `SettingsEffect`), opened by `OpenSettings` (`,`) and saved through `SaveSettings` and its handler into `config.json` via `SettingsFile`; `CurrentSettings` is read per scan, wizard and redraw. The setup facts already have ports: `ITerminalFragment` (`Exists`, `Location`), `IClaudeSettings` (`ReadNotificationChannelAsync` answering a `NotificationChannelState`), `IUpdateCheckRecords` (`CheckedAt`, `Newest`) and the `VANTAGE_LOG` path read in `Program.cs`. Detail-pane will have added a pane under each table; this change lands after it.

## Goals / Non-Goals

**Goals:**
- A third tab built the way the other two are: a reducer, a screen definition and a `TableBody`, so the key bar, help, tooltips, filter and footer work unchanged.
- One home for each settings rule: the checks the dialog makes now stay in one place and are applied to the one value being saved.

**Non-Goals:**
- Writing to the Windows Terminal fragment or Claude's settings from the tab (the CLI's `setup-terminal` keeps that).
- Any new setting or a change to `config.json`'s shape.

## Decisions

- **Rows are a read model, `ConfigRow`, keyed by kind and index.** A closed `ConfigRowKind` (RepositoryRoot, WorktreeRoot, OpenFolderWith, DefaultBase, CheckForUpdates, SettingsFile, TerminalProfiles, ClaudeBell, DiagnosticLog, LastUpdateCheck) plus the root's index makes the selection key stable across a save, so the selection stays on the row just edited. Built by `Application\ConfigRows` from `CurrentSettings` and a `SetupFacts` record. Alternative: one row type per kind; rejected, because the table needs one `TRow`, and the switch over a closed enum makes each new kind fail the build where it needs words (CS8509).
- **Setup facts load like a listing.** `ConfigView` has a load (`TriggerLoad`) that reads `SetupFacts` off the UI thread through the ports, with the setting rows built at once from `CurrentSettings` and the setup rows shown as "reading…" until the facts arrive. It runs when the tab is shown and after every save; the Config tab has no refresh timer. A failed read of one port becomes that row's "could not be read", never a load failure of the tab.
- **One dialog per setting kind, one reducer.** `SettingsView` keeps its reducer and its rules but works on a `SettingsField` subset: `SettingsState.Open` takes the field being edited (a root at an index, a new root, the worktree root, the editor, the base) and validates only it on Save, against the other settings as saved (so "already a root" can be checked). `SettingsDialog` becomes a smaller binder showing that field's controls only; the editor dialog keeps the preset choice and the custom executable and arguments. Alternative: a new reducer per kind; rejected, as it would copy the problems and their wording.
- **Effects and handlers.** `EditSetting(ConfigRowKey)`, `AddRepositoryRoot` (dialog effects, routed by `DialogEffects` to the dialog, which settles on the existing `SaveSettings` effect with the whole new `Settings`), `RemoveRepositoryRoot(int index)` and `ToggleUpdateCheck` (background effects with handlers returning outcome records). `Settings` gains `WithRootRemoved` and `WithRootAdded` that refuse the last root and a duplicate as values (`SettingsProblem.LastRoot`, `SettingsProblem.RootAlreadyListed`) rather than exceptions. Enter on a setup row and d off a root row are reducer footers (`Footer.ConfigAdvice`), not effects.
- **Tab keys live in `ConsoleView`.** `ConsoleTab` gains `Config`; Left and Right become `PreviousTab` and `NextTab` effects marked `ISwitchesTab`, with `SelectTab(ConsoleTab)` for 1, 2 and 3; `SwitchToWorktrees`, `SwitchToSessions` and `OpenSettings` go with `w` and `,`. `ConsoleScreen` no longer strips Left and Right from the window, only from `Tabs` and the tab views, and passes Shift+Left and Shift+Right to `TableBody.Scroll`.
- **One settings row and a one-row icon.** `Wording.SettingsRows` returns one row; `VantageIcon.Height` becomes 1 and `Fit` sizes it from the cell aspect (two cells on Windows Terminal's 10 x 20 cell, clamped to 1 to 3); `StripTop` moves from 2 to 1. The icon's pixels stay 48 x 48; the terminal scales them to the cell.
- **Counts and title.** `Wording.ConfigCounts` gives `N settings` and, when any setup row says to run something, `N to set up` in amber; the table's title is the usual `TableTitle` (`10 rows, selected: Worktree root`).

## Risks / Trade-offs

- [Removing `w` and `,` breaks muscle memory] → a pre-1.0 minor change, named in the README's keys table and the release notes; Left and Right are the more discoverable keys.
- [Shift+Left and Shift+Right may be bound by a terminal] → Windows Terminal 1.25 binds neither by default (to check in the owner's terminal check); the help lists them.
- [A one-row icon is small] → the owner chose it over keeping two rows (30 October 2026); `Fit` keeps its square shape from the reported cell size.
- [Saving one value while another tab's scan runs] → `CurrentSettings` is read per scan already; the save applies to the next scan, as the dialog's did.
- [The Claude settings read touches a file a running Claude may be writing] → read only, through the existing adapter, tolerant of a partial file (an unreadable file is that row's "could not be read").

## Migration Plan

`config.json` is unchanged, so an earlier version reads what this one writes and the other way round. Rolling back is installing the earlier release; nothing on disc changes.

## Open Questions

- Whether Shift+Left and Shift+Right reach Vantage in every terminal the owner uses (checked in the terminal check; the fallback would be `<` and `>`).
