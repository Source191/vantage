namespace Source191.Vantage.Domain;

public sealed record SessionListingEntry(int? ProcessId, string? Status, string? WaitingFor, string? State);