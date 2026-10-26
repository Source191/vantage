# Proposal

## Why

The README's Install section names the prerequisites in one line but not how to get them, and Claude Code's WinGet package does not update itself and lagged behind the minimum version (2.1.302 against 2.1.309 on 2026-10-26).

## What Changes

- A Prerequisites section before Install, listed in Contents: the `winget` commands for Windows Terminal, PowerShell 7, the .NET 10 SDK and git, Claude Code's native installer, why Vantage needs each, the optional GitHub and Azure CLIs for the PR column, and a one-line check.
- Install's prerequisite line becomes a link to the new section.
- No behaviour change, no spec change, no version bump: the README is not one of the files that require one.