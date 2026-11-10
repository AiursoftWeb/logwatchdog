using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace Aiursoft.LogWatchDog.Tests;

internal sealed class TestEnvironment : IDisposable
{
    private readonly Dictionary<string, string?> original = new();
    internal string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    internal TestEnvironment() => Directory.CreateDirectory(DirectoryPath);

    internal void Set(string name, string? value)
    {
        original.TryAdd(name, Environment.GetEnvironmentVariable(name));
        Environment.SetEnvironmentVariable(name, value);
    }

    public void Dispose()
    {
        foreach (var pair in original) Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        Directory.Delete(DirectoryPath, true);
    }
}
