using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

/// <summary>What the update key does with what the update check knows: ask, or say why there is nothing to do.</summary>
public sealed class UpdateVantageShould
{
    [Fact]
    public void AskToUpdateWhenAnUpdateIsKnown()
    {
        UpdateStatus available = new UpdateStatus.Available(ReleaseVersions.Version("0.9.0"), ReleaseVersions.Version("0.10.0"));

        UpdateVantage.Refusal(available).ShouldBeNull();
    }

    [Fact]
    public void SayUpToDateOtherwise()
    {
        UpdateVantage.Refusal(new UpdateStatus.UpToDate()).ShouldBe(new StatusMessage.UpToDate());
    }

    [Fact]
    public void SayTheCheckIsOff()
    {
        UpdateVantage.Refusal(new UpdateStatus.Off()).ShouldBe(new StatusMessage.UpdateCheckOff());
    }
}