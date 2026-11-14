using System.Net;
using System.Text;
using Aiursoft.AlertCenter.SDK;
using Aiursoft.AlertCenter.SDK.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aiursoft.LogWatchDog.Tests;

[TestClass]
public sealed class IncidentRecoveryTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task FiveHealthyScansMitigateAnExistingIncidentAcrossReporterScopes(bool sparse)
    {
        using var environment = new TestEnvironment();
        var settings = new Settings("test", ["*"], "", "https://alerts.example", "test-key", environment.DirectoryPath);
        var store = new StateStore(settings);
        var history = new History(settings);
        using var handler = new RecoveryApi(settings.Source);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAlertCenterSdk(settings.AlertCenterUrl, settings.AlertCenterKey);
        services.AddSingleton<IHttpClientFactory>(new RecoveryClientFactory(handler));
        await using var provider = services.BuildServiceProvider();
        var minute = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        for (var i = 0; i < 5; i++)
        {
            await using var scope = provider.CreateAsyncScope();
            var reporter = new Reporter(scope.ServiceProvider.GetRequiredService<AlertCenterApiClient>(), settings,
                store, NullLogger<Reporter>.Instance, TimeProvider.System);
            var cutoff = DateTimeOffset.FromUnixTimeSeconds((minute + i - 1) * 60);
            var buckets = sparse
                ? new[] { new Bucket("nopkg.example", (minute - 2) * 60, new Counts(1, 0)) }
                : new[] { new Bucket("nopkg.example", cutoff.AddMinutes(-1).ToUnixTimeSeconds(), new Counts(100, 0)) };
            await reporter.Scan(new RecentLog(cutoff, buckets), history, minute + i, cutoff, CancellationToken.None);
            Assert.AreEqual(i == 4 ? 1 : 0, handler.Mitigations);
        }
        Assert.AreEqual(5, handler.Queries);
    }

    [TestMethod]
    public async Task ANewFailureBreaksRecoveryAndSilenceDoesNotMitigate()
    {
        using var environment = new TestEnvironment();
        var settings = new Settings("test", ["*"], "", "https://alerts.example", "test-key", environment.DirectoryPath);
        using var handler = new RecoveryApi(settings.Source);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAlertCenterSdk(settings.AlertCenterUrl, settings.AlertCenterKey);
        services.AddSingleton<IHttpClientFactory>(new RecoveryClientFactory(handler));
        await using var provider = services.BuildServiceProvider();
        var reporter = new Reporter(provider.GetRequiredService<AlertCenterApiClient>(), settings, new StateStore(settings),
            NullLogger<Reporter>.Instance, TimeProvider.System);
        var minute = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        for (var i = 0; i < 9; i++)
        {
            var samples = new Counts[15];
            samples[^1] = new Counts(100, i == 4 ? 1UL : 0UL);
            await reporter.Reconcile("nopkg.example", new Observation(DateTimeOffset.UtcNow, samples), null,
                handler.Incident, minute + i, CancellationToken.None);
        }
        await reporter.Reconcile("nopkg.example", new Observation(DateTimeOffset.UtcNow, new Counts[15]), null,
            handler.Incident, minute + 9, CancellationToken.None);
        Assert.AreEqual(0, handler.Mitigations);
    }

    private sealed class RecoveryClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecoveryApi(string source) : HttpMessageHandler
    {
        internal IncidentDetails Incident { get; } = new()
        {
            IncidentId = Guid.NewGuid(), Version = Guid.NewGuid(), Status = "new", Level = "sev3",
            ReceivedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
            Metadata = new JObject { ["source"] = source, ["host"] = "nopkg.example",
                ["policy"] = new JObject { ["Alpha"] = 0.01, ["ServerDisplay"] = "test" } }
        };
        internal int Queries { get; private set; }
        internal int Mitigations { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual("Bearer test-key", request.Headers.Authorization!.ToString());
            object response;
            if (request.Method == HttpMethod.Get)
            {
                Queries++;
                response = new { Code = 0, Message = "Queried", ProtocolVersion = "10.0.34", TotalCount = 1, Items = new[] { Incident } };
            }
            else
            {
                Assert.AreEqual($"/api/v1/incidents/{Incident.IncidentId}/mitigate", request.RequestUri!.AbsolutePath);
                var body = JObject.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                Assert.AreEqual(Incident.Version, body["version"]!.ToObject<Guid>());
                StringAssert.Contains(body.Value<string>("reason")!, "Five consecutive healthy checks");
                Mitigations++;
                response = new { Code = 0, Message = "Mitigated", ProtocolVersion = "10.0.34" };
            }
            return new HttpResponseMessage(HttpStatusCode.Created)
            { Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json") };
        }
    }
}
