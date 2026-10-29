namespace Source191.Vantage.Domain;

/// <summary>An amount of memory in bytes; a megabyte is 1,048,576 bytes, as Task Manager counts it.</summary>
public readonly record struct MemorySize : IComparable<MemorySize>
{
    public const long BytesPerMegabyte = 1_048_576;

    public static readonly MemorySize Zero;

    private MemorySize(long bytes) => Bytes = bytes;

    public long Bytes { get; }

    public static MemorySize FromBytes(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);

        return new MemorySize(bytes);
    }

    public static MemorySize FromMegabytes(long megabytes) => FromBytes(megabytes * BytesPerMegabyte);

    public MemorySize Plus(MemorySize other) => new(Bytes + other.Bytes);

    public int CompareTo(MemorySize other) => Bytes.CompareTo(other.Bytes);

    public static MemorySize operator +(MemorySize left, MemorySize right) => left.Plus(right);

    public static bool operator <(MemorySize left, MemorySize right) => left.CompareTo(right) < 0;

    public static bool operator >(MemorySize left, MemorySize right) => left.CompareTo(right) > 0;

    public static bool operator <=(MemorySize left, MemorySize right) => left.CompareTo(right) <= 0;

    public static bool operator >=(MemorySize left, MemorySize right) => left.CompareTo(right) >= 0;
}
