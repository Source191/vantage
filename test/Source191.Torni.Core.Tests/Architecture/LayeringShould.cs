using NetArchTest.Rules;
using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Architecture;

/// <summary>
/// The dependency direction: Domain at the bottom, Ports above it, Application above that, and Adapters and Tui on top,
/// with the tool project (Program.cs in the global namespace and its Composition folder) the only place that may know everything.
/// </summary>
public sealed class LayeringShould
{
    private const string Root = "Source191.Torni";

    private static Types Solution => Types.InAssemblies([typeof(Session).Assembly, typeof(Source191.Torni.Adapters.Files.Folders).Assembly, typeof(Source191.Torni.Tui.ConsoleScreen).Assembly]);

    [Fact]
    public void KeepDomainFreeOfEverythingElse()
    {
        Solution.That().ResideInNamespace($"{Root}.Domain")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Ports", $"{Root}.Application", $"{Root}.Adapters", $"{Root}.Tui", "Terminal.Gui", "System.IO.File", "System.IO.Directory", "System.Diagnostics.Process")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepTerminalGuiInsideTui()
    {
        Solution.That().ResideInNamespaceStartingWith(Root).And().DoNotResideInNamespaceStartingWith($"{Root}.Tui")
            .ShouldNot().HaveDependencyOn("Terminal.Gui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepFileSystemAndProcessesInsideAdapters()
    {
        Solution.That().ResideInNamespaceStartingWith(Root).And().DoNotResideInNamespaceStartingWith($"{Root}.Adapters")
            .ShouldNot().HaveDependencyOnAny("System.IO.File", "System.IO.Directory", "System.Diagnostics.Process")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepApplicationFreeOfAdaptersAndTui()
    {
        Solution.That().ResideInNamespaceStartingWith($"{Root}.Application")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Adapters", $"{Root}.Tui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepAdaptersFreeOfApplicationAndTui()
    {
        Solution.That().ResideInNamespaceStartingWith($"{Root}.Adapters")
            .ShouldNot().HaveDependencyOnAny($"{Root}.Application", $"{Root}.Tui")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void KeepPortsFreeOfApplication()
    {
        Solution.That().ResideInNamespaceStartingWith($"{Root}.Ports")
            .ShouldNot().HaveDependencyOn($"{Root}.Application")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }

    [Fact]
    public void HandTheFolderDeleterOnlyToWorktreeRemover()
    {
        Types everything = Types.InAssemblies([typeof(Session).Assembly, typeof(Source191.Torni.Adapters.Files.Folders).Assembly, typeof(Source191.Torni.Tui.ConsoleScreen).Assembly, typeof(Source191.Torni.TestDoubles.FakeFolders).Assembly]);

        IEnumerable<string> dependants = everything.That().HaveDependencyOn($"{Root}.Ports.IFolderDeleter")
            .GetTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal);

        dependants.ShouldBe([$"{Root}.Adapters.Files.Folders", $"{Root}.Application.Actions.WorktreeRemover", $"{Root}.TestDoubles.FakeFolders"]);
    }

    /// <summary>
    /// Boundary interfaces belong in Ports. Domain declares two: ITextValue (the three doors of a text value) and
    /// IProcessRegistry (the process table the tab rules read; implemented in Adapters, faked in TestDoubles).
    /// </summary>
    [Fact]
    public void DeclareNoDomainInterfaceButTheTwoNamed()
    {
        IEnumerable<string> interfaces = typeof(Session).Assembly.GetTypes()
            .Where(type => type.IsInterface && type.Namespace == $"{Root}.Domain" && !type.IsNested)
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal);

        interfaces.ShouldBe(["IProcessRegistry", "ITextValue`1"]);
    }

    [Fact]
    public void KeepSystemCommandLineInsideCommandLine()
    {
        Solution.That().ResideInNamespaceStartingWith(Root).And().DoNotHaveNameStartingWith("CommandLine")
            .ShouldNot().HaveDependencyOn("System.CommandLine")
            .GetResult().FailingTypeNames.ShouldBeNull();
    }
}