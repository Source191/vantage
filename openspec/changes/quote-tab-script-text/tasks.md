# Tasks

## 1. Quote every single-quote character

- [x] 1.1 `PowerShellScriptShould.DoubleEveryCharacterPowerShellReadsAsASingleQuote`: a theory over `'`, U+2018, U+2019, U+201A and U+201B, each doubled with itself inside the literal (red first); make `PowerShellScript.Quote` double all five; verify the Core suite passes
- [x] 1.2 `PowerShellQuotingShould.PrintQuotedTextBackUnchanged` in Infrastructure.Tests: run real `pwsh -NoProfile -NonInteractive -EncodedCommand` through `ProcessRunner` on `Write-Output` of `Quote(text)`, where the text holds all five quote characters, a backtick, a `$name` and `'; Remove-Item C:\nowhere; '`; verify the output equals the text and that the test fails against the old `Quote`

## 2. The trust tab

- [ ] 2.1 `TrustRecoveryShould.KeepAFolderNameWithAnApostropheInsideTheMessage`: the trust tab for `C:\repos\Bob's tools` has a `Write-Host` line whose argument is `Quote` of the whole message (red first); build the line through `Quote`; verify the test and the existing trust tests pass

## 3. Documentation

- [ ] 3.1 CLAUDE.md: the line on user text names `PowerShellScript.Quote` as the only way a value enters a tab script and the five characters it doubles; verify `openspec validate` passes for every open change

## Workflow follow-up

- One pull request: task groups 1 to 3, archived in the same pull request. It goes before `consolidate-helpers`, which moves the helper into `TabScripts`.
- Patch release: bump `Version` on its own pull request after the README's manual checks, as CLAUDE.md describes.
