using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.LogWatchDog.Tests;

[TestClass]
public sealed class RuntimeContractTests
{
    private TestEnvironment environment = null!;

    [TestInitialize]
    public void Initialize()
    {
        environment = new TestEnvironment();
        environment.Set("SERVER_NAME", "test-server");
        environment.Set("MONITORED_HOSTS", "*");
        environment.Set("CLICKHOUSE_CONNECTION_STRING", "Host=localhost");
        environment.Set("ALERTCENTER_URL", "https://alerts.example/");
        environment.Set("ALERTCENTER_API_KEY", "test-key");
        environment.Set("DATA_DIRECTORY", environment.DirectoryPath);
        environment.Set("HTTP_ABUSE_ENABLED", null);
        environment.Set("HTTP_ABUSE_EXCLUDED_SOURCE_CIDRS", null);
        File.Delete(Program.HealthFile);
        File.Delete(AbuseScanner.HealthFile);
    }

    [TestCleanup]
    public void Cleanup()
    {
        File.Delete(Program.HealthFile);
        File.Delete(AbuseScanner.HealthFile);
        environment.Dispose();
    }

    [TestMethod]
    public void ExistingEnvironmentAndSourceIdentityArePreserved()
    {
        var settings = Settings.Load();
        Assert.IsTrue(settings.AllHosts);
        Assert.IsFalse(settings.AbuseEnabled);
        Assert.AreEqual("http-error-bridge:v1:test-server", settings.Source);
        Assert.AreEqual("caddy-abuse:v1:test-server", settings.AbuseSource);
        Assert.AreEqual(environment.DirectoryPath, settings.DataDirectory);
    }

    [TestMethod]
    [DataRow("*,app.example")]
    [DataRow("")]
    [DataRow("127.0.0.1")]
    public void InvalidHostnameScopeIsRejected(string hosts)
    {
        environment.Set("MONITORED_HOSTS", hosts);
        Assert.ThrowsExactly<InvalidOperationException>(() => Settings.Load());
    }

    [TestMethod]
    public void ExactHostsAreNormalizedAndDeduplicated()
    {
        environment.Set("MONITORED_HOSTS", " B.example, a.example,b.example ");
        CollectionAssert.AreEqual(new[] { "a.example", "b.example" }, Settings.Load().Hosts);
    }

    [TestMethod]
    public void InvalidAbuseSwitchIsNotSilentlyIgnored()
    {
        environment.Set("HTTP_ABUSE_ENABLED", "maybe");
        Assert.ThrowsExactly<InvalidOperationException>(() => Settings.Load());
    }

    [TestMethod]
    public void PersistentIncidentIdentitySurvivesToolRestart()
    {
        var settings = Settings.Load();
        var first = new StateStore(settings);
        var id = first.Prepare("app.example");
        Assert.AreEqual(id, new StateStore(settings).Find("app.example"));
        Assert.AreEqual(id, first.Prepare("app.example"));
    }

    [TestMethod]
    public void PersistentDirectoryKeepsOneWriter()
    {
        var store = new StateStore(Settings.Load());
        using var lease = store.Acquire();
        Assert.ThrowsExactly<IOException>(() => store.Acquire());
    }

    [TestMethod]
    public void MissingTrafficIsNotHealthyEvidence()
    {
        var empty = new Observation(DateTimeOffset.UtcNow, new Counts[15]);
        Assert.IsFalse(empty.Healthy(0.05));
        Assert.IsFalse(empty.Blackout);
    }

    [TestMethod]
    public void MissingMinuteBreaksConsecutiveFailureEvidence()
    {
        var sequence = new Sequence();
        Assert.IsFalse(sequence.Step(10, true, 3));
        Assert.IsFalse(sequence.Step(11, true, 3));
        Assert.IsFalse(sequence.Step(13, true, 3));
        Assert.AreEqual(1, sequence.Count);
    }

    [TestMethod]
    public async Task HealthcheckWithoutCredentialsRejectsMissingEvidence()
    {
        environment.Set("ALERTCENTER_API_KEY", null);
        Assert.AreEqual(1, await Program.Main(["--healthcheck"]));
    }

    [TestMethod]
    public async Task AbuseModeRequiresBothFreshHealthFiles()
    {
        environment.Set("HTTP_ABUSE_ENABLED", "true");
        await File.WriteAllTextAsync(Program.HealthFile, "test");
        Assert.AreEqual(1, await Program.Main(["--healthcheck"]));
        await File.WriteAllTextAsync(AbuseScanner.HealthFile, "test");
        Assert.AreEqual(0, await Program.Main(["--healthcheck"]));
    }

    [TestMethod]
    [DataRow(-4)]
    [DataRow(1)]
    public async Task OldOrFutureHealthEvidenceIsRejected(int minutes)
    {
        await File.WriteAllTextAsync(Program.HealthFile, "test");
        File.SetLastWriteTimeUtc(Program.HealthFile, DateTime.UtcNow.AddMinutes(minutes));
        Assert.AreEqual(1, await Program.Main(["--healthcheck"]));
    }
}
