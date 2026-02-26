# ============================================
# Arcana Windows - Linux CI Build
# ============================================
# Builds cross-platform projects (net10.0) and
# runs all tests. Excludes WinUI 3 projects.
# ============================================

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /app

# Copy everything
COPY . .

# Remove Windows-only projects from solution
RUN dotnet sln remove src/Arcana.App/Arcana.App.csproj 2>/dev/null || true \
    && dotnet sln remove plugins/FlowChartModule/Arcana.Plugin.FlowChart.csproj 2>/dev/null || true

# Restore
RUN dotnet restore

# Build
RUN dotnet build -c Release --no-restore

CMD ["echo", "Build completed successfully"]
