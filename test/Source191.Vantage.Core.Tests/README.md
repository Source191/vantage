# Source191.Vantage.Core.Tests

The fast suite, about two seconds: domain rules, reducers, use cases and read models over in-memory fakes. No temporary folders, no git and no processes.

- **References Infrastructure:** so sociable tests can assemble the real adapters over a fake process runner.
- **References Tui:** so the architecture test can inspect its assembly, `DialogEffectsShould` can ask which effects have a dialog and `ConfigScreenDefinitionShould` can read a Config tab cell (Tui makes its internals visible to this project for that).
- **`InvariantGlobalization`:** the tests run in the invariant globalisation mode the shipped tool runs in, so a call that behaves differently there (such as `CompareOptions.NumericOrdering`, which invariant mode ignores) fails here rather than only for users. It also switches off the culture analysers for this project's own code; the libraries under test keep them.
