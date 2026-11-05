using Source191.Torni.Domain;

namespace Source191.Torni.TestDoubles;

/// <summary>Confirmed processes for tests that start from one: the listing's door, with the process started when the session did.</summary>
public static class TestProcesses
{
    public static ConfirmedProcess Confirmed(int processId, DateTimeOffset startedAt) =>
        ConfirmedProcess.TryConfirm(processId, startedAt, startedAt, out ConfirmedProcess? process)
            ? process
            : throw new InvalidOperationException("A process confirmed against its own start time is always confirmed");

    /// <summary>The process as the registry reports it running; the registry must know its start time.</summary>
    public static ConfirmedProcess Confirmed(IProcessRegistry processes, int processId)
    {
        return Confirmed(processId, processes.StartTimeOf(processId) ?? throw new InvalidOperationException($"Process {processId} is not running in the fake"));
    }
}