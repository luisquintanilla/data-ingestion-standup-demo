FROM node:24.22.1-bookworm-slim AS node
FROM mcr.microsoft.com/dotnet/sdk:10.0.403-noble

# The official SDK image includes PowerShell, Git, curl and the .NET runtime.
COPY --from=node /usr/local/ /usr/local/

RUN test "$(dotnet --version)" = "10.0.403" \
    && test "$(node --version)" = "v24.22.1" \
    && for tool in bash git curl pwsh; do command -v "$tool" >/dev/null || exit 1; done \
    && pwsh -NoLogo -NoProfile -Command '$ErrorActionPreference = "Stop"; $PSVersionTable.PSVersion.ToString()'
