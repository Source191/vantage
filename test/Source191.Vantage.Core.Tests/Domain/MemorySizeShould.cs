using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class MemorySizeShould
{
    private const long Megabyte = 1_048_576;

    [Fact]
    public void StartAtZero()
    {
        MemorySize.Zero.Bytes.ShouldBe(0);
        default(MemorySize).ShouldBe(MemorySize.Zero);
    }

    [Fact]
    public void HoldTheBytesItWasGiven()
    {
        MemorySize.FromBytes(29 * Megabyte).Bytes.ShouldBe(29 * Megabyte);
        MemorySize.FromMegabytes(29).ShouldBe(MemorySize.FromBytes(29 * Megabyte));
    }

    [Fact]
    public void RefuseANegativeSize()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => MemorySize.FromBytes(-1));
    }

    [Fact]
    public void AddUp()
    {
        (MemorySize.FromMegabytes(296) + MemorySize.FromMegabytes(607)).ShouldBe(MemorySize.FromMegabytes(903));
        MemorySize.FromMegabytes(1).Plus(MemorySize.Zero).ShouldBe(MemorySize.FromMegabytes(1));
    }

    [Fact]
    public void OrderBySize()
    {
        MemorySize[] sizes = [MemorySize.FromMegabytes(30), MemorySize.FromMegabytes(100), MemorySize.Zero];

        sizes.OrderDescending().ShouldBe([MemorySize.FromMegabytes(100), MemorySize.FromMegabytes(30), MemorySize.Zero]);
        (MemorySize.FromMegabytes(30) < MemorySize.FromMegabytes(100)).ShouldBeTrue();
        (MemorySize.FromMegabytes(100) > MemorySize.FromMegabytes(30)).ShouldBeTrue();
        (MemorySize.Zero <= MemorySize.FromMegabytes(0)).ShouldBeTrue();
        (MemorySize.FromMegabytes(30) >= MemorySize.FromMegabytes(30)).ShouldBeTrue();
    }
}