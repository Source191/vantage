# Design

## Context

See proposal.md, Why. The adapter Base64-encodes each tab's whole script for `pwsh -EncodedCommand`, so `wt.exe` never parses it. The only parser that matters is PowerShell's tokenizer reading the script. Two places build script text: `PowerShellScript` (`Quote` and `Invoke`, used by `OpenTabHandler` and `TrustRecovery`) and the `Write-Host` line in `TrustRecovery.TabFor`. The worktree shell tab's script is the constant `Clear-Host`, and its folder goes to `wt.exe` as the starting directory argument.

## Goals / Non-Goals

**Goals:** every value inside a tab script is a single-quoted literal that PowerShell reads back byte for byte. That is proven against real `pwsh`, not only against the tokenizer's rules as written down.

**Non-Goals:** moving the helper or the trust script, which `consolidate-helpers` turns into `TabScripts`; quoting for other shells; validating names, since a name with a quote is legitimate and must work.

## Decisions

- **Double each quote character with itself.** PowerShell treats `'`, U+2018, U+2019, U+201A and U+201B alike as single quotes. Inside a single-quoted string, two quote characters in a row stand for one. Doubling each character with itself means the literal always ends with the ASCII `'` that `Quote` writes, and the text comes back with its own characters, so `O’Neill` stays curly.
  - **Rejected, normalising curly quotes to ASCII:** it changes the text, so a path with a curly apostrophe would name a different folder.
  - **Rejected, a here-string (`@'...'@`):** it needs line breaks and still ends at `'@` at the start of a line.
- **The trust message goes through `Quote` as one value.** `Write-Host {Quote(message)} -ForegroundColor Cyan` keeps the sentence where it is, and the folder name is just part of a literal.
- **Real-pwsh test in Infrastructure.Tests.** `PowerShellQuotingShould` runs `pwsh -NoProfile -NonInteractive -EncodedCommand` through the real `ProcessRunner` with `Write-Output (Quote(text))`. The text holds every quote kind, a backtick, a `$` and a would-be `; Remove-Item` command, and the test asserts the output equals the input. That is the same encoding the tabs use. CI's Windows runner has PowerShell 7, as the developer machines do. The Core tests pin the exact quoted strings, so a regression shows in the fast suite too.

## Risks / Trade-offs

- [PowerShell adds more quote characters in a later version] → the real-pwsh test fails only for characters it includes; the list lives in one constant beside a comment naming the tokenizer's set, so adding one is a one-line change.
- [The real-pwsh test is slower than the Core suite] → it is one process start in the adapter suite, which already starts git and processes.
