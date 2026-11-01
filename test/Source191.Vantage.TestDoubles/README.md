# Source191.Vantage.TestDoubles

The fakes both test projects share, hand-written rather than from a mocking library, one per boundary: the process runner, process registry, Windows Terminal, terminal console and console screen, transcripts, folders, state and settings, clipboard, desktop, editor locator, package versions, pull requests, Claude settings, crash file, terminal fragment and a diagnostic sink. They are the only test doubles. No test framework here.

- **References Infrastructure:** `TemporaryGitRoot`, a real throwaway repository for the adapter tests, is built through the real process runner.
