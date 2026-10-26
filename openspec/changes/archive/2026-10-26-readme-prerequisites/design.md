# Design

## Context

Documentation only; see proposal.md.

## Decisions

`winget` for everything it carries, because Windows 11 ships with it. Claude Code through its native installer rather than WinGet: it installs to `~\.local\bin\claude.exe`, where Vantage looks, and updates itself, while the WinGet package does not. Package ids were checked with `winget show --id <id> --exact` on 2026-10-26.