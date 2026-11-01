# Design

## Context

See proposal.md for why. The threading as of main at `0d90932`: `ScreenLoop.cs` runs `UpdateCheck.RunAsync` on a pool thread through `Task.Run` and never awaits it before the console opens; `ConsoleScreen` attaches the clipboard as its application starts and detaches it as it ends, on the UI thread; `CopyFolderPathHandler` and `CopyProblemReportHandler` call `IClipboard.TrySetText` from the dispatcher, off the UI thread; `SaveSettingsHandler` and `ToggleUpdateCheckHandler` call `CurrentSettings.SaveAsync`, and every scan, wizard, dialog and Config tab read takes `CurrentSettings.Value`.

The CoreCLR memory model (`docs/design/specs/Memory-model.md` in dotnet/runtime) already makes publishing a constructed object safe without fences, and orders reads that depend on the reference. What a plain field does not give is protection from the JIT merging adjacent reads of the same location, such as hoisting a read out of a loop a getter was inlined into. A `volatile` field gives that, with acquire and release ordering, at the cost of a plain `mov` on x64 and `ldar` on ARM64.

## Goals / Non-Goals

**Goals:**
- One home, and one comment, for the cross-thread replace-and-read pattern.
- No null held in any of the three fields.

**Non-Goals:**
- Atomic read-modify-write: two saves at once still end with whichever replaced last, as today.
- Testing the memory-model guarantee itself: a unit test cannot observe it, so the comment and the cited spec carry it.

## Decisions

**`SharedSnapshot<T>` is a sealed class with a `volatile` field, `where T : class`.** A primary constructor takes the first value; `Value` reads the field; `Replace(T next)` writes it. The non-nullable `class` constraint means a `SharedSnapshot<string?>` produces a nullable warning, which fails the build. The constructor's field initialiser guards with `initial ?? throw new ArgumentNullException(nameof(initial))` and `Replace` with `ArgumentNullException.ThrowIfNull`, as every public entry point does, so a `null!` cannot get in either. The comment on the class says: replaced whole by one thread and read by others; `volatile` stops the compiler reusing an earlier read and orders the read after the write; the value must be immutable, because only the reference is shared; see the CoreCLR memory model. Alternatives: a lock (adds a field, suggests an invariant across several steps that does not exist); `Interlocked` (there is no `Interlocked.Read` for references, so the read becomes `CompareExchange(ref value, null, null)`); `volatile` on each field (three comments, and a bare field to misuse).

**It lives in `Application`.** `CurrentSettings` and `UpdateCheck` are in `Application` and `ScreenClipboard` in `Tui`, which may depend on `Application`. It is not a domain concept, so not `Domain`. The class has one instance field, inside the two-field guideline, and `CurrentSettings` keeps two (`store` and the snapshot).

**`NewestRelease` is a closed two-case Domain record: `None` and `Found(ReleaseVersion Version)`.** It is a named case rather than a null object because `Offer` acts differently on each; it sits beside the null objects the code already has (`Footer.Blank`, `WizardEffect.None`). Alternative: keep `string` and store `""` for none (a magic value, worse than null).

**Null stays at the two boundaries and never reaches Domain.** The change touches two nullables that are not its own: the update-check port (`ReadUpdateCheckAsync` answers `UpdateCheckRecord?`, whose `Newest` is `string?`) and Terminal.Gui's `IApplication.Clipboard`. Each is translated on the one line that receives it; everything Domain and the snapshot see is non-nullable.
- `NewestRelease.Recorded(string text)` takes non-nullable text: `Found` when `ReleaseVersion.TryParse` reads it, `None` when it is not a version. The name avoids `From`, which the text values reserve for "builds or throws". `UpdateCheck` turns the port's null into `None` where it reads the record (`record?.Newest is string text ? NewestRelease.Recorded(text) : new NewestRelease.None()`), so a missing record and a record with no newest read alike, as today.
- `UpdateCheckRule.Newest(versions)` answers a `NewestRelease` (`None` when the list holds no release) instead of `ReleaseVersion?`. `UpdateCheck` replaces the snapshot with it and converts it to the record's text in one private switch naming both cases, whose discard arm throws `UnreachableException` (the two-case rule in CLAUDE.md): `Found` gives the version's text, `None` gives the port's null.
- `UpdateCheckRule.Offer(ToolVersion, bool, NewestRelease)` tests the case with one pattern, `newest is NewestRelease.Found found && found.Version.IsHigherThan(current)`, so it needs no switch. This keeps today's answers, including a recorded prerelease, which `TryParse` reads and `IsHigherThan` compares as it does now.
- `ReleaseVersion.TryParse` still answers null for text that is not a version; `Recorded` pattern-matches it on one line. Changing it to the `out` shape is the later `null-suppressions` change, which alters only that line here.
- Making the port itself null-free (an `UpdateCheckRecord` null object, `Newest` as something other than `string?`) reaches `JsonFileStore`, `InMemoryState`, `SetupFactsReader` and the state file's JSON, so it belongs to the later boundary change.

**`ScreenClipboard` holds a `SharedSnapshot<IClipboard>`.** Two private nested classes: `NoClipboard`, whose `TrySetText` answers false (today's answer when nothing is attached), and `ApplicationClipboard(IApplication app)`, which answers `app.Clipboard is { } clipboard && clipboard.TrySetClipboardData(text)`. Terminal.Gui 2.5.0 declares `Clipboard` nullable ("null if Driver is not initialized"), so that one line is the boundary check; it is made on each copy rather than once at `Attach`, so nothing depends on Terminal.Gui never replacing its clipboard during a run. `Attach` replaces with an `ApplicationClipboard`, `Detach` with the `NoClipboard` instance, and `TrySetText` delegates to the snapshot's value. Both nested classes stay private to `Tui`, which has no test project; the smoke check and the copy handlers' fakes cover them, as they cover `ScreenClipboard` now.

## Risks / Trade-offs

- [An analyser objects to `volatile`] → the first build after `SharedSnapshot` is written shows it; suppressing a rule for the one field with a justification is acceptable, and so is the fallback of `Volatile.Read` and `Volatile.Write` inside the one class.
- [Someone stores a mutable object in a snapshot] → the class comment says the value must be immutable; `Settings`, `NewestRelease` and the two clipboards are.
- [`UpdateCheckRuleShould`'s `Offer` facts change their argument type] → each keeps its expected answer, so a changed answer shows as a failing fact, not a silently rewritten one.
