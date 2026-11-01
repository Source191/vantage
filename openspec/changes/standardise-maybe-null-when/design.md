# Design

## Context

See proposal.md for why. The four outliers and their callers:

| Method | Layer | Callers |
|---|---|---|
| `ClaudeVersion.TryParse(string?, out Version?)` | Application | `Program.cs`, `ClaudeVersionShould` |
| `ConfigView.TryPathOf(ConfigRow, out string?)` | Application | `ConfigView.WithPath`, `ConfigViewState` |
| `TabKeys.TryFor(Key, out ViewEffect?)` | Application | the three views' reducers, `TabKeysShould` |
| `KeyMapping.TryFrom(GuiKey, out Key?)` | Tui | `TableBody.OnKey` |

Every caller declares `out T? x`. Every body assigns null on the false path and returns `x is not null`, as `BranchName.TryParse` already does. `ClaudeVersion.TryParse` forwards to `Version.TryParse`, whose own parameter is `[NotNullWhen(true)] out Version?`.

`NullFreeModelsShould.IsOutUntilParsed` exempts a nullable `out` parameter carrying either attribute, which is why the three Application outliers pass today. `NullabilityInfoContext` reads a `[MaybeNullWhen(false)] out T` parameter as `ReadState` Nullable (checked 1 November 2026), so the `MaybeNullWhen` branch of the exemption is still needed after this change.

## Goals / Non-Goals

**Goals:**

- One spelling for a Try method's `out` parameter across `src`, kept mechanically in Core.

**Non-Goals:**

- Changing any Try method's name, parameters, body or callers.
- Changing the `TryParse` house-shape rule for the `string?` input parameter.

## Decisions

**Signatures only.** Each outlier becomes `[MaybeNullWhen(false)] out T name` with `T` non-nullable. A compile on 1 November 2026 showed no new warning in the bodies, which assign null on the false path, nor in a method that forwards to `Version.TryParse`. Callers keep `out T? x`, because declaring a non-nullable local such as `out Version parsed` warns CS8600 in either form, so the existing call sites are already the right shape. Alternative: also change callers to `out var`, rejected because the house style is concrete types.

**`IsOutUntilParsed` accepts only `[MaybeNullWhen(false)]`.** Dropping the `NotNullWhen` branch makes `[NotNullWhen(true)] out T?` a reported nullable parameter in `Domain`, `Ports` and `Application`. The probe's `TryFind(int, [NotNullWhen(true)] out string?)` moves from `IHouseShapes` to a probe of its own, and `ExemptTheHouseShapes` asserts that one is reported as `TryFind(found)`. Alternative: leave the test accepting both, rejected because the standard would then rest on review alone.

**`KeyMapping` is converted but not checked.** `NullFreeModelsShould` reflects over Core only, and Tui is where Terminal.Gui's nulls arrive, so widening it is a separate decision. Alternatives considered: a source-text fact beside `OneTypePerFileShould` that fails for `NotNullWhen` anywhere under `src`, or the banned-API analyser. Both add a mechanism for one attribute in one project, so the `CLAUDE.md` rule and review keep Tui.

**The comment lives on the interface.** `ITextValue<TSelf>.TryParse` gains `<param name="value">` saying the value is null exactly when the method returns false. The interface is where a reader meets the shape first; the implementations and the four converted methods keep their own summaries, which already say what false means.

## Risks / Trade-offs

- [A Tui or Infrastructure method reintroduces `NotNullWhen`] → the `CLAUDE.md` idiom line names the shape, and review catches it; a source-text fact remains the fallback if it recurs.
- [An analyser rule flags `MaybeNullWhen` on a value type in future] → none exist today, and the idiom line says the shape is for reference types.
