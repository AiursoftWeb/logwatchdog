# LogWatchDog

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.com/aiursoft/logwatchdog/-/blob/master/LICENSE)
[![Pipeline stat](https://badge-proxy.aiursoft.com/aiursoft/logwatchdog/badges/master/pipeline.svg)](https://gitlab.aiursoft.com/aiursoft/logwatchdog/-/pipelines)
[![Test Coverage](https://badge-proxy.aiursoft.com/aiursoft/logwatchdog/badges/master/coverage.svg)](https://gitlab.aiursoft.com/aiursoft/logwatchdog/-/pipelines)
[![S360 compliance](https://s360.aiursoft.com/badge.svg?project=aiursoft%2Flogwatchdog)](https://s360.aiursoft.com/SupplyChain?tab=projects&project=aiursoft%2Flogwatchdog&scope=baseline&ecosystem=all)
[![NuGet version (Aiursoft.LogWatchDog)](https://img.shields.io/nuget/v/Aiursoft.LogWatchDog.svg)](https://www.nuget.org/packages/Aiursoft.LogWatchDog/)
[![Man hours](https://manhours.aiursoft.com/r/github.com/aiursoftweb/logwatchdog.svg)](https://manhours.aiursoft.com/r/github.com/aiursoftweb/logwatchdog.html)

Monitors HTTP errors and abnormal request traffic from Caddy logs, reporting incidents to AlertCenter.

## Installation

Requirements:

1. [.NET 10 SDK](https://dot.net/)

Run the following command to install this tool:

```bash
dotnet tool install --global Aiursoft.LogWatchDog
```

## Usage

Set the environment variables described in the [configuration guide](https://gitlab.aiursoft.com/aiursoft/logwatchdog/-/blob/master/docs/configuration.md), then run:

```bash
log-watchdog
```

To check the last successful scan, run `log-watchdog --healthcheck`.

## Run locally

With the same environment variables configured, run:

```bash
dotnet run --project ./src/Aiursoft.LogWatchDog
```

## Run in Microsoft Visual Studio

1. Open `Aiursoft.LogWatchDog.sln`.
2. Select `Aiursoft.LogWatchDog` as the startup project and press `F5`.

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you with push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
