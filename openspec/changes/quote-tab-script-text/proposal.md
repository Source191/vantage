# Proposal

## Why

Two places let text escape from the PowerShell literal it is meant to sit in.

- **The quoting helper misses curly quotes.** Every value in a tab's script goes through `PowerShellScript.Quote`, which doubles the ASCII apostrophe only. PowerShell also ends a single-quoted string at U+2018, U+2019, U+201A and U+201B. So a value holding a curly quote, such as a profile path `C:\Users\O’Neill`, closes the literal early, and whatever follows runs as code.
- **The trust tab is not quoted at all.** `TrustRecovery.TabFor` puts the repository folder name straight into a single-quoted `Write-Host` message without the helper. A folder named `Bob's tools` breaks the trust tab's script, and a folder named to carry commands runs them.

The CLAUDE.md rule that user text is never interpolated into PowerShell source does not hold today. Fixing it now means `consolidate-helpers` starts from a correct helper when it moves it into `TabScripts`.

## What Changes

- **All four curly quotes:** `PowerShellScript.Quote` doubles every character PowerShell reads as a single quote: the ASCII apostrophe and U+2018 to U+201B.
- **The trust tab:** `TrustRecovery.TabFor` builds its message line through `Quote`, so the folder name is literal text.
- **Proof against PowerShell itself:** an adapter test runs real `pwsh` on quoted text that holds every kind of quote, plus a would-be command, and checks the text comes back unchanged.
- **CLAUDE.md:** the rule names the helper and the characters it covers.

Out of scope:
- **Double quotes and backticks:** they have no meaning inside a single-quoted string, so they need no handling.
- **Tab titles and starting directories:** `wt.exe` receives these as separate arguments, not script source.
- **Moving the helper:** `TabScripts` is `consolidate-helpers`' job.
- **Multiple Claude accounts:** a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: a new requirement that a tab Vantage opens for a session runs its values as literal text, whatever characters they hold.
- `session-launch`: "Untrusted workspaces are handled" gains a scenario for a folder whose name holds an apostrophe.

## Impact

- **Core:** `Application\PowerShellScript.cs` and `Application\Launch\TrustRecovery.cs`.
- **Tests:** new cases in `PowerShellScriptShould` and `TrustRecoveryShould`, and a new `PowerShellQuotingShould` in Infrastructure.Tests, which runs `pwsh` as the other adapter tests run git.
- **Docs:** CLAUDE.md's Terminal.Gui facts line about user text.
- **Release:** a patch version bump on its own pull request, since this is a fix.
