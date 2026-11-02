using NetArchTest.Rules;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Architecture;

/// <summary>
/// The dependency direction: Domain at the bottom, Ports above it, Application above that, and Adapters and Tui on top,
/// with the tool project (Program.cs in the global namespace and its Composition folder) the only place that may know everything.
/// </summary>
public sealed class LayeringShould
{
    private const string Root = "Source191.Vantage";

    private static Types Vantage => Types.InAssemblies([typeof(Session).Assembly, typeof(Source191.Vantage.Adapters.Files.Folders).Assembly, typeof(Source191.Vantage.Tui.ConsoleScreen).Assembly]);

    [Fact]
    public void KeepDomainFreeOfEverythingElse()
    {
        Vantage.That().ResideInNamespace($"{Root}.Domain")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Ports", $"{Root}.Application", $"{Root}.Adapters", $"{Root}.Tui", "Terminal.Gui", "System.IO.File", "System.IO.Directory", "System.Diagnostics.Process")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepTerminalGuiInsideTui()
    {
        Vantage.That().ResideInNamespaceStartingWith(Root).And().DoNotResideInNamespaceStartingWith($"{Root}.Tui")
            .ShouldNot().HaveDependencyOn("Terminal.Gui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepFileSystemAndProcessesInsideAdapters()
    {
        Vantage.That().ResideInNamespaceStartingWith(Root).And().DoNotResideInNamespaceStartingWith($"{Root}.Adapters")
            .ShouldNot().HaveDependencyOnAny("System.IO.File", "System.IO.Directory", "System.Diagnostics.Process")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepApplicationFreeOfAdaptersAndTui()
    {
        Vantage.That().ResideInNamespaceStartingWith($"{Root}.Application")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Adapters", $"{Root}.Tui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepAdaptersFreeOfApplicationAndTui()
    {
        Vantage.That().ResideInNamespaceStartingWith($"{Root}.Adapters")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Application", $"{Root}.Tui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepPortsFreeOfApplication()
    {
        Vantage.That().ResideInNamespaceStartingWith($"{Root}.Ports")
            .ShouldNot().HaveDependencyOn($"{Root}.Application")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepSystemCommandLineInsideCommandLine()
    {
        Vantage.That().ResideInNamespaceStartingWith(Root).And().DoNotHaveNameStartingWith("CommandLine")
            .ShouldNot().HaveDependencyOn("System.CommandLine")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }
}