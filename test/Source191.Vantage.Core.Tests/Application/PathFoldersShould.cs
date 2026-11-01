using Source191.Vantage.Adapters.Processes;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// A relative PATH entry would be searched against the folder Vantage was started from, which a cloned repository can
/// plant a program in, so only full paths are kept.
/// </summary>
public sealed class PathFoldersShould
{
    [Fact]
    public void KeepOnlyTheEntriesThatAreFullPaths()
    {
        PathFolders.From(@".;tools;;  ;C:\Program Files\Git\cmd").ShouldBe([@"C:\Program Files\Git\cmd"]);
    }

    [Fact]
    public void KeepNothingForAnUnsetPath()
    {
        PathFolders.From(null).ShouldBeEmpty();
    }
}