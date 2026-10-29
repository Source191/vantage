# Tasks

## 1. README Prerequisites

- [x] 1.1 Add `--source winget` to every `winget install` line and say why in one sentence; verify in a fresh Windows Sandbox (Store source left in place) that every Prerequisites line exits 0
- [x] 1.2 Add the user PATH line after the Claude Code installer and say why in the Claude Code bullet; verify in the same Sandbox run that the check line's `claude --version` prints a version in a new terminal, and `openspec validate readme-clean-machine` passes

## Workflow follow-up

- Archive the change in the same pull request, open it and merge it once `check` passes.
