# ============================
# Prepare Building Environment
FROM --platform=$BUILDPLATFORM hub.aiursoft.com/aiursoft/internalimages/dotnet AS build-env
ARG TARGETARCH
ARG NUGET_SOURCE=https://api.nuget.org/v3/index.json

WORKDIR /src
COPY . .
RUN if [ "$TARGETARCH" = "arm64" ]; then \
        RID="linux-arm64"; \
    elif [ "$TARGETARCH" = "amd64" ]; then \
        RID="linux-x64"; \
    else \
        RID="linux-$TARGETARCH"; \
    fi && \
    dotnet publish ./src/Aiursoft.LogWatchDog/Aiursoft.LogWatchDog.csproj \
        --configuration Release --no-self-contained --runtime "$RID" --output /app \
        -p:UseAppHost=false -p:GeneratePackageOnBuild=false \
        --configfile nuget.config -p:RestoreSources="$NUGET_SOURCE"

# ============================
# Prepare Runtime Environment
FROM hub.aiursoft.com/aiursoft/internalimages/dotnetonlyruntime
WORKDIR /app
COPY --from=build-env /app .
RUN mkdir -p /data && chown 1654:1654 /data && chmod 700 /data
USER 1654:1654
HEALTHCHECK --interval=60s --timeout=10s --start-period=15m --retries=3 CMD ["dotnet", "/app/log-watchdog.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "/app/log-watchdog.dll"]
