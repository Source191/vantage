# Design

## Context

See proposal.md, Why. Today: `FolderPath`, `SessionId`, `JobId` and `BranchName` have a public constructor that throws and a static `TryParse(string?)` returning a nullable; `SessionName` has a private constructor, a throwing `Parse`, a nullable `TryParse(string)` and `Validate`; `WorktreeSlug.From(string)` and `JobId.Of(SessionId)` return nullables. Seventeen `src` call sites use the nullable shape, seven of them chained (`?? throw`, `== worktree`, `is not FolderPath path`, and `.Select(FolderPath.TryParse).OfType<FolderPath>()` in `RepositoryCatalogue`). `LaunchPlanResult(LaunchPlan? Plan, LaunchProblem? Problem)` is read as `Plan is null` by `HereCommandRunner` and `WizardView`. The tests cast fixture rows with `As.Job(row)` forty times. .NET 10 supports static abstract interface members, and the test project already references NetArchTest.

## Goals / Non-Goals

**Goals:**
- Three doors, one spelling each: constructor, `TryParse`, `Validate`.
- No nullable return from any Domain factory and no result modelled as a pair of nullables.
- The shape enforced by the compiler and the membership by a test.

**Non-Goals:**
- `From` as the throwing door; `from-factory-spike` tries it on this foundation.
- `IParsable<T>`; its format-provider parameters are noise and nothing consumes generic parsing.
- A `Result<T, E>` type or any change to `Validate`.

## Decisions

- **`bool TryParse(string?, out T)` over a nullable return.** It removes every nullable return, it is the shape every .NET reader expects from the name, and the out value is annotated `[MaybeNullWhen(false)]` so the compiler treats it as non-null after a true result. The interface declares the annotation, and an implementation that drops it fails to build (CS8767 is an error under warnings-as-errors). Alternative rejected: keeping nullable returns under a new name, which keeps the nulls and spends the word `From` that the spike needs.
- **Constructors as the throwing door.** `new X(text)` already means build or throw, is enforced by the language, fits the `X value = new(text)` style and is what four of the five types do. `SessionName.Parse` becomes the public constructor, which checks and throws as `FolderPath`'s and `JobId`'s do; `Validate` and `TryParse` run the same check first and then construct, so the rule is one private static shared by all three doors and nothing recurses. Alternative deferred to the spike: private constructors and `From`.
- **The interface lives in Domain and has three consumers:** `TextValueJsonConverter<T>` in the state adapter, `Parsed<T>()` next to the interface for the LINQ chains, and the architecture fact. The generic constraint is `where TSelf : class, ITextValue<TSelf>`, so the nullable annotations mean reference nullability.
- **`WorktreeSlug.TryParse(string, out WorktreeSlug)`.** The slug is derived rather than parsed, but one verb for one shape wins, and `SessionName.Slug` goes so the Domain has no nullable property for a derivation; `LaunchPlanBuilder` and `LaunchPreview` call `WorktreeSlug.TryParse`.
- **`JobId.TryParse(SessionId, out JobId)`** replaces `Of`; the listing parser's `JobId.TryParse(row.Id, out id) || JobId.TryParse(sessionId, out id)` reads as the fallback it is.
- **The three resume handlers discard the bool.** The CLI port takes `SessionName?` as "resume under this name, or under none", so `_ = SessionName.TryParse(session.Name, out SessionName? name)` followed by passing `name` is exactly that contract. This is the one place the new shape reads worse than the old, and a comment says why.
- **`LaunchPlanResult` is two cases,** `Planned(LaunchPlan Plan)` and `Refused(LaunchProblem Problem)`, switched with an `UnreachableException` arm as `unreachable-arms` sets out. `HereCommand` and `LaunchPlanBuilder` return one or the other; `HereCommandRunner` and `WizardView.Started` match on them.
- **`WizardProblem.None` and `WizardEffect.None`** follow `Footer.Blank`: `Wording.Describe(WizardProblem.None)` is the empty string, and the binder tests `is not WizardEffect.None` to decide whether to close.
- **The architecture fact uses reflection,** including `NullabilityInfoContext` for the nullable-return check, rather than an analyzer. The naming rules in `.editorconfig` cannot see return nullability, the banned-API analyzer has nothing to ban once `Parse` is gone, and a custom analyzer is a project to maintain for what one test covers.
- **Typed fixtures.** `SessionTestData.Job(...)` and `Terminal(...)` return the typed session and the row fixtures build on them, so the forty casts become direct references and `As` goes.

## Risks / Trade-offs

- [Sonar's rule against `out` parameters (S3874)] → it exempts the `Try` pattern; the build confirms, and the rule is disabled for the Domain if it does not.
- [The discarded bool in the resume handlers looks like a bug] → the comment, and `AdoptSessionHandlerShould` already asserts that an unsafe listed name resumes with no name.
- [A large mechanical diff hides a semantic change] → every call site change is a compile error until made, and the solution suite and the hand-off check run before the pull request.
