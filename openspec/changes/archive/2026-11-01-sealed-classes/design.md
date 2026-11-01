# Design

## Context

See proposal.md for why. `Directory.Build.props` sets `AnalysisLevel` to `10.0-recommended`, whose global config holds `dotnet_diagnostic.CA1852.severity = warning`, and `TreatWarningsAsErrors` makes that an error. CA1852 only looks at types invisible outside their assembly and skips an assembly with `InternalsVisibleTo` unless `ignore_internalsvisibleto` is set. A source scan on 1 November 2026 found every class and record already `sealed`, `abstract` or `static`.

## Goals / Non-Goals

**Goals:**
- A failing fact names any concrete class or record left open, whatever its accessibility.
- CA1852 checks the internal types of every assembly, including those with `InternalsVisibleTo`.

**Non-Goals:**
- Reflection over the executable and `tools\HeadlessTerminal`; CA1852 covers their internals and their public types are few.
- Sealing overridden members.

## Decisions

- **Plain reflection, not NetArchTest.** The rule is one predicate (`IsClass && !IsAbstract && !IsSealed`; a static class is `abstract sealed` in metadata, so it falls out), and reflection matches `TextValuesShould` and `NullFreeModelsShould`. NetArchTest 1.3.2 has `AreNotSealed` but no "has no subtype" test, so the open-base case would need reflection anyway.
- **An open class with a subtype is allowed.** That is CA1852's own definition of "can be sealed"; a concrete base with subclasses cannot be. The subtypes are looked for in the same set of assemblies.
- **Compiler-generated types are skipped** (`CompilerGeneratedAttribute`, as Debug builds emit async state machines as classes), so the fact speaks only of types a person wrote.
- **The failure lists full names** (`ShouldBeEmpty()` over strings), so a red run says what to seal.
- **A probe proves the predicate.** Nested probe types in the test file (an open class, a sealed one, a static one, an abstract record with a sealed case, an open base with a subclass) check the finder reports only the open leaf, so the fact cannot pass by finding nothing.
- **CA1852 stays at the severity the analysis level gives it**; only `ignore_internalsvisibleto` is added, in `.editorconfig` beside the other analyser settings.

## Risks / Trade-offs

- [The probe's own open class would make the whole-assembly fact fail] → the whole-assembly fact excludes the test assembly and the probe is checked only through the finder.
- [A future Terminal.Gui view needs to be subclassed by another of ours] → allowed by the subtype rule without an exception list.
