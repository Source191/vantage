using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Views;

/// <summary>The new-session wizard's rules: what is offered, chosen and previewed, where focus goes, and what starts.</summary>
public sealed class WizardViewShould
{
    private const string Root = @"C:\Users\me\source\repos";
    private static readonly string Agents = Path.Combine(Root, "Northwind.Agents");
    private static readonly string Billing = Path.Combine(Root, "Northwind.Billing");
    private static readonly string Templates = Path.Combine(Root, "Northwind.Templates");

    private readonly FakeFolders folders = new FakeFolders()
        .WithDirectory(Path.Combine(Agents, ".git"))
        .WithDirectory(Path.Combine(Billing, ".git"))
        .WithDirectory(Path.Combine(Templates, ".git"));

    private WizardState Opened => WizardState.Open(new RepositoryCatalogue(folders, [Root]), [Templates], folders, WorktreeBase.RemoteDefault);

    private WizardState InAgents => Reduce(Opened, new WizardInput.FilterChanged("agents"));

    private WizardState FixedInBilling => WizardState.Open(new RepositoryCatalogue(folders, [Root]), [], folders, WorktreeBase.Head, new FolderPath(Billing));

    // 1.1 Choices, filter and the list.

    [Fact]
    public void OpenWithRecentRepositoriesFirstAndTheFirstChoiceSelected()
    {
        WizardState opened = Opened;

        opened.Choices.Select(choice => choice.Name).ShouldBe(["Northwind.Templates", "Northwind.Agents", "Northwind.Billing", "repos (no repository)"]);
        opened.Chosen!.Name.ShouldBe("Northwind.Templates");
        opened.Focus.ShouldBe(WizardField.RepositoryFilter);
        opened.Problem.ShouldBeNull();
        opened.Mode.ShouldBe(LaunchMode.CurrentBranch);
        opened.Base.ShouldBe(WorktreeBase.RemoteDefault);
    }

    [Fact]
    public void NarrowChoicesAsTheFilterChanges()
    {
        WizardState moved = Reduce(Opened, new WizardInput.ListMoved(2));

        WizardState narrowed = Reduce(moved, new WizardInput.FilterChanged("BILL"));

        narrowed.Choices.Select(choice => choice.Name).ShouldBe(["Northwind.Billing"]);
        narrowed.SelectedIndex.ShouldBe(0, "a new filter selects its first choice");
        narrowed.Chosen!.Name.ShouldBe("Northwind.Billing");
        Reduce(narrowed, new WizardInput.FilterChanged("nothing matches")).ShouldSatisfyAllConditions(
            empty => empty.Choices.ShouldBeEmpty(),
            empty => empty.SelectedIndex.ShouldBeNull(),
            empty => empty.Chosen.ShouldBeNull());
    }

    [Fact]
    public void MoveAndPageTheListWithinBounds()
    {
        Reduce(Opened, new WizardInput.ListMoved(1)).Chosen!.Name.ShouldBe("Northwind.Agents");
        Reduce(Opened, new WizardInput.ListMoved(20)).Chosen!.Name.ShouldBe("repos (no repository)", "a page past the end stops on the last choice");
        Reduce(Opened, new WizardInput.ListMoved(-1)).Chosen!.Name.ShouldBe("Northwind.Templates", "the first choice is as far up as it goes");
        Reduce(Opened, new WizardInput.ChoiceSelected(2)).Chosen!.Name.ShouldBe("Northwind.Billing", "the list's own selection is the choice");
        Reduce(Opened, new WizardInput.ChoiceSelected(9)).Chosen!.Name.ShouldBe("Northwind.Templates", "an index outside the list changes nothing");
    }

    // 1.2 Confirming the repository.

    [Fact]
    public void ConfirmTheRepositoryAndMoveFocusToName()
    {
        WizardState refused = Reduce(Reduce(Opened, new WizardInput.FilterChanged("nothing matches")), new WizardInput.ConfirmRepository());
        WizardState chosen = Reduce(Reduce(refused, new WizardInput.FilterChanged("agents")), new WizardInput.ConfirmRepository());

        chosen.Focus.ShouldBe(WizardField.Name);
        chosen.Problem.ShouldBeNull("confirming clears an earlier problem");
        chosen.Chosen!.Name.ShouldBe("Northwind.Agents");
    }

    [Fact]
    public void RefuseToConfirmWithNoChoice()
    {
        WizardState listed = Reduce(Opened, new WizardInput.ListMoved(1));
        WizardState empty = Reduce(listed, new WizardInput.FilterChanged("nothing matches"));

        WizardState refused = Reduce(empty, new WizardInput.ConfirmRepository());

        refused.Problem.ShouldBe(new WizardProblem.NoRepository());
        refused.Focus.ShouldBe(WizardField.RepositoryFilter);
    }

    [Fact]
    public void KeepAProblemWhileTheUserTypes()
    {
        WizardState refused = Reduce(Reduce(Opened, new WizardInput.FilterChanged("nothing matches")), new WizardInput.ConfirmRepository());

        Reduce(refused, new WizardInput.FilterChanged("agents")).Problem.ShouldBe(new WizardProblem.NoRepository());
    }

