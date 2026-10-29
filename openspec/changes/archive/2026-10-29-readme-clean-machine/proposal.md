# Proposal

## Why

Following the README's Prerequisites on a clean Windows 11 (Windows Sandbox) failed in two places. Every `winget install` line stopped with "Failed when searching source: msstore" (0x8a15003b) although each id is in the winget source, because winget will not choose a package while a source errors. And the Claude Code installer put `claude.exe` in `~\.local\bin` without adding it to the PATH, so the README's own check line (`claude --version`) failed in a new terminal.

## What Changes

- Every `winget install` line in the README (Prerequisites and the optional PR-column tools) gains `--source winget`, with one sentence saying why.
- A line after the Claude Code installer adds `~\.local\bin` to the user PATH, and the Claude Code bullet says the installer does not.

Out of scope: any change to the tool (Vantage already looks in `~\.local\bin` itself), a clean-machine test in CI, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Documentation only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `README.md`: the Prerequisites section. No `Version` bump: the README ships in the package but is not a tool path for `Assert-VersionBumped.ps1`, so the next release carries it.
