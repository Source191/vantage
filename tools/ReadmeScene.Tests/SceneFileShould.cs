using Microsoft.Extensions.Time.Testing;

namespace Source191.Torni.ReadmeScene.Tests;

public sealed class SceneFileShould
{
    private const string OneOfEach = """
        {
          "claudeVersion": "2.1.316",
          "repositories": [
            { "name": "Northwind.Orders", "remote": "https://github.com/northwind-demo/Northwind.Orders.git" }
          ],
          "sessions": [
            {
              "name": "fix-tax-rounding",
              "repository": "Northwind.Orders",
              "worktree": "fix-tax-rounding",
              "kind": "background",
              "state": "waiting",
              "waitingFor": "permission to run dotnet test",
              "startedMinutesAgo": 95,
              "tab": true,
              "memoryMegabytes": 240
            }
          ],
          "automatedRuns": [ { "repository": "Northwind.Orders", "startedMinutesAgo": 1, "memoryMegabytes": 180 } ],
          "pullRequests": [
            { "repository": "Northwind.Orders", "branch": "fix-tax-rounding", "number": 142, "state": "open", "openedDaysAgo": 2 }
          ]
        }
        """;

    [Fact]
    public void ReadTheRepositoriesSessionsAndPullRequests()
    {
        Scene scene = SceneFile.Read(OneOfEach);

        scene.ClaudeVersion.ShouldBe("2.1.316");
        scene.Repositories.ShouldHaveSingleItem().ShouldBe(new SceneRepository("Northwind.Orders", "https://github.com/northwind-demo/Northwind.Orders.git"));
        SceneSession session = scene.Sessions.ShouldHaveSingleItem();
        session.Name.ShouldBe("fix-tax-rounding");
        session.Repository.ShouldBe("Northwind.Orders");
        session.Worktree.ShouldBe("fix-tax-rounding");
        session.Kind.ShouldBe(SceneKind.Background);
        session.State.ShouldBe(SceneState.Waiting);
        session.WaitingFor.ShouldBe("permission to run dotnet test");
        session.Tab.ShouldBeTrue();
        session.MemoryMegabytes.ShouldBe(240);
        scene.AutomatedRuns.ShouldHaveSingleItem().Repository.ShouldBe("Northwind.Orders");
        scene.PullRequests.ShouldHaveSingleItem().ShouldBe(new ScenePullRequest("Northwind.Orders", "fix-tax-rounding", 142, ScenePullRequestState.Open, 2));
    }

    [Fact]
    public void PlaceEachStartTimeMinutesBeforeNow()
    {
        FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero));
        Scene scene = SceneFile.Read(OneOfEach);

        scene.Sessions[0].StartedAt(clock).ShouldBe(new DateTimeOffset(2026, 11, 4, 10, 25, 0, TimeSpan.Zero));
        scene.PullRequests[0].OpenedAt(clock).ShouldBe(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ReadTheShippedScene()
    {
        Scene scene = SceneFile.Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scene.json")));

        scene.Sessions.Select(session => session.State).Distinct().Order().ShouldBe(
            [SceneState.Working, SceneState.Waiting, SceneState.Idle, SceneState.Done, SceneState.Failed, SceneState.Stopped]);
        scene.Sessions.ShouldContain(session => session.Kind == SceneKind.Interactive);
        scene.AutomatedRuns.ShouldNotBeEmpty();
        scene.PullRequests.Select(pullRequest => scene.Repositories.Single(repository => repository.Name == pullRequest.Repository).Remote)
            .ShouldContain(remote => remote.Contains("dev.azure.com", StringComparison.Ordinal));
        scene.PullRequests.Select(pullRequest => scene.Repositories.Single(repository => repository.Name == pullRequest.Repository).Remote)
            .ShouldContain(remote => remote.Contains("github.com", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Contoso.Payments", "https://github.com/northwind-demo/Contoso.Payments.git")]
    [InlineData("Northwind.Orders", "https://github.com/someone-real/Northwind.Orders.git")]
    [InlineData("Northwind.Orders", "https://dev.azure.com/some-company/Orders/_git/Northwind.Orders")]
    [InlineData("torni", "https://github.com/northwind-demo/torni.git")]
    public void RefuseARealLookingRepository(string name, string remote)
    {
        string json = $$"""{ "claudeVersion": "2.1.316", "repositories": [ { "name": "{{name}}", "remote": "{{remote}}" } ] }""";

        Should.Throw<InvalidDataException>(() => SceneFile.Read(json));
    }

    [Theory]
    [InlineData("Northwind.Orders", "https://github.com/northwind-demo/Northwind.Orders.git")]
    [InlineData("Northwind.Billing", "https://dev.azure.com/northwind-demo/Northwind/_git/Northwind.Billing")]
    [InlineData("torni", "https://github.com/Source191/torni.git")]
    public void AcceptAMadeUpRepositoryOrItsOwn(string name, string remote)
    {
        string json = $$"""{ "claudeVersion": "2.1.316", "repositories": [ { "name": "{{name}}", "remote": "{{remote}}" } ] }""";

        SceneFile.Read(json).Repositories.ShouldHaveSingleItem().Name.ShouldBe(name);
    }
}