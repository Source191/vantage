using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Reflection;

namespace Source191.Torni.Core.Tests.Architecture;

/// <summary>
/// A port takes the Domain value where one exists (SessionId, JobId, FolderPath, BranchName, SessionName) and adapters
/// read its Value. A parameter named for one of those values is never a string, unless its interface is kept here with
/// its reason.
/// </summary>
public sealed class PortParametersShould
{
    private static readonly string[] ValueNames = ["sessionId", "jobId", "directory", "repository", "worktree", "branch"];

    /// <summary>The interfaces kept on purpose, each with its reason. Fixing one removes its entry; <see cref="KeepNoStaleEntry"/> says so.</summary>
    private static readonly Dictionary<Type, string> Kept = new()
    {
        [typeof(IGit)] = "seventeen methods and about forty call sites; migrates in its own change",
    };

    [Fact]
    public void TakeDomainValues()
    {
        IEnumerable<string> strings = PortInterfaces()
            .Where(port => !Kept.ContainsKey(port))
            .SelectMany(StringParametersNamedForAValue);

        strings.ShouldBeEmpty();
    }

    [Fact]
    public void KeepNoStaleEntry()
    {
        IEnumerable<string> stale = Kept.Keys
            .Where(port => !StringParametersNamedForAValue(port).Any())
            .Select(port => port.Name);

        stale.ShouldBeEmpty();
    }

    [Fact]
    public void FindTheProbe()
    {
        StringParametersNamedForAValue(typeof(IProbe)).ShouldBe(["IProbe.Take(sessionId)"]);
    }

    private static IEnumerable<Type> PortInterfaces()
    {
        return typeof(IGit).Assembly.GetTypes()
            .Where(type => type.IsInterface && type.Namespace == "Source191.Torni.Ports");
    }

    private static IEnumerable<string> StringParametersNamedForAValue(Type port)
    {
        return port.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .SelectMany(method => method.GetParameters().Select(parameter => (method, parameter)))
            .Where(pair => pair.parameter.ParameterType == typeof(string) && ValueNames.Contains(pair.parameter.Name, StringComparer.Ordinal))
            .Select(pair => $"{port.Name}.{pair.method.Name}({pair.parameter.Name})");
    }

    public interface IProbe
    {
        void Take(string sessionId);

        void TakeTheValue(SessionId sessionId, string path);
    }
}