    // 1.3 The preview.

    [Fact]
    public void PreviewFollowsNameModeAndSelection()
    {
        InAgents.Preview.ShouldBe("Runs in Northwind.Agents on its current branch; the name is only the session's label.");

        WizardState worktree = Reduce(InAgents, new WizardInput.ModeChanged(LaunchMode.NewWorktree));
        worktree.Preview.ShouldBe("The name becomes the worktree folder Northwind.Agents-<name> and its branch.");

        WizardState named = Reduce(worktree, new WizardInput.NameChanged("Fix 123"));
        named.Preview.ShouldBe("Worktree Northwind.Agents-fix-123 beside the repository, on branch fix-123.");

        WizardState elsewhere = Reduce(named, new WizardInput.FilterChanged("billing"));
        elsewhere.Preview.ShouldBe("Worktree Northwind.Billing-fix-123 beside the repository, on branch fix-123.");

        Reduce(named, new WizardInput.FilterChanged("nothing matches")).Preview.ShouldBe(string.Empty, "with nothing chosen there is nothing to preview");
    }

    // 1.4 Enter moves on.

    [Fact]
    public void NextFieldMovesNameToMode()
    {
        WizardState inName = Reduce(InAgents, new WizardInput.ConfirmRepository());

        Reduce(inName, new WizardInput.NextField()).Focus.ShouldBe(WizardField.Mode);
        Reduce(Reduce(inName, new WizardInput.FieldFocused(WizardField.Prompt)), new WizardInput.NextField()).Focus.ShouldBe(WizardField.Prompt, "Enter in the prompt is a new line, not a move");
    }

    [Fact]
    public void FollowFocusMovedByTheTerminal()
    {
        Reduce(InAgents, new WizardInput.FieldFocused(WizardField.Base)).Focus.ShouldBe(WizardField.Base);
    }

    // 1.5 Start and cancel.

    [Fact]
    public void StartRefusesNoRepositoryBlankNameAndPlanProblemsWithTheRightFocus()
    {
        WizardState nothingChosen = Reduce(Opened, new WizardInput.FilterChanged("nothing matches"));
        WizardUpdate noRepository = WizardView.Reduce(nothingChosen, new WizardInput.Start(OpenInTab: false));
        noRepository.ShouldSatisfyAllConditions(
            update => update.Effect.ShouldBeNull(),
            update => update.State.Problem.ShouldBe(new WizardProblem.NoRepository()),
            update => update.State.Focus.ShouldBe(WizardField.RepositoryFilter));

        WizardState blank = Reduce(Reduce(InAgents, new WizardInput.FieldFocused(WizardField.Prompt)), new WizardInput.NameChanged("   "));
        WizardUpdate noName = WizardView.Reduce(blank, new WizardInput.Start(OpenInTab: false));
        noName.ShouldSatisfyAllConditions(
            update => update.Effect.ShouldBeNull(),
            update => update.State.Problem.ShouldBe(new WizardProblem.Refused(new LaunchProblem.InvalidName(NameProblem.Missing))),
            update => update.State.Focus.ShouldBe(WizardField.Name));

        WizardState noRepositoryWorktree = Reduce(Reduce(Reduce(Opened, new WizardInput.FilterChanged("no repository")), new WizardInput.ModeChanged(LaunchMode.NewWorktree)), new WizardInput.NameChanged("fix-123"));
        WizardUpdate refused = WizardView.Reduce(noRepositoryWorktree, new WizardInput.Start(OpenInTab: false));
        refused.ShouldSatisfyAllConditions(
            update => update.Effect.ShouldBeNull(),
            update => update.State.Problem.ShouldBe(new WizardProblem.Refused(new LaunchProblem.WorktreeNeedsRepository())),
            update => update.State.Focus.ShouldBe(WizardField.Name));
    }

    [Fact]
    public void StartYieldsALaunchEffect()
    {
        WizardState filled = Fill(InAgents);

        WizardUpdate started = WizardView.Reduce(filled, new WizardInput.Start(OpenInTab: false));

        WizardEffect.Launch launch = started.Effect.ShouldBeOfType<WizardEffect.Launch>();
        launch.OpenInTab.ShouldBeFalse();
        launch.Plan.ShouldSatisfyAllConditions(
            plan => plan.Directory.Value.ShouldBe(@"C:\Users\me\source\repos\Northwind.Agents-fix-123"),
            plan => plan.Name.Text.ShouldBe("Fix 123"),
            plan => plan.Worktree.ShouldNotBeNull(),
            plan => plan.ReuseExistingWorktree.ShouldBeFalse(),
            plan => plan.Base.ShouldBe(WorktreeBase.Head),
            plan => plan.OpeningPrompt.ShouldBe("Read the README"));
    }

