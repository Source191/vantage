using Source191.Torni.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Source191.Torni.ReadmeScene;

/// <summary>
/// Reads <c>scene.json</c>. Only made-up repositories are allowed: a Northwind.* name whose remote belongs to the
/// <c>northwind-demo</c> GitHub owner or Azure DevOps organisation, or Torni itself at its own GitHub address.
/// </summary>
public static class SceneFile
{
    public const string DemoOwner = "northwind-demo";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static Scene Read(string json)
    {
        Scene read = JsonSerializer.Deserialize<Scene>(json, Options) ?? throw new InvalidDataException("The scene file is empty.");
        Scene scene = read with
        {
            Repositories = read.Repositories ?? [],
            Sessions = read.Sessions ?? [],
            AutomatedRuns = read.AutomatedRuns ?? [],
            PullRequests = read.PullRequests ?? [],
        };
        SceneRepository? real = scene.Repositories.FirstOrDefault(repository => !IsMadeUp(repository));

        return real is null ? scene : throw new InvalidDataException($"{real.Name} at {real.Remote} is not a made-up repository.");
    }

    private static bool IsMadeUp(SceneRepository repository) => RemoteRepository.TryParse(repository.Remote, out RemoteRepository? remote) && remote switch
    {
        RemoteRepository.GitHub { Owner: "Source191", Name: "torni" } => repository.Name == "torni",
        RemoteRepository.GitHub gitHub => IsNorthwind(repository) && gitHub.Owner == DemoOwner,
        RemoteRepository.AzureDevOps azure => IsNorthwind(repository) && azure.Organisation == DemoOwner,
        _ => false,
    };

    private static bool IsNorthwind(SceneRepository repository) => repository.Name.StartsWith("Northwind.", StringComparison.Ordinal);
}