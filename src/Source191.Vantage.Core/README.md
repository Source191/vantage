# Source191.Vantage.Core

`Domain`, `Ports` and `Application`: the rules, the boundary interfaces, the reducers and the use cases. No file system, no processes and no Terminal.Gui; the architecture test in `Core.Tests` enforces the layering.

Two packages: Microsoft.Extensions.Logging.Abstractions, only for `Application\Diagnostics\TerminalGuiBridge` (the `ILogger` Terminal.Gui writes to), and System.CommandLine for the command line in `Application\CommandLine`.

- **No `InvariantGlobalization`:** only an executable's `runtimeconfig.json` carries the setting at run time, so on a library it would do nothing but switch off the culture analysers (CA1304, CA1305, CA1309, CA1310, CA1311), which then check this project's code. The tool project, `src\Source191.Vantage`, keeps it, so the shipped tool still runs invariant.
