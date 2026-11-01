namespace Source191.Vantage.Application.TerminalSetup;

/// <summary>What <c>vantage setup-terminal</c> was asked to do; an omitted sound means "use the default".</summary>
public sealed record TerminalSetupRequest(SoundArgument Sound, SoundArgument SessionSound, bool Yes, bool Remove);