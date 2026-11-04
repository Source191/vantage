using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Domain;

/// <summary>A process is ended only after one of three checks has confirmed it is still the one that was recorded.</summary>
public sealed class ConfirmedProcessShould
{
    [Fact]
    public void ConfirmAProcessStartedNoLaterThanTheSessionPlusFiveSeconds()
    {
        ConfirmedProcess.TryConfirm(4242, Fixtures.Noon.AddSeconds(-3), sessionStartedAt: Fixtures.Noon, out ConfirmedProcess? before).ShouldBeTrue();
        ConfirmedProcess.TryConfirm(4242, Fixtures.Noon.AddSeconds(5), sessionStartedAt: Fixtures.Noon, out ConfirmedProcess? rounded).ShouldBeTrue();

        before.Id.ShouldBe(4242);
        before.StartedAt.ShouldBe(Fixtures.Noon.AddSeconds(-3));
        rounded.StartedAt.ShouldBe(Fixtures.Noon.AddSeconds(5));
    }

    [Fact]
    public void RefuseAProcessStartedLaterThanThat()
    {
        ConfirmedProcess.TryConfirm(4242, Fixtures.Noon.AddSeconds(6), sessionStartedAt: Fixtures.Noon, out _).ShouldBeFalse();
        ConfirmedProcess.TryConfirm(4242, Fixtures.Noon.AddMinutes(10), sessionStartedAt: Fixtures.Noon, out _).ShouldBeFalse();
    }

    [Fact]
    public void RefuseAProcessWhoseStartCannotBeRead()
    {
        ConfirmedProcess.TryConfirm(4242, processStartedAt: null, sessionStartedAt: Fixtures.Noon, out _).ShouldBeFalse();
    }

    [Fact]
    public void ConfirmAClaimOnlyWhenTheStartTimeIsExactlyTheRecordedOne()
    {
        ConfirmedProcess.TryConfirmClaim(4242, Fixtures.Noon, claimedStartedAt: Fixtures.Noon, out ConfirmedProcess? exact).ShouldBeTrue();

        exact.Id.ShouldBe(4242);
        exact.StartedAt.ShouldBe(Fixtures.Noon);
        ConfirmedProcess.TryConfirmClaim(4242, Fixtures.Noon.AddTicks(1), claimedStartedAt: Fixtures.Noon, out _).ShouldBeFalse();
        ConfirmedProcess.TryConfirmClaim(4242, Fixtures.Noon.AddTicks(-1), claimedStartedAt: Fixtures.Noon, out _).ShouldBeFalse();
        ConfirmedProcess.TryConfirmClaim(4242, processStartedAt: null, claimedStartedAt: Fixtures.Noon, out _).ShouldBeFalse();
    }

    [Fact]
    public void ConfirmAParentStartedNoLaterThanItsConfirmedChild()
    {
        ConfirmedProcess.TryConfirm(4242, Fixtures.Noon, Fixtures.Noon, out ConfirmedProcess? child).ShouldBeTrue();

        ConfirmedProcess.TryConfirmParent(4000, Fixtures.Noon.AddMinutes(-1), child, out ConfirmedProcess? older).ShouldBeTrue();
        ConfirmedProcess.TryConfirmParent(4001, Fixtures.Noon, child, out ConfirmedProcess? sameTime).ShouldBeTrue();

        older.Id.ShouldBe(4000);
        older.StartedAt.ShouldBe(Fixtures.Noon.AddMinutes(-1));
        sameTime.Id.ShouldBe(4001);
    }

    [Fact]
    public void RefuseAParentYoungerThanItsChildOrWhoseStartCannotBeRead()
    {
        ConfirmedProcess.TryConfirm(4242, Fixtures.Noon, Fixtures.Noon, out ConfirmedProcess? child).ShouldBeTrue();

        ConfirmedProcess.TryConfirmParent(4000, Fixtures.Noon.AddTicks(1), child, out _).ShouldBeFalse();
        ConfirmedProcess.TryConfirmParent(4000, parentStartedAt: null, child, out _).ShouldBeFalse();
    }
}