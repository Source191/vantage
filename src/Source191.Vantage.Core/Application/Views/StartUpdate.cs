using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Open the tab that waits for this Vantage to exit, updates the tool and starts it again; raised once the question is answered yes.</summary>
public sealed record StartUpdate(ReleaseVersion Newest) : ViewEffect;