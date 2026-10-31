using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

/// <summary>What the update key does with what the update check knows: ask, or say why there is nothing to do.</summary>
public sealed class UpdateVantageShould
{
    [Fact]
    public void AskToUpdateWhenAnUpdateIsKnown()
    {
        ReleaseVersion newest = ReleaseVersions.Version("0.10.0");
        UpdateStatus available = new UpdateStatus.Available(ReleaseVersions.Version("0.9.0"), newest);

        UpdateVantage.Decide(available).ShouldBe(new UpdateDecision.Ask(newest));
    }

    [Fact]
    public void SayUpToDateOtherwise()
    {
        UpdateVantage.Decide(new UpdateStatus.UpToDate()).ShouldBe(new UpdateDecision.Refuse(new StatusMessage.UpToDate()));
    }

    [Fact]
    public void SayTheCheckIsOff()
    {
        UpdateVantage.Decide(new UpdateStatus.Off()).ShouldBe(new UpdateDecision.Refuse(new StatusMessage.UpdateCheckOff()));
    }
}