# Source191.Vantage.Infrastructure

The adapters behind Core's ports: git, the Claude CLI, Windows Terminal, processes, files, state, pull requests (gh and az), package versions (the nuget.org update check) and diagnostics. Depends on Core only.

- **`AllowUnsafeBlocks`:** the `[LibraryImport]` source generator behind the Win32 calls in `ProcessRegistry` and `ConsoleRecovery` refuses to run without it, and its generated code is the only code that uses it. The project writes no `unsafe` code of its own, and IDE0380 fails the build on an `unsafe` modifier nothing needs.
- **No `InvariantGlobalization`:** only an executable's `runtimeconfig.json` carries the setting at run time, so on a library it would do nothing but switch off the culture analysers (CA1304, CA1305, CA1309, CA1310, CA1311), which then check this project's code. The tool project, `src\Source191.Vantage`, keeps it, so the shipped tool still runs invariant.
