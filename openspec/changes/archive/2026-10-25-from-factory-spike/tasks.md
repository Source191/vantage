# Tasks

## 1. The spelling

- [x] 1.1 `Architecture\TextValuesShould` flips: no public constructors on a text value and `From` declared on the interface (red first). Add `static abstract TSelf From(string text)` to `ITextValue<TSelf>`, implement it on the five types and make their constructors private. Verify the Domain tests fail only at construction sites.
- [x] 1.2 `Domain\TextValueContractShould`: one theory over the five types, with refused text making `From` throw `ArgumentException`, accepted text giving a value equal to `TryParse`'s, and `Value` equal to the text given. Verify it passes.
- [x] 1.3 Replace every `new X(text)` on a text value with `X.From(text)` in `src` and `test`. Verify `dotnet build`, the solution suite and `dotnet format --verify-no-changes` pass.

## 2. The decision

- [x] 2.1 The pull request description shows a `SessionTestData` fixture, `FolderPath.Combine` and `SiblingWorktree.For` in both spellings and answers the three criteria from design.md; the product owner decides. Verify the decision is written in this file: merged with a patch bump and the CLAUDE.md sentence updated, or closed with the reason.
  Merged: the product owner keeps `From`; the contract theory over all six types is worth the spelling change.

## Workflow follow-up

- P5 in `public-release`'s sequence: the refactors stream, stacked on P4 (`facts-not-sentences`), decided before the features stream continues past P6.
- After `text-value-doors`; the only change in its pull request.
- Archive the change whichever way the decision goes, so the record stays with the other decisions.
