using Source191.Torni.Application;
using Source191.Torni.Domain;
using Source191.Torni.Tui;

namespace Source191.Torni.Core.Tests.Tui;

public sealed class OptionSelectorsShould
{
    [Fact]
    public void LabelEachMemberAsWordingDescribesIt()
    {
        OptionSelectors.Labels<LaunchMode>(Wording.Describe).ShouldBe(["Current branch", "New worktree"]);
        OptionSelectors.Labels<WorktreeBase>(Wording.Describe).ShouldBe(["origin's default branch, as last fetched", "the commit checked out now (HEAD)"]);
        OptionSelectors.Labels<EditorPreset>(Wording.Describe).Count.ShouldBe(Enum.GetValues<EditorPreset>().Length);
    }

    [Fact]
    public void ConvertBetweenIndexAndMemberByPosition()
    {
        OptionSelectors.Index(WorktreeBase.Head).ShouldBe(1);
        OptionSelectors.At<LaunchMode>(1).ShouldBe(LaunchMode.NewWorktree);
        OptionSelectors.At<LaunchMode>(null).ShouldBe(LaunchMode.CurrentBranch);
        OptionSelectors.At<EditorPreset>(OptionSelectors.Index(EditorPreset.Custom)).ShouldBe(EditorPreset.Custom);
    }
}