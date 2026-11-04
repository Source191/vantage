namespace Source191.Torni.Application.TerminalSetup;

/// <summary>What <c>torni setup-terminal</c> was asked to do; an omitted sound means "use the default".</summary>
public sealed record TerminalSetupRequest(SoundArgument Sound, SoundArgument SessionSound, bool Yes, bool Remove);