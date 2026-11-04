using Source191.Torni.Adapters.Claude;
using Source191.Torni.Domain;

namespace Source191.Torni.Infrastructure.Tests;

public sealed class ClaudeTranscriptsShould : IDisposable
{
    private readonly string home = Path.Combine(Path.GetTempPath(), "torni-transcripts-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void FindASessionsTranscriptInAnyProjectFolder()
    {
        string project = Path.Combine(home, "projects", "C--Users-me-source-repos-Northwind-Orders");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "5a4a7350-31ac-49b6-899f-c7d832d8ef63.jsonl"), "{}\n");
        ClaudeTranscripts transcripts = new(home);

        transcripts.HasMessages(SessionId.From("5a4a7350-31ac-49b6-899f-c7d832d8ef63")).ShouldBeTrue();
        transcripts.HasMessages(SessionId.From("00000000-0000-0000-0000-000000000000")).ShouldBeFalse();
    }

    [Fact]
    public void TreatAMissingProjectsFolderAsNoMessages()
    {
        ClaudeTranscripts transcripts = new(home);

        transcripts.HasMessages(SessionId.From("5a4a7350-31ac-49b6-899f-c7d832d8ef63")).ShouldBeFalse();
    }

    public void Dispose()
    {
        if (Directory.Exists(home))
        {
            Directory.Delete(home, recursive: true);
        }
    }
}