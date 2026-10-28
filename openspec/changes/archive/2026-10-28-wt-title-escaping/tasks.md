# Tasks

## 1. The spike at the terminal

- [x] 1.1 From a PowerShell tab outside Claude Code, open six tabs and note each one's title and whether a second tab or command appeared: `wt.exe -w 0 new-tab --title 'a;b' pwsh.exe -NoExit`, the same with `'a\;b'`, with `'fix a;b'`, with `'fix a\;b'`, with `'say "hi"'` and with `'say \"hi\"'`. The two with a space are passed quoted, as the adapter passes a real session name. Verify by the titles shown; record the six outcomes in this file.
  Outcome (28 October 2026, Windows Terminal 1.25.2733, PowerShell 7.6.9): `'a;b'` opened two tabs, `a` and `b` failing with "error 2147942402 (0x80070002) when launching `b pwsh.exe -NoExit`"; `'a\;b'` one tab titled `a;b`; `'fix a;b'` two tabs, `fix a` and the same failing `b`, so quoting does not protect a semicolon; `'fix a\;b'` one tab titled `fix a;b`; `'say "hi"'` one tab titled `say "hi"`; `'say \"hi\"'` one tab titled `say \"hi\"` (the backslashes kept, so a quote needs no escape).
- [x] 1.2 In a trusted repository, run `claude --bg -n 'a;b'`, then `claude agents --json` and note whether the name is listed as typed; stop and remove the throwaway job with `claude stop <id>` and `claude rm <id>`. Verify by the listing; record the outcome here.
  Outcome (28 October 2026, CLI 2.1.312): ticked with no remark in the test plan, so the name was listed as typed; the CLI is not a reason for the rule.
- [x] 1.3 Only if 1.1 showed a title carrying `;` or `"`: open such a tab, then from Vantage select a session whose tab has that title and press `o`. Verify the tab is focused; record the outcome here.
  Outcome (28 October 2026): could not be run. The wizard refuses `test;works` with the UnsafeCharacters sentence (which overflows the dialog's width and is cut off after "cannot pass them"), and `SessionName.TryParse` drops a listed name holding `;`, so no row can carry such a title until the rule is relaxed. Deferred, by the product owner's decision, to the manual checks of `relax-session-name-rule`.

## 2. Recording the fact

- [x] 2.1 Add the verified line to CLAUDE.md's "Windows Terminal facts" (and to "Claude Code CLI facts" if the CLI refused the name); make `SessionName`'s doc comment, its `TryParse` summary and the `WindowsTerminalArguments` comment state the same reason. Verify `grep -rn "cannot pass them safely\|would refuse" src` shows one consistent reason, and raise `Version` by a patch if any `src\` file changed.
  Outcome: CLAUDE.md's "Windows Terminal facts" carries the line; `SessionName`, its `Validate` and `TryParse` summaries, `WindowsTerminalArguments` and the three resume handlers now name the semicolon splitting, not the CLI; `Version` is 0.16.2-preview.4. The one other match, `Wording`'s UnsafeCharacters sentence, is now imprecise about the double quote but is quoted exactly by `session-launch`'s spec, so it stays until the rule is relaxed.
- [x] 2.2 If 1.1 to 1.3 all passed: propose `relax-session-name-rule` with `/opsx:propose`, carrying the `session-launch` delta for "A session name is mandatory", the adapter's escaping and the removal of `NameProblem.UnsafeCharacters`. Verify `openspec validate relax-session-name-rule` passes. If any step failed, record here that the rule stays and why.
  Outcome: 1.1 and 1.2 passed and 1.3 could not run, so the product owner chose to propose `relax-session-name-rule` now, relaxing both characters, with 1.3's `o` check, a backslash before a semicolon, the OSC 2 re-title and the wizard's cut-off sentence carried into its tasks. `openspec validate relax-session-name-rule` passes.

## Workflow follow-up

- Part of P13 in `public-release`'s sequence: the spike runs during the final manual test session.
- Independent of the other agreed changes; one pull request, archived in it.
- Needs the product owner at the terminal; nothing here can run inside a Claude Code session.
