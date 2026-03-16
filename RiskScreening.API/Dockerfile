# ============================================================
# Stage 1 — Base runtime image
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Configurable port and timezone via build args (overridable at runtime via env vars)
ARG PORT=8080
ARG TZ=America/Lima

ENV ASPNETCORE_URLS=http://+:${PORT}
ENV TZ=${TZ}
ENV App__TimeZone=${TZ}

EXPOSE ${PORT}

# Playwright/Chromium dependencies — required for ICIJ scraping
# These are the minimum packages Chromium needs on Debian-based images
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 libnspr4 libatk1.0-0t64 libatk-bridge2.0-0t64 \
    libcups2t64 libdrm2 libxkbcommon0 libxcomposite1 \
    libxdamage1 libxrandr2 libgbm1 libpango-1.0-0 \
    libcairo2 libasound2t64 libxshmfence1 libx11-xcb1 \
    libxfixes3 libxcursor1 libxi6 libxtst6 libxss1 \
    libgtk-3-0t64 libdbus-glib-1-2 \
    fonts-liberation xdg-utils wget ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# ============================================================
# Stage 2 — Build + publish
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["RiskScreening.API/RiskScreening.API.csproj", "RiskScreening.API/"]
RUN dotnet restore "RiskScreening.API/RiskScreening.API.csproj"
COPY . .
WORKDIR "/src/RiskScreening.API"
RUN dotnet build "./RiskScreening.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RiskScreening.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Install Playwright Chromium browser for ICIJ scraping
ENV PLAYWRIGHT_BROWSERS_PATH=/app/publish/.playwright
RUN dotnet tool install --global Microsoft.Playwright.CLI 2>/dev/null || true \
    && export PATH="$PATH:/root/.dotnet/tools" \
    && playwright install --with-deps chromium
RUN dotnet dev-certs https --clean 2>/dev/null || true

# ============================================================
# Stage 3 — Final runtime image
# ============================================================
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Playwright expects browsers here
ENV PLAYWRIGHT_BROWSERS_PATH=/app/.playwright

# Grant execute permissions on Playwright binaries so the non-root user can run Chromium
RUN chmod -R +x /app/.playwright 2>/dev/null || true

# Run as non-root user (set by base image)
USER $APP_UID

ENTRYPOINT ["dotnet", "RiskScreening.API.dll"]
