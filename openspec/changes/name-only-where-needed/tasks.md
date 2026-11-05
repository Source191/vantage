## 1. Identifiers

- [ ] 1.1 Rename the identifiers and test names that only meant "this program" with the token-level tool. Verify the build and the whole suite pass unchanged, and `git grep` finds no `Torni` identifier outside namespaces.

## 2. State word and label

- [ ] 2.1 `TabOwnerJsonConverterShould` and the tab-file facts expect `console` for the console's own tab; red, then `TabOwnerJsonConverter`; green.
- [ ] 2.2 The issue form applies `problem-report`; rename the GitHub label `from-vantage` to `problem-report`. Verify with `gh label list`.

## 3. Sentences

- [ ] 3.1 On-screen text: each `Wording` sentence that names the program without needing to, its test first. Verify the Core suite and that no spec quotes a changed sentence.
- [ ] 3.2 Comments in C#, workflows and `openspec\config.yaml`. Verify by reading the diff.
- [ ] 3.3 The Unreleased section of `CHANGELOG.md`. Verify with `Assert-ChangelogUpdated.ps1`.

## 4. Internals and archive

- [ ] 4.1 CI's extracted-zip folder, the temporary folder prefixes, `Get-TorniVersion` and the `.gitignore` comments go neutral. Verify the build, the suite and the ps1 scripts that use them.
- [ ] 4.2 The build, the whole suite and `dotnet format --verify-no-changes` pass; archive the change. Verify with `openspec validate --all`.
