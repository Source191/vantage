namespace Source191.Vantage.UnitTests;

public sealed class ScaffoldShould
{
    [Fact]
    public void PackTheToolAsVantage()
    {
        string? assemblyName = typeof(Program).Assembly.GetName().Name;

        assemblyName.ShouldBe("vantage");
    }
}