    [Fact]
    public void StartReusesAWorktreeFolderThatExists()
    {
        folders.WithDirectory(@"C:\Users\me\source\repos\Northwind.Agents-fix-123");

        WizardUpdate started = WizardView.Reduce(Fill(InAgents), new WizardInput.Start(OpenInTab: false));

        started.Effect.ShouldBeOfType<WizardEffect.Launch>().Plan.ReuseExistingWorktree.ShouldBeTrue();
    }

    [Fact]
    public void StartAndOpenInTabYieldsALaunchEffectWithOpenInTab()
    {
        WizardUpdate started = WizardView.Reduce(Fill(InAgents), new WizardInput.Start(OpenInTab: true));

        started.Effect.ShouldBeOfType<WizardEffect.Launch>().OpenInTab.ShouldBeTrue();
    }

    [Fact]
    public void CancelYieldsClose()
    {
        WizardUpdate cancelled = WizardView.Reduce(Fill(InAgents), new WizardInput.Cancel());

        cancelled.Effect.ShouldBe(new WizardEffect.Close());
    }

    // 1.6 A fixed directory.

    [Fact]
    public void FixedDirectoryHidesTheChooserAndWorktreeControls()
    {
        WizardState fixedInBilling = FixedInBilling;

        fixedInBilling.ShowsChooser.ShouldBeFalse();
        Opened.ShowsChooser.ShouldBeTrue();
        fixedInBilling.Focus.ShouldBe(WizardField.Name);
        fixedInBilling.Chosen.ShouldBe(new RepositoryChoice("Northwind.Billing", new FolderPath(Billing), IsRepository: true));
        fixedInBilling.Base.ShouldBe(WorktreeBase.Head, "the default base still comes from settings");
        Reduce(fixedInBilling, new WizardInput.NextField())
            .Focus.ShouldBe(WizardField.Name, "the Where choice is hidden, so Enter has nowhere to go");
    }

    [Fact]
    public void FixedDirectoryStartsInThatFolder()
    {
        WizardState named = Reduce(FixedInBilling, new WizardInput.NameChanged("Refund retry policy"));

        WizardUpdate started = WizardView.Reduce(named, new WizardInput.Start(OpenInTab: false));

        LaunchPlan plan = started.Effect.ShouldBeOfType<WizardEffect.Launch>().Plan;
        plan.Directory.Value.ShouldBe(Billing);
        plan.Worktree.ShouldBeNull();
        named.Preview.ShouldBe("Runs in Northwind.Billing on its current branch; the name is only the session's label.");
    }

    // 1.7 One list, two ways in.

    [Fact]
    public void ArrowsInTheFilterDriveTheListAndLettersInTheListReturnToTheFilter()
    {
        WizardState inList = Reduce(Opened, new WizardInput.ListMoved(1));
        inList.Focus.ShouldBe(WizardField.RepositoryList);
        inList.Chosen!.Name.ShouldBe("Northwind.Agents");

        Reduce(inList, new WizardInput.FilterChanged("t")).Focus.ShouldBe(WizardField.RepositoryFilter);

        WizardState emptyList = Reduce(Opened, new WizardInput.FilterChanged("nothing matches"));
        Reduce(emptyList, new WizardInput.ListMoved(1)).Focus.ShouldBe(WizardField.RepositoryList, "an arrow still moves focus to an empty list");
    }

    // 1.8 Echoes from the controls are harmless.

    public static TheoryData<WizardInput> Echoes =>
    [
        new WizardInput.FilterChanged("north"),
        new WizardInput.ChoiceSelected(1),
        new WizardInput.NameChanged("Fix 123"),
        new WizardInput.ModeChanged(LaunchMode.NewWorktree),
        new WizardInput.BaseChanged(WorktreeBase.Head),
        new WizardInput.PromptChanged("Read the README"),
        new WizardInput.FieldFocused(WizardField.Prompt),
        new WizardInput.ConfirmRepository(),
    ];

    [Theory]
    [MemberData(nameof(Echoes))]
    public void BeIdempotentForARepeatedInput(WizardInput input)
    {
        WizardState moved = Reduce(Reduce(Opened, new WizardInput.FilterChanged("north")), new WizardInput.ListMoved(2));
        WizardState once = Reduce(moved, input);

        Reduce(once, input).ShouldBe(once);
    }

    [Fact]
    public void KeepTheMovedSelectionWhenTheSameFilterTextArrivesAgain()
    {
        WizardState moved = Reduce(Reduce(Opened, new WizardInput.FilterChanged("north")), new WizardInput.ListMoved(2));

        Reduce(moved, new WizardInput.FilterChanged("north")).SelectedIndex.ShouldBe(2);
    }

    private static WizardState Fill(WizardState state)
    {
        return new WizardInput[]
        {
            new WizardInput.NameChanged("Fix 123"),
            new WizardInput.ModeChanged(LaunchMode.NewWorktree),
            new WizardInput.BaseChanged(WorktreeBase.Head),
            new WizardInput.PromptChanged("Read the README"),
        }.Aggregate(state, Reduce);
    }

    private static WizardState Reduce(WizardState state, WizardInput input) => WizardView.Reduce(state, input).State;
}