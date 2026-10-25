# Design

## Context

`UpdateCheck` takes an `enabled` function and passes it to `UpdateCheckRule`, which refuses to check or offer when it is false. `Program.cs` supplies `() => settings.Value.CheckForUpdates`, so the setting already stops the request, the write and the notice, and the `U` key already says the check is off. The harness starts Vantage through `DriverOptions.ChildEnvironment`, which copies the harness's own environment and adjusts two terminal variables.

## Goals / Non-Goals

**Goals:** a harness run never contacts nuget.org or writes `update-check.json`; the rule for "set" is tested without touching the real environment.

**Non-Goals:** a UI for the variable; reading it on every use.

## Decisions

- **Reuse the `enabled` seam.** The variable is read once in `Program.cs` and folded into the function: `settings.Value.CheckForUpdates && !optOut.Applies`. No change to `UpdateCheck` or `UpdateCheckRule`, whose tests already pin what "disabled" means.
- **`UpdateCheckOptOut` in `Domain`** wraps the variable's text: `VariableName` and `Applies` (true for any value with a non-whitespace character; unset, empty or blank means the check runs as before). It is tested with plain strings, so no test sets the process environment.
- **Name `VANTAGE_NO_UPDATE_CHECK`**, in the style of `VANTAGE_LOG`, naming the thing turned off.
- **The harness sets it unconditionally** in `ChildEnvironment`, overriding any inherited value with `1`; the harness never wants a check. A test on `ChildEnvironment` pins it.

## Risks

- A user who sets the variable globally gets no notice and no `U`: that is the intent, and the footer already says the check is off.
