# Proposal

## Why

Every class and record in Vantage that can be sealed should be. Microsoft's code analysis rule [CA1852: Seal internal types](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1852) recommends sealing any type that is not accessible outside its assembly and has no subtypes, because sealing lets the runtime devirtualise calls and makes type checks cheaper; the fix is non-breaking. The solution's `AnalysisLevel` of `10.0-recommended` already raises CA1852 as a warning (an error here), and the code meets it today, but the rule has two gaps: it ignores `public` types, and it switches itself off in an assembly with `InternalsVisibleTo` (Infrastructure and HeadlessTerminal). Most of Vantage's types are `public` because the projects reference one another, so nothing stops the next unsealed class.

## What Changes

- **`Architecture\SealedTypesShould`** in `test\Source191.Vantage.Core.Tests`: reflects over the Core, Infrastructure, Tui and TestDoubles assemblies and fails, naming the type, for any class or record that is not `sealed`, `abstract` or `static` and has no subtype in those assemblies. Compiler-generated types are skipped.
- **`.editorconfig`:** `dotnet_code_quality.CA1852.ignore_internalsvisibleto = true`, so CA1852 also checks the internal types of Infrastructure and HeadlessTerminal at build time.
- **`CLAUDE.md`:** one bullet in the C# idioms: every class or record is `sealed`, `abstract` or `static`, citing CA1852 and naming `SealedTypesShould`; the layering sentence names the new fact beside the others.
- **Sealing:** any type the fact or the build reports is sealed. The source scan found none; if one turns up under `src\`, `Version` is raised as the release rule requires.
- `skip_specs: true`: a test, an analyser setting and a document; no behaviour changes.

Out of scope: the Framework Design Guidelines' advice not to seal classes without a good reason, which is for reusable public libraries rather than an application whose types are consumed only inside its own solution; checking the `Source191.Vantage` executable and `tools\HeadlessTerminal` by reflection (Core.Tests does not reference them; CA1852 covers their internal types); sealing members; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: an architecture test, an analyser setting and a documentation rule, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Tests:** new `test\Source191.Vantage.Core.Tests\Architecture\SealedTypesShould.cs`.
- **Build:** root `.editorconfig` gains one CA1852 option.
- **Docs:** `CLAUDE.md` (one bullet in C# idioms; the layering sentence names `SealedTypesShould`).
