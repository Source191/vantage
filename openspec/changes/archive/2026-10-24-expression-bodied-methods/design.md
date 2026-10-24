# Design

## Context

`Directory.Build.props` sets `EnforceCodeStyleInBuild` and `TreatWarningsAsErrors`, so an IDE rule at warning severity fails the build. `.editorconfig` at the root governs every project; the nearest `.editorconfig` wins key by key, so a folder-level file can override one rule for the projects beneath it.

## Goals / Non-Goals

**Goals:**
- One mechanical sweep, then the analyser keeps it that way.

**Non-Goals:**
- Hand-reformatting multi-line single statements into expression bodies.
- Touching test code.

## Decisions

- **`when_on_single_line` over `true`:** `true` would also convert multi-line `switch` expressions and constructor calls, where a block body with `return` reads at least as well. `when_on_single_line` matches the agreed scope exactly and lets the analyser do the deciding.
- **Folder-level overrides for tests over severity in each csproj:** `test\.editorconfig` covers `Core.Tests`, `Infrastructure.Tests` and `TestDoubles` in one file; `tools\HeadlessTerminal.Tests` needs its own because `tools\HeadlessTerminal` is in scope. Each sets `csharp_style_expression_bodied_methods = false:silent` and `dotnet_diagnostic.IDE0022.severity = silent`. Neither file is `root = true`, so everything else is still inherited.
- **`dotnet format` over hand edits:** the code fix is Roslyn's own, so generic constraints, `async` methods and explicit interface implementations come out right; the diff is reviewed rather than written.
- **No new tests:** no behaviour changes; the full suite, the format check and the hand-off check are the safety net.

## Risks / Trade-offs

- [`dotnet format` leaves a method it cannot fix and the build fails] → convert it by hand in the same commit.
- [`async Task M() { await X(); }` becomes `async Task M() => await X();`, keeping a state machine that could be dropped] → leave it; changing `async` to a returned `Task` alters exception timing and is not a style change.
- [Line length grows past the formatter's comfort] → `when_on_single_line` only fires when the body already fits on one line.
