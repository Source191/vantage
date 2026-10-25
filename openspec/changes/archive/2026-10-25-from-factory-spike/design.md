# Design

## Context

See proposal.md, Why. After `text-value-doors` every text type has a public constructor that throws, a `bool TryParse` on `ITextValue<TSelf>`, and `TextValuesShould` asserts exactly one public string constructor and no member named `From`. Constructors cannot be interface members, so a contract test over all five types needs reflection; a static abstract `From` can be called as `T.From(text)` in a generic method.

## Goals / Non-Goals

**Goals:**
- A diff that contains the `From` spelling and nothing else.
- A written decision, whichever way it goes.

**Non-Goals:**
- Changing `TryParse` or `Validate`.
- Keeping both doors; if `From` stays, the constructors are private.

## Decisions

- **Only this change in its pull request.** Anything else in the diff would muddy the comparison.
- **`From` on the interface, constructors private.** Two throwing doors would be the inconsistency the previous change removed. `From` is the one that can be generic.
- **The contract theory is the benefit to weigh.** `TextValueContractShould` runs one theory for the five types: refused text makes `From` throw `ArgumentException`, accepted text gives a value equal to `TryParse`'s, and `Value` is the text given. With constructors the same test needs `Activator.CreateInstance` and unwrapping `TargetInvocationException`; the per-type facts from `text-value-doors` already cover each type on its own.
- **The cost to weigh is at the sites.** `FolderPath repository = new(@"C:\...")` becomes `FolderPath repository = FolderPath.From(@"C:\...")`, losing target-typed `new`, which the repository's style prefers; about two hundred test lines change once and every future fixture line is longer by a word.
- **Decision criteria, written before the diff exists:** readability at three representative sites (a `SessionTestData` fixture, `FolderPath.Combine`, `SiblingWorktree.For`); whether the generic theory catches anything the per-type facts do not; and what a sixth text value would cost under each spelling. The pull request description shows the three sites side by side.
- **Recording.** Merged: the tasks file notes the decision and `CLAUDE.md`'s three-doors sentence says `From`. Closed: the tasks file notes the decision and why, the change is archived as decided against, and the constructor stays the door.

## Risks / Trade-offs

- [A mechanical diff of two hundred lines is reviewed by skimming] → every change is a compile error until made, and the solution suite runs; the review is of the three sites, not the two hundred.
- [An unmerged spike leaves an open change] → it is archived with its decision, so the record stays and `openspec list` stays clean.
