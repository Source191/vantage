# Source191.Vantage.Infrastructure.Tests

The adapter suite: real git, real processes, real files and the console, on throwaway folders.

`Contract\Snapshots` holds the Claude Code help snapshots that `ClaudeHelpSurfaceShould` compares against; the contract tests and how to accept a new snapshot are in `CLAUDE.md`.

- **`InvariantGlobalization`:** the tests run in the invariant globalisation mode the shipped tool runs in, so a call that behaves differently there (such as `CompareOptions.NumericOrdering`, which invariant mode ignores) fails here rather than only for users. It also switches off the culture analysers for this project's own code; the libraries under test keep them.
