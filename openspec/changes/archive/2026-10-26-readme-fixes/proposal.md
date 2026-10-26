# Proposal

## Why

Two mistakes in the README survived the changes that touched it: the `vantage here` example still passes `-d "Flaky orders suite"`, an option `remove-session-descriptions` removed (the command now exits 2 on it), and the Settings section reads "The The `e` key".

## What Changes

- The `vantage here` example drops `-d "Flaky orders suite"`.
- The Settings section reads "The `e` key opens the selected row's folder in this editor."
- No behaviour change, no spec change, no version bump: the README is not one of the files that require one.
