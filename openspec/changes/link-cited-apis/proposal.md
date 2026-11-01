# Proposal

## Why

Many comments state what an external API does (a Win32 or NT call, a structure, a console escape sequence, a `wt.exe` argument, a git or gh output format, the NuGet API) and the code relies on exactly that behaviour, but they name the API without saying where it is documented. The owner asked for Microsoft Learn links for the Win32 calls and for the other external APIs the comments cite, so a reader can check a claim at its source.

## What Changes

- **Comments under `src\`, `test\` and `tools\`:** where a comment states or relies on an external API's documented behaviour, its existing words are wrapped in `<see href="...">` in XML doc comments, or the URL is appended in parentheses in `//` comments. Microsoft Learn first (no locale segment), otherwise the official source (git-scm.com, cli.github.com, semver.org, the xterm and kitty specifications, vswhere's repository). A passing mention of a type the code obviously uses is left alone.
- **Workflow and script comments under `.github\` and `tools\*.ps1`:** the URL on the comment line or the next one.
- **`CLAUDE.md`'s facts sections:** Markdown links on the cited APIs, except the lines next to the `Tab.cpp` link that #138 added on `main`, which are left as they are so the stack merges without a conflict.
- Every URL answered 200 before it was used.
- `Version` rises to `0.19.0-preview.14`, because comments under `src\` change; no behaviour changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: comments and documentation only, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** comments only, in the adapters (processes, terminal, git, pull requests, packages), a few Domain and Tui types, the headless terminal tool and their tests.
- **Docs:** `CLAUDE.md` facts sections; workflow comments.
- **Version:** `src\Source191.Vantage\Source191.Vantage.csproj` to `0.19.0-preview.14`.
