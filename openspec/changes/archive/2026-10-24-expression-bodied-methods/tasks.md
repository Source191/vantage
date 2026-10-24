# Tasks

## 1. Rule

- [x] 1.1 Root `.editorconfig`: `csharp_style_expression_bodied_methods = when_on_single_line` and `dotnet_diagnostic.IDE0022.severity = warning`; `test\.editorconfig` and `tools\HeadlessTerminal.Tests\.editorconfig` set both back to silent and `false`; verify `dotnet build -c Debug` fails with IDE0022 only in `src\` and `tools\HeadlessTerminal\`

## 2. Sweep

- [x] 2.1 `dotnet format style --diagnostics IDE0022`; review the diff (generic constraints, `async` methods, interface implementations); verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass and no test file changed
- [x] 2.2 Verify `dotnet run --project tools\HeadlessTerminal -- check` passes from `tools\HeadlessTerminal`

## 3. Documentation

- [x] 3.1 CLAUDE.md Code rules: single-line methods are expression-bodied, enforced by IDE0022; verify `openspec validate expression-bodied-methods` passes

## Workflow follow-up

- One pull request: commit the rule and the sweep together (the rule alone does not build), the documentation second. Archive the change in this pull request.
