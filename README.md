# Aiursoft LogWatchDog

<a name="logwatchdog"></a>
[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AiursoftWeb/logwatchdog/blob/master/LICENSE)
[![Pipeline stat](https://badge-proxy.aiursoft.com/aiursoft/logwatchdog/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/logwatchdog/-/pipelines)
[![Test Coverage](https://badge-proxy.aiursoft.com/aiursoft/logwatchdog/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/logwatchdog/-/pipelines)
[![S360 compliance](https://s360.aiursoft.com/badge.svg?project=aiursoft%2Flogwatchdog)](https://s360.aiursoft.com/SupplyChain?tab=projects&project=aiursoft%2Flogwatchdog&scope=baseline&ecosystem=all)
[![NuGet version (Aiursoft.LogWatchDog)](https://img.shields.io/nuget/v/Aiursoft.LogWatchDog.svg)](https://www.nuget.org/packages/Aiursoft.LogWatchDog/)
[![Man hours](https://manhours.aiursoft.com/r/github.com/aiursoftweb/logwatchdog.svg)](https://manhours.aiursoft.com/r/github.com/aiursoftweb/logwatchdog.html)

Monitors HTTP errors and abnormal request traffic from Caddy logs, reporting incidents to AlertCenter.

## Overview

### What is LogWatchDog?

Monitor hostname errors and optional request-abuse evidence from Caddy logs in ClickHouse, and report incidents to AlertCenter. This is a long-running monitoring process.

## Installation

Install the CLI with the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0):

```bash
dotnet tool install --global Aiursoft.LogWatchDog
```

Update an existing installation:

```bash
dotnet tool update --global Aiursoft.LogWatchDog
```

Remove the CLI:

```bash
dotnet tool uninstall --global Aiursoft.LogWatchDog
```

**Deployment with Docker**

The image is built from this repository and runs the same monitoring process. Put the variables from [Configuration](#configuration) in a private environment file, then run one replica:

```bash
docker run -d --name log-watchdog \
  --env-file log-watchdog.env \
  -v log-watchdog-data:/data \
  aiursoft/logwatchdog:latest
```

The container runs as UID 1654. Keep `/data` writable and persistent across replacements so pending reports and monitoring history can resume. To check the last successful scans:

```bash
docker exec log-watchdog dotnet /app/log-watchdog.dll --healthcheck
```

## Usage

Set the environment variables described in the [configuration guide](https://github.com/AiursoftWeb/logwatchdog/blob/master/docs/configuration.md), then run:

```bash
log-watchdog
```

To check the last successful scan, run `log-watchdog --healthcheck`.

### Health check

Run `log-watchdog --healthcheck` in the same environment as the running monitor. Exit code `0` requires a recent hostname scan marker (less than three minutes old); when request-abuse detection is enabled, its marker must also be recent. Otherwise the command exits with `1`. It does not start a scan.

## Configuration

Monitors hostnames in a server's Caddy ClickHouse logs. Checks every minute using a complete fifteen-minute window, with a one-minute ingestion allowance. Daily P95/P99 thresholds use up to thirty days of history.

Sev3 requires three anomalous checks (at least 100 requests, 10 failures and max(P95, 1%) error rate). Three complete minutes at 90% failures, with at least five failures, also trigger sev3 during cold start. Sev2 additionally requires 150 failures, max(P99, 5%), three severe checks, and **at least 35 minutes since AlertCenter actually created the sev3**. Five healthy checks automatically mitigate; final resolution is manual.

Run one replica per server with:

- `SERVER_NAME`: stable server identity.
- `MONITORED_HOSTS`: `*` discovers all DNS hostnames observed in logs, including newly added domains; alternatively supply comma-separated exact hostnames. Wildcards cannot be mixed with a list. IP addresses and malformed hosts are excluded.
- `CLICKHOUSE_CONNECTION_STRING`: existing read-only access to `logs.caddy_requests`.
- `ALERTCENTER_URL` and `ALERTCENTER_API_KEY`: dedicated application credentials with query, create, severity, metadata and mitigate permissions.
- A persistent writable `/data` volume for UID 1654 (override with `DATA_DIRECTORY`).

History needs seven observed UTC dates. Incident thresholds are frozen; a cold-start incident learns its escalation threshold once history becomes available. Recovery requires a fifteen-minute rate below half the incident's sev3 threshold, a five-minute rate at or below that value, and recent response evidence. Empty traffic and stale collection do not prove recovery.

The source must deliver complete logs within the ingestion allowance. The bridge rejects stale node traffic, but request timestamps alone cannot prove ingestion completeness. It reads only aggregated counts, excludes status 101, and never creates or changes ClickHouse tables. Startup loads history; later refreshes append missing days rather than rescan thirty days each minute. Wildcard mode also includes valid DNS names supplied by clients, rather than a Caddy configuration allowlist. Use an explicit list to restrict scope. History stores only nonempty five-minute buckets; quiet hosts do not imply recovery.

Optional request-abuse detection uses the same application, credentials and volume. Set `HTTP_ABUSE_ENABLED=true` only after verifying trusted client IP attribution and timely, complete access logs. `HTTP_ABUSE_EXCLUDED_SOURCE_CIDRS` accepts comma-separated approved source networks (IPv6 /64 or broader); private/reserved sources are excluded automatically. IPv4 hosts and IPv6 /64 groups are evaluated symmetrically across servers.

The detector confirms sustained repeated downloads over 20/60 minutes or high request volume over five minutes. Suspected activity creates sev3; attributable server failures or measured latency degradation for other visitors can create or upgrade to sev2. One active abuse incident per server collects the evidence. Ten fresh complete minutes of low activity for every implicated source automatically mitigate, including after blocking. It never blocks traffic, resolves or reopens incidents. Confirmed reports persist through API outages and restarts; manual closure suppresses the same continuing activity until recovery. This feature is disabled by default and does not change the hostname error rules above.

`DATA_DIRECTORY` defaults to `/data`; `HTTP_ABUSE_ENABLED` defaults to `false`. `HTTP_ABUSE_EXCLUDED_SOURCE_CIDRS` is an optional comma-separated list. Configure these alongside the required server, hosts, ClickHouse and AlertCenter values above.

## Development

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

From the repository root, restore and build:

```bash
dotnet restore Aiursoft.LogWatchDog.sln
dotnet build Aiursoft.LogWatchDog.sln --no-restore
```

<a name="run-locally"></a>
<a name="local-run"></a>
Run the tool from source, with the prerequisites and configuration above:

```bash
dotnet run --project src/Aiursoft.LogWatchDog/Aiursoft.LogWatchDog.csproj
```

<a name="run-in-microsoft-visual-studio"></a>
In Microsoft Visual Studio, open `Aiursoft.LogWatchDog.sln`, select `Aiursoft.LogWatchDog` as the startup project and press `F5`. Configure the arguments and environment for the task you want to run.

Run the existing tests:

```bash
dotnet test Aiursoft.LogWatchDog.sln --no-restore
```


## Documentation

- [Configuration guide](https://github.com/AiursoftWeb/logwatchdog/blob/master/docs/configuration.md)

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you have push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.

## License

Licensed under the [MIT License](https://github.com/AiursoftWeb/logwatchdog/blob/master/LICENSE).
