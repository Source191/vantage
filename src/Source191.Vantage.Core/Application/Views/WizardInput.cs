using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>The wizard's fields, in the order Tab visits them.</summary>
public enum WizardField
{
    RepositoryFilter,
    RepositoryList,
    Name,
    Mode,
    Base,
    Prompt,
}

/// <summary>What happened in the wizard: each case is one control event or key, raised by the dialog that binds them.</summary>
public abstract record WizardInput
{
    /// <summary>The repository filter's text changed, typed there or forwarded from a letter typed in the list.</summary>
    public sealed record FilterChanged(string Text) : WizardInput;

    /// <summary>An arrow or page key typed in the filter; the dialog passes the page size as the delta for a page key.</summary>
    public sealed record ListMoved(int Delta) : WizardInput;

    /// <summary>The list's own selection changed, by its own keys or a click.</summary>
    public sealed record ChoiceSelected(int Index) : WizardInput;

    public sealed record NameChanged(string Text) : WizardInput;

    public sealed record ModeChanged(LaunchMode Mode) : WizardInput;

    public sealed record BaseChanged(WorktreeBase Base) : WizardInput;

    public sealed record PromptChanged(string Text) : WizardInput;

    /// <summary>A field took focus, by Tab, a click or the dialog itself.</summary>
    public sealed record FieldFocused(WizardField Field) : WizardInput;

    /// <summary>Enter in the filter or the list.</summary>
    public sealed record ConfirmRepository : WizardInput;

    /// <summary>Enter in a one-line field.</summary>
    public sealed record NextField : WizardInput;

    /// <summary>F5, or F6 to open the session in its own tab as well.</summary>
    public sealed record Start(bool OpenInTab) : WizardInput;

    /// <summary>Esc or the Cancel button.</summary>
    public sealed record Cancel : WizardInput;
}

/// <summary>What the dialog does after a reduce: nothing, start the plan, or close.</summary>
public abstract record WizardEffect
{
    public sealed record Launch(LaunchPlan Plan, bool OpenInTab) : WizardEffect;

    public sealed record Close : WizardEffect;
}

/// <summary>Why the wizard will not go on, as a fact; <see cref="Wording"/> says it.</summary>
public abstract record WizardProblem
{
    public sealed record NoRepository : WizardProblem;

    public sealed record NoName : WizardProblem;

    /// <summary>The plan builder said no, for its own reason.</summary>
    public sealed record Refused(LaunchProblem Problem) : WizardProblem;
}

public sealed record WizardUpdate(WizardState State, WizardEffect? Effect);