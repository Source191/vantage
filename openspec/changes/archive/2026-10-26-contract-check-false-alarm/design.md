# Design

## Context

CI tooling only; see proposal.md. The colour cannot be reproduced locally, even with `GITHUB_ACTIONS` and `CI` set, so it was diagnosed from the run's `contract-results` artefact.

## Decisions

- **Strip rather than `--no-ansi`:** Microsoft.Testing.Platform's `--no-ansi` could not be shown to change the runner's output, because the colour never appears locally; stripping `\e\[[0-9;?]*[A-Za-z]` from the file can be proved against the real artefact. The `Tee-Object` stays, so the live log is still coloured.
- **The error goes in the file:** the issue step already quotes `^::error::` lines from `contract-output.txt`, so writing the step's own error there is the smallest change that gets it into the issue.
- **A fallback sentence:** whatever path leads to the issue step, its fenced block is never empty.
