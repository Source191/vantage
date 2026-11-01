# HeadlessTerminal.Tests

Tests for the [HeadlessTerminal](../HeadlessTerminal/README.md) tool, kept beside it rather than in the application suites.

- **`InvariantGlobalization`:** the tests run in the invariant globalisation mode the shipped tool runs in, so a call that behaves differently there (such as `CompareOptions.NumericOrdering`, which invariant mode ignores) fails here rather than only for users. It also switches off the culture analysers for this project's own code; the libraries under test keep them.
