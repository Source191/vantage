using Source191.Torni.Domain;
using System.Globalization;
using System.Text.Json;

namespace Source191.Torni.ReadmeScene;

/// <summary>What <c>gh pr list</c> and <c>az repos pr list</c> answer for one branch of a scene repository, in each CLI's own JSON.</summary>
public sealed class ScenePullRequests(Scene scene, TimeProvider clock)
{
    public string GitHubJson(string ownerAndName, string branch)
    {
        object[] rows = [.. For(branch, remote => remote is RemoteRepository.GitHub gitHub && $"{gitHub.Owner}/{gitHub.Name}" == ownerAndName)
            .Select(pullRequest => new
            {
                number = pullRequest.Number,
                url = $"https://github.com/{ownerAndName}/pull/{pullRequest.Number}",
                state = pullRequest.State switch
                {
                    ScenePullRequestState.Merged or ScenePullRequestState.Completed => "MERGED",
                    ScenePullRequestState.Closed or ScenePullRequestState.Abandoned => "CLOSED",
                    ScenePullRequestState.Open or ScenePullRequestState.Draft => "OPEN",
                },
                isDraft = pullRequest.State == ScenePullRequestState.Draft,
                headRefName = branch,
                createdAt = Iso(pullRequest),
            })];

        return JsonSerializer.Serialize(rows);
    }

    public string AzureDevOpsJson(string organisation, string project, string name, string branch)
    {
        object[] rows = [.. For(branch, remote => remote is RemoteRepository.AzureDevOps azure && azure.Organisation == organisation && azure.Project == project && azure.Name == name)
            .Select(pullRequest => new
            {
                pullRequestId = pullRequest.Number,
                sourceRefName = $"refs/heads/{branch}",
                status = pullRequest.State switch
                {
                    ScenePullRequestState.Merged or ScenePullRequestState.Completed => "completed",
                    ScenePullRequestState.Closed or ScenePullRequestState.Abandoned => "abandoned",
                    ScenePullRequestState.Open or ScenePullRequestState.Draft => "active",
                },
                isDraft = pullRequest.State == ScenePullRequestState.Draft,
                creationDate = Iso(pullRequest),
            })];

        return JsonSerializer.Serialize(rows);
    }

    private IEnumerable<ScenePullRequest> For(string branch, Func<RemoteRepository, bool> isRemote) =>
        scene.PullRequests.Where(pullRequest => pullRequest.Branch == branch && scene.Repositories.Any(repository =>
            repository.Name == pullRequest.Repository && RemoteRepository.TryParse(repository.Remote, out RemoteRepository? remote) && isRemote(remote)));

    private string Iso(ScenePullRequest pullRequest) => pullRequest.OpenedAt(clock).ToString("O", CultureInfo.InvariantCulture);
}