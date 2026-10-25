# Tasks

## 1. The spike at the terminal

- [ ] 1.1 From a PowerShell tab outside Claude Code, open four tabs and note each one's title and whether a second tab or command appeared: `wt.exe -w 0 new-tab --title 'a;b' pwsh.exe -NoExit`, the same with `'a\;b'`, with `'say "hi"'` and with `'say \"hi\"'`. Verify by the titles shown; record the four outcomes in this file.
- [ ] 1.2 In a trusted repository, run `claude --bg -n 'a;b'`, then `claude agents --json` and note whether the name is listed as typed; stop and remove the throwaway job with `claude stop <id>` and `claude rm <id>`. Verify by the listing; record the outcome here.
- [ ] 1.3 Only if 1.1 showed a title carrying `;` or `"`: open such a tab, then from Vantage select a session whose tab has that title and press `o`. Verify the tab is focused; record the outcome here.

## 2. Recording the fact

- [ ] 2.1 Add the verified line to CLAUDE.md's "Windows Terminal facts" (and to "Claude Code CLI facts" if the CLI refused the name); make `SessionName`'s doc comment, its `TryParse` summary and the `WindowsTerminalArguments` comment state the same reason. Verify `grep -rn "cannot pass them safely\|would refuse" src` shows one consistent reason, and raise `Version` by a patch if any `src\` file changed.
- [ ] 2.2 If 1.1 to 1.3 all passed: propose `relax-session-name-rule` with `/opsx:propose`, carrying the `session-launch` delta for "A session name is mandatory", the adapter's escaping and the removal of `NameProblem.UnsafeCharacters`. Verify `openspec validate relax-session-name-rule` passes. If any step failed, record here that the rule stays and why.

## Workflow follow-up

- Independent of the other agreed changes; one pull request, archived in it.
- Needs the product owner at the terminal; nothing here can run inside a Claude Code session.
