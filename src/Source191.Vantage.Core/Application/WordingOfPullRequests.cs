using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>The words for pull requests: the PR cell, why a row has none, and each host's numbering and CLI.</summary>
public static partial class Wording
{
    /// <summary>The PR column: the number and state for a found pull request, empty otherwise.</summary>
    public static string PullRequestCell(PullRequestLookup lookup) => lookup switch
    {
        PullRequestLookup.Found found => $"{PullRequestNumber(found.PullRequest.Host, found.PullRequest.Number)} {Describe(found.PullRequest.State)}",
        _ => string.Empty,
    };

    /// <summary>GitHub numbers pull requests as #123, Azure DevOps as !123.</summary>
    public static string PullRequestNumber(PullRequestHost host, int number) => host switch
    {
        PullRequestHost.GitHub => $"#{number}",
        PullRequestHost.AzureDevOps => $"!{number}",
    };

    public static string Describe(PullRequestState state) => state switch
    {
        PullRequestState.Open => "open",
        PullRequestState.Draft => "draft",
        PullRequestState.Merged => "merged",
        PullRequestState.Closed => "closed",
        PullRequestState.Active => "active",
        PullRequestState.Completed => "completed",
        PullRequestState.Abandoned => "abandoned",
    };

    private static string Describe(Footer.NoPullRequest none) => none.Gap switch
    {
        PullRequestGap.NoBranch => $"{none.RowName} has no branch, so it has no pull request.",
        PullRequestGap.NoRemote => $"{none.RowName}'s repository has no origin remote to look up pull requests on.",
        PullRequestGap.UnknownHost => $"{none.RowName}'s origin is on neither GitHub nor Azure DevOps, so Vantage cannot look up its pull requests.",
        PullRequestGap.CliMissing => $"Install {Cli(none.Host)} to see {none.RowName}'s pull request.",
        PullRequestGap.NotSignedIn => $"Sign in with {SignIn(none.Host)} to see {none.RowName}'s pull request.",
        PullRequestGap.Looking => $"Still looking up {none.RowName}'s pull request; try again in a moment.",
        PullRequestGap.Failed when none.Host is PullRequestHost host => $"{Capitalised(Cli(host))} could not list {none.RowName}'s pull requests.",
        PullRequestGap.Failed => $"Could not look up {none.RowName}'s pull requests.",
        PullRequestGap.NoPullRequest => $"No pull request found for {none.RowName}'s branch.",
    };

    private static string Cli(PullRequestHost? host) => host switch
    {
        PullRequestHost.GitHub => "the GitHub CLI (gh)",
        PullRequestHost.AzureDevOps => "the Azure CLI (az) with the azure-devops extension",
        null => "the GitHub CLI (gh) or the Azure CLI (az)",
    };

    private static string SignIn(PullRequestHost? host) => host switch
    {
        PullRequestHost.GitHub => "gh auth login",
        PullRequestHost.AzureDevOps => "az login",
        null => "gh auth login or az login",
    };

    private static string Capitalised(string text) => string.Concat(char.ToUpperInvariant(text[0]).ToString(), text[1..]);
}