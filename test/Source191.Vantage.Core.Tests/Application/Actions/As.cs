using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Actions;

/// <summary>A fixture row's session as the kind an effect takes; the cast fails the test if the fixture is the other kind.</summary>
internal static class As
{
    public static BackgroundSession Job(SessionRow row) => (BackgroundSession)row.Session;

    public static InteractiveSession Terminal(SessionRow row) => (InteractiveSession)row.Session;
}