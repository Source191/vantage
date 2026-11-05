using Source191.Torni.Adapters.Processes;

namespace Source191.Torni.Infrastructure.Tests;

/// <summary>Asks the real Windows association data, which opens nothing; every Windows install associates .txt, .cmd and .exe.</summary>
public sealed class FileAssociationsShould
{
    [Fact]
    public void AnswerNoneForATypeNothingIsAssociatedWith()
    {
        FileAssociations.NoneFor(".torninoapp").ShouldBeTrue();
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".cmd")]
    [InlineData(".exe")]
    public void AnswerSomeForATypeWindowsOpens(string extension)
    {
        FileAssociations.NoneFor(extension).ShouldBeFalse();
    }

    [Fact]
    public void NeverAnswerNoneForAnEmptyExtension()
    {
        FileAssociations.NoneFor(string.Empty).ShouldBeFalse("Windows answers an invalid argument, not no association");
    }
}