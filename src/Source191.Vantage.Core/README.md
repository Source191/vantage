# Source191.Vantage.Core

`Domain`, `Ports` and `Application`: the rules, the boundary interfaces, the reducers and the use cases. No file system, no processes and no Terminal.Gui; the architecture test in `Core.Tests` enforces the layering.

Two packages: Microsoft.Extensions.Logging.Abstractions, only for `Application\Diagnostics\TerminalGuiBridge` (the `ILogger` Terminal.Gui writes to), and System.CommandLine for the command line in `Application\CommandLine`.
