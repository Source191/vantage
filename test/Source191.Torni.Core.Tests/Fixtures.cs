using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Launch;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests;

/// <summary>What the tests of the words and the clock share: one noon, and a builder that makes any case of a closed hierarchy.</summary>
internal static class Fixtures
{
    public static readonly DateTimeOffset Noon = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Builds a case by its one constructor that does not take the case itself, with a placeholder for each field.</summary>
    public static object Build(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));

        return constructor.Invoke([.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))]);
    }

    public static object Placeholder(Type type)
    {
        return type switch
        {
            _ when type == typeof(string) => "fix-123",
            _ when type == typeof(ListedName) => Listed.As("fix-123"),
            _ when type == typeof(SessionName) => SessionName.From("fix-124"),
            _ when type == typeof(int) => 2,
            _ when type == typeof(bool) => true,
            _ when type == typeof(RemovalReason) => new RemovalReason.Locked(),
            _ when type == typeof(BranchToConfirm) => new BranchToConfirm(FolderPath.From(@"C:\repos\Northwind.Orders"), BranchName.From("fix-123")),
            _ when type == typeof(EditorChoice) => EditorChoice.VsCode,
            _ when type == typeof(FolderPath) => FolderPath.From(@"C:\repos\Northwind.Orders"),
            _ when type == typeof(JobId) => JobId.From("81cdb0f9"),
            _ when type == typeof(SessionId) => SessionId.From("sess-1"),
            _ when type == typeof(ReleaseVersion) => ReleaseVersions.Version("0.21.0"),
            _ when type == typeof(BranchName) => BranchName.From("fix-123"),
            _ when type == typeof(OpenTabOutcome) => new OpenTabOutcome.SwitchedTo(Listed.As("fix-123")),
            _ when type == typeof(RemoveWorktreeOutcome) => new RemoveWorktreeOutcome.DeletedFolder("Northwind.Orders-fix-123"),
            _ when type == typeof(IReadOnlyList<ListedName>) => new List<ListedName> { Listed.As("fix-123") },
            _ when type == typeof(IReadOnlyList<SkippedSession>) => new List<SkippedSession>(),
            _ when type == typeof(IReadOnlyList<ResumeNote>) => new List<ResumeNote>(),
            _ when type == typeof(PullRequestHost) => PullRequestHost.AzureDevOps,
            _ when type == typeof(LookupHost) => new LookupHost.Known(PullRequestHost.GitHub),
            _ when type == typeof(PullRequestGap) => PullRequestGap.NotSignedIn,
            _ when type == typeof(WorktreeHealthKind) => WorktreeHealthKind.Locked,
            _ when type == typeof(HealthReason) => new HealthReason.FolderGone(),
            _ when type == typeof(NameProblem) => NameProblem.Missing,
            _ when type == typeof(LaunchProblem) => new LaunchProblem.WorktreeNeedsLetters(),
            _ when type == typeof(ViewEffect) => new Quit(),
            _ when type == typeof(Outcome) => new StopSessionOutcome.Stopped(Listed.As("fix-123"), TabClosed: false),
            _ when type == typeof(SetupItem) => SetupItem.TerminalProfiles,
            _ when type == typeof(ConfigRow) => new ConfigRow.CheckForUpdates(On: true),
            _ => throw new InvalidOperationException($"A field of type {type.Name} needs a placeholder."),
        };
    }
}