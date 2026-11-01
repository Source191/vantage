---
paths:
  - "**/Wording*.cs"
  - "**/CommandLine*.cs"
  - "**/WhereLabel*.cs"
  - "**/DetailLayout*.cs"
  - "**/StatusLayout*.cs"
  - "**/ConfigScreenDefinition*.cs"
  - "**/WorktreeSlug*.cs"
  - "**/NumberAwareOrder*.cs"
  - "**/GitCli*.cs"
  - "**/AzureDevOpsPullRequests*.cs"
  - "**/*.csproj"
---

# Locale and translation (checked 1 November 2026, .NET 10.0.403 SDK, git 2.56.0.windows.3)

The rules every file follows (invariant culture, ordinal comparisons, times converted by zone) are in `CLAUDE.md`'s C# idioms; these are the details behind them and what other languages will need.

- Windows zone ids (`GMT Standard Time`, `E. Africa Standard Time`) resolve in invariant mode, from the registry, so tests use them, with a summer date wherever the UK must differ from UTC.
- Always invariant, whatever language the screen speaks: `config.json` and the state files, the diagnostic lines, every argument given to git, claude, gh, az, wt and pwsh, the reading of their output, and `ITextValue` parsing.
- Translating means turning invariant mode off in the tool project, which loads ICU and makes every culture-aware call follow the user's culture (decide each one, and run the tests under a culture such as `tr-TR`, whose dotted and dotless i break case-insensitive culture comparisons); moving `Wording` and its partials and the command line's descriptions to resources, with plurals per language rather than `WordingOfOutcomes.Count`'s added s; and formatting dates and numbers in the user's culture (`HH:mm`, `d MMM HH:mm`, `d MMMM yyyy HH:mm` and `F1` are invariant on purpose today). System.CommandLine's own help, parse errors and suggestions stay English, as they cannot be replaced.
- Text is measured and cut by UTF-16 characters, not terminal cells (`WhereLabel`, `DetailLayout`, `StatusLayout`, `ConfigScreenDefinition.Middle`), so Chinese and Japanese, two cells a character, overflow their columns and a cut can split an emoji's surrogate pair.
- `WorktreeSlug` keeps only `a-z0-9`, so a name in Cyrillic or Chinese is refused as needing letters and `café` becomes `caf`.
- az writes the ANSI code page (cp1252 here) when its output is piped and ignores `PYTHONIOENCODING` and `PYTHONUTF8` (`az.cmd` starts Python with `-I`), so only ASCII may be read from it, as now.
- Git for Windows ships no translations, so `GitCli`'s English matches ("Permission denied", "Directory not empty") hold, but a translated git would need `LC_ALL=C`, which `IProcessRunner` cannot set yet. `git status` with `core.quotePath=false` still quotes a name holding a double quote, a backslash or a control character (`-z` would not).
- Key letters follow English words (`c` copy, `f` folder, `s` solution), and the fixed column and dialog widths, and the 120 x 36 floor, are sized for English, which German and many other languages outrun.
- In invariant mode culture-aware comparisons are ordinal and every `CompareOptions` but `IgnoreCase` is ignored without an exception, which is why `NumericOrdering` gave way to `Domain\NumberAwareOrder`. `InvariantGlobalization` is set in the tool project and the three test projects, so the tests run as the tool does, and never in a library, where it would only switch off CA1304, CA1305, CA1309, CA1310 and CA1311 (each project's README says so).