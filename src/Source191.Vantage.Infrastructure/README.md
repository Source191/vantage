# Source191.Vantage.Infrastructure

The adapters behind Core's ports: git, the Claude CLI, Windows Terminal, processes, files, state and diagnostics. Depends on Core only.

- **`AllowUnsafeBlocks`:** the `[LibraryImport]` source generator behind the Win32 calls in `ProcessRegistry` and `ConsoleRecovery` refuses to run without it, and its generated code is the only code that uses it. The project writes no `unsafe` code of its own, and IDE0380 fails the build on an `unsafe` modifier nothing needs.
