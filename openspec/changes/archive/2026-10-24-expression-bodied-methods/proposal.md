# Proposal

## Why

About 95 methods outside the test projects are a single statement on a single line wrapped in braces, because `.editorconfig` sets `csharp_style_expression_bodied_methods = false`. Four lines of ceremony for one line of meaning; an expression body reads as what it is, a calculation or a delegation.

## What Changes

- `.editorconfig`: `csharp_style_expression_bodied_methods = when_on_single_line` and `dotnet_diagnostic.IDE0022.severity = warning`, so the build (code style enforced, warnings as errors) fails on a new block-bodied single-line method.
- `test\.editorconfig` and `tools\HeadlessTerminal.Tests\.editorconfig`: the test projects keep `false:silent`; test code is not swept or enforced.
- Every single-line method in `src\` and `tools\HeadlessTerminal\` becomes expression-bodied (`dotnet format style --diagnostics IDE0022`).
- CLAUDE.md Code rules: single-line methods are expression-bodied.

Out of scope: test projects (`test\*`, `tools\HeadlessTerminal.Tests`), constructors, local functions, operators, single statements that span several lines (`switch` expressions, multi-line `new`), multiple Claude accounts.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Style only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `.editorconfig` and two new folder-level `.editorconfig` files.
- `src\Source191.Vantage.Core`, `src\Source191.Vantage.Infrastructure`, `src\Source191.Vantage.Tui`, `tools\HeadlessTerminal`: mechanical edits to method bodies; no signature or behaviour changes.
- CLAUDE.md.
