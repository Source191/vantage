using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Open the newest release's GitHub page in the default browser, from the What's new dialog, which stays open.</summary>
public sealed record OpenReleasePage(ReleaseVersion Version) : ViewEffect;