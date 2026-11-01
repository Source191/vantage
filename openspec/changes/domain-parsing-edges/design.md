# Design

## Context

See proposal.md for why. The code as it stands: `RemoteRepository.cs:54` is the GitHub pattern, with no userinfo group, and `:57-60` the Azure DevOps pattern, with `(?:[^@/\s]+@)?` on both https forms. `ReleaseVersion.cs:12` is a public positional record; `TryParse` (`:16-34`) checks the three numbers through `Number` (`:73`, `int.TryParse` with `NumberStyles.None`, which admits leading zeros) and only that the prerelease is non-empty, so its identifiers are first read in `CompareIdentifier` (`:102-111`). `ClaudeCodeSupport.cs:11` is the one production `new ReleaseVersion(…)`. `WhereLabel.Inside.Fit` (`WhereLabel.cs:42-60`) tries whole, then the repository elided when `room >= 3`, then `Ellipsis + Separator + CutEnd(Place, …)` whatever the width; `CutEnd` (`:94`) answers `…` for a width of 0 and `ElideMiddle` (`:62-73`) slices below 0 at that width. `BatchFileArgument.cs:17` is `^[A-Za-z0-9 ._/-]+$`, whose `$` matches before a final `\n`. `ClaudeCli.cs:98-103` holds `LeadingVersion` and `BackgroundedLine`, used at `:26` and `:82-88`, beside `JobId.IsJobId` (`JobId.cs:39`) and `Application\Diagnostics\ClaudeVersion.TryParse`. `Architecture\ParsingShould.FindEveryParser` already lists every Domain type with a `TryParse`; `TextValuesShould.HaveNoPublicConstructor` covers only the seven `ITextValue`s.

## Goals / Non-Goals

**Goals:**
- Each rule has one home: what a job id is in `JobId`, what a version text is in `ClaudeVersion` and `ReleaseVersion`, what az may be given in `BatchFileArgument`.
- `TryParse` is a real gate: nothing it admits compares unequal to something it says is equal, and nothing `Fit` answers is wider than asked.

**Non-Goals:**
- Making Domain regex-free as a rule, or banning `Regex` with an analyser.
- Reading any remote shape beyond the seven already read plus GitHub https with a user name.
- Changing what the header or Config tab shows for a version.

## Decisions

### GitHub https takes the same optional user name as Azure DevOps
The GitHub pattern's https alternative becomes `https://(?:[^@/\s]+@)?github\.com/`, the group Azure DevOps already uses, non-capturing, so only owner and name are ever taken from the URL. Alternative: `Uri.TryCreate` and `Uri.UserInfo`, which rejects the scp-style forms and would split the parser in two; see the next decision.

### The RemoteRepository and WorktreeSlug regexes stay
`Uri.TryCreate` reads four of the seven remote shapes and refuses `git@github.com:owner/name`, `git@ssh.dev.azure.com:v3/…` and `user@vs-ssh.visualstudio.com:v3/…` because `@` is not a scheme character, so a replacement needs `Uri` for four shapes and a split-and-check for three, a case-insensitive host test, `DefaultCollection` and `_git` skipped, `.git` and `/` trimmed: about forty lines where two declarations with named groups stand, and 23 passing cases to keep. A span loop that lowercases, collapses runs and trims a slug is ten lines against `[^a-z0-9]+`. Both are kept as the review's D-06 recommends.

### A prerelease identifier is checked as it is parsed
`TryParse` splits the prerelease on `.` and refuses any identifier that is empty or that is all digits with a leading zero (`text is "0" or [not '0', ..]` when every character is a digit); the same leading-zero rule applies to major, minor and patch through `Number`. `1.0.0-0` and `1.0.0-01a` still read (semantic versioning allows a zero and an alphanumeric identifier with a leading zero); no character-set rule is added. `CompareIdentifier` keeps using `Number`, now unable to meet a leading zero. Alternative: compare numeric identifiers as text once normalised, which would leave `TryParse` admitting non-versions.

### ReleaseVersion becomes a non-positional record with one trusted door
A positional record's constructor is public, so `ReleaseVersion` declares `Major`, `Minor`, `Patch` and `Prerelease` as get-only properties set by a private constructor; equality stays value-based and `IComparable` unchanged. `From(string text)` runs `TryParse` and throws `ArgumentException` otherwise, the spelling CLAUDE.md gives text Vantage already trusts, so `ClaudeCodeSupport.Minimum` reads `ReleaseVersion.From("2.1.309")` and inherits every check. Alternative: `Of(major, minor, patch)`, which `TextValuesShould` bans by name for text values and which would need its own negative-number guards. Tests build versions through `ReleaseVersions.Version(text)` in Core.Tests and `ReleaseVersion.From` in Infrastructure.Tests, which cannot see that helper.

### The constructor fact moves to the parsers
`ParsingShould` gains `HideTheConstructorBehindEveryTryParse`: every type `FindEveryParser` lists has no public constructor (`RemoteRepository`'s is protected, so it passes). `TextValuesShould.HaveNoPublicConstructor` then says the same of a subset and is removed, so the fact has one home. Alternative: a second theory in `TextValuesShould` over a wider type list, which would leave two.

### Fit degrades to a cut, never to an overrun
`WhereLabel` names `LeastElided = 3` (a character, the ellipsis, a character) and `LeastPlaced = 5` (the ellipsis, the separator, a character). `Inside.Fit`: whole when it fits; an empty place, or a width under `LeastPlaced`, elides the repository alone to the width; a room of at least `LeastElided` elides the repository before the tail; otherwise `… › ` and the place cut to the rest, which from `LeastPlaced` up is at least one character. `ElideMiddle` below `LeastElided` cuts at the end, and `CutEnd` answers an empty string for a width of 0, which also changes `Outside.Fit(0)` from `…` to nothing. Alternative: the review's `Math.Max(placeRoom, 1)` floor, which still overruns at widths 2 to 4.

### SearchValues for the batch-file rule
`BatchFileArgument` holds a `static readonly SearchValues<char>` of the 67 allowed characters (letters, digits, space, dot, underscore, hyphen, slash) and `IsSafe` is `text.Length > 0 && !text.AsSpan().ContainsAnyExcept(Plain)`; the class stops being `partial` and drops `System.Text.RegularExpressions`. Alternative: `RegexOptions.Multiline` off and `\z`, which keeps a regex for a character-set test.

### ClaudeCli reads words, not patterns
`InterpretStart` splits standard output on whitespace, skips to `backgrounded`, skips that word and the separator (whatever a mis-decoded middle dot became) and hands the next word to `JobId.TryParse`; a missing or malformed word falls through to the untrusted-workspace and failed cases as now. `VersionAsync` takes the first word of the trimmed output and answers `Printed(word)` only when `ClaudeVersion.TryParse` reads it (Adapters may depend on Application). Both are stricter than the regexes: `81cdb0f9x` and `2.1.310abc` are refused where `\b` and a leading match accepted them, which the facts pin. The class stops being `partial`.

## Risks / Trade-offs

- [A GitHub remote whose user name holds `/` or whitespace] → unchanged: it is refused as now, since the group is `[^@/\s]+`, and no such URL is a valid origin.
- [A recorded `update-check.json` or nuget.org list holding a version with a leading zero] → it is skipped as a non-version by `Parsed` and `NewestRelease.Recorded`, as `not a version` is today; nuget.org never publishes one.
- [`Outside.Fit(0)` changing from `…` to empty] → no screen hands a width of 0 today; the new facts fix the answer.
- [A future CLI printing the version after a prefix word] → `VersionAsync` answers `NotRead`, which the header shows as unknown, as it does for any unreadable output.
