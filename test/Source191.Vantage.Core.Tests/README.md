# Source191.Vantage.Core.Tests

The fast suite, about two seconds: domain rules, reducers, use cases and read models over in-memory fakes. No temporary folders, no git and no processes.

- **References Infrastructure:** so sociable tests can assemble the real adapters over a fake process runner.
- **References Tui:** so the architecture test can inspect its assembly and `DialogEffectsShould` can ask which effects have a dialog.
