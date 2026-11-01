# Proposal

## Why

Vantage annotates a Try method's `out` parameter two ways. Thirteen use `[MaybeNullWhen(false)] out T` with a non-nullable declared type, the shape `ITextValue<TSelf>.TryParse` declares and the base class library's `IParsable<TSelf>` and `Dictionary.TryGetValue` use. Four use `[NotNullWhen(true)] out T?`, chosen in null-free-screens and null-free-follow-ups. A test compile on 1 November 2026 showed the two give identical warnings to callers and inside the method body, so the difference is spelling only. The owner chose `[MaybeNullWhen(false)]` throughout, because it keeps the declared type non-nullable, which matches the null programme's rule that Vantage's own members are not `T?`. An alias such as `NullWhen` was considered and declined.

## What Changes

- **The four outliers take the house shape:** `ClaudeVersion.TryParse`, `ConfigView.TryPathOf`, `TabKeys.TryFor` and `KeyMapping.TryFrom` declare `[MaybeNullWhen(false)] out T` with a non-nullable `T`. Their bodies and callers stay as they are.
- **`NullFreeModelsShould` stops accepting `[NotNullWhen(true)]`:** a nullable `out` parameter is exempt only when it carries `[MaybeNullWhen(false)]`, so a new `[NotNullWhen(true)] out T?` in `Domain`, `Ports` or `Application` fails the build's tests. Its probe moves the `NotNullWhen` example from the exempt shapes to a reported one.
- **`ITextValue<TSelf>.TryParse` explains the attribute:** a `<param>` comment says the value is null exactly when the method returns false, so a reader does not have to decode "maybe".
- **`CLAUDE.md`:** the house-shape sentence names only `[MaybeNullWhen(false)]`, and the C# idioms gain one line stating the shape and that callers declare `out T? x`.
- **Version:** `src\` changes, so `Version` rises by one preview step above main when the change is implemented.
- `skip_specs: true`: annotations, a test and documentation; no behaviour changes.

Out of scope:

- **An alias** such as `NullWhen` for `MaybeNullWhenAttribute`, declined by the owner.
- **A mechanical check over `Infrastructure` and `Tui`:** `NullFreeModelsShould` covers Core only, so `KeyMapping` is converted by hand and kept by the `CLAUDE.md` rule and review (see design.md).
- **Value-type `out` parameters:** none exist, and `MaybeNullWhen` has no effect on one.
- **Multiple Claude accounts**, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: an annotation refactor, a test rule and documentation, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `src\Source191.Vantage.Core\Application\Diagnostics\ClaudeVersion.cs`, `Application\Views\ConfigView.cs`, `Application\Views\TabKeys.cs`, `Domain\ITextValue.cs` (comment only) and `src\Source191.Vantage.Tui\KeyMapping.cs`; `src\Source191.Vantage\Source191.Vantage.csproj` for the version.
- **Tests:** `test\Source191.Vantage.Core.Tests\Architecture\NullFreeModelsShould.cs`.
- **Docs:** `CLAUDE.md`.
