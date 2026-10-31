namespace Source191.Vantage.Application;

/// <summary>
/// One value replaced whole by one thread and read by others, such as the settings a save replaces while scans read
/// them. The field is <c>volatile</c> so the compiler never reuses an earlier read of it (a read hoisted out of a
/// loop a getter was inlined into would never see a replacement) and each read is ordered after the write it sees;
/// the CoreCLR memory model (docs/design/specs/Memory-model.md in dotnet/runtime) already makes publishing a
/// constructed object safe. Only the reference is shared, so the value must be immutable. It never holds null: the
/// type is a non-nullable reference type and both doors refuse one. Replacing is not a read-modify-write: two
/// replacements at once end with whichever came last.
/// </summary>
public sealed class SharedSnapshot<T>(T initial)
    where T : class
{
    private volatile T value = initial ?? throw new ArgumentNullException(nameof(initial));

    public T Value => value;

    public void Replace(T next)
    {
        ArgumentNullException.ThrowIfNull(next);
        value = next;
    }
}
