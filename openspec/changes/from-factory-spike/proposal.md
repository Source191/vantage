# Proposal

## Why

The product owner prefers `From` as the name of a factory that throws, and wants to see it on an otherwise consistent codebase before deciding. After `text-value-doors` the five text types all use the constructor as their throwing door, so a pull request that changes only that spelling shows the whole trade in one diff: what `From` costs at every construction site, and the one thing it enables, a contract test written once for every type.

## What Changes

- **`From` joins the interface:** `ITextValue<TSelf>` gains `static abstract TSelf From(string text)`, each type implements it as the throwing door, and the constructors become private.
- **Every construction site changes spelling:** `new FolderPath(text)` becomes `FolderPath.From(text)`, and likewise for `SessionId`, `JobId`, `BranchName` and `SessionName`. About seven sites in `src` and two hundred in tests.
- **The architecture fact flips:** no public constructors on a text value, and `From` present.
- **One generic contract theory** over the five types: text that `TryParse` refuses makes `From` throw `ArgumentException`; text it accepts gives a `From` value equal to the parsed one; `Value` round-trips.
- **The decision is recorded** in this change's tasks.md and the change is archived either way: merged as a patch bump, or closed unmerged with the reason written down so the question does not reopen.

Out of scope: any other change in the same pull request, so the diff is the argument; and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. A refactor with no behaviour change; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Domain:** `ITextValue.cs`, `FolderPath.cs`, `Identifiers.cs`, `SessionName.cs`.
- **Everywhere a text value is constructed:** `FolderPath.Combine`, `FolderPath.Parent`, `SiblingWorktree.For`, `HereCommand.Plan`, `TabRegistration`, the state store, and about two hundred test lines.
- **Tests:** `Architecture\TextValuesShould` and a new `Domain\TextValueContractShould`.
- **Docs:** CLAUDE.md's three-doors sentence, if merged; `Version`, if merged.
