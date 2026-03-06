#!/bin/sh
set -e

# Set JAVA_HOME
export JAVA_HOME=$(dirname $(dirname $(readlink -f $(which java))))
export PATH="$PATH:$JAVA_HOME/bin:/root/.dotnet/tools"

echo "JAVA_HOME=$JAVA_HOME"
java -version

echo "=== SonarQube Begin ==="
dotnet sonarscanner begin \
  /k:"${SONAR_PROJECT_KEY:-dotnet-app}" \
  /n:"${SONAR_PROJECT_NAME:-Dotnet App}" \
  /d:sonar.host.url="${SONAR_HOST_URL:-http://sonarqube:9000/sonarqube}" \
  /d:sonar.token="${SONAR_TOKEN}" \
  /d:sonar.exclusions="**/bin/**,**/obj/**,**/TestResults/**" \
  /d:sonar.coverage.exclusions="**/tests/**,**/Tests/**,**/PluginManagerService.cs,**/PluginManager.cs,**/PluginHealthMonitor.cs,**/DependencyInjection/**,**/Platform/NetworkMonitor.cs,**/IAuthenticationPlugin.cs,**/LazyContributionService.cs" \
  /d:sonar.cs.opencover.reportsPaths="/app/**/coverage.opencover.xml"

echo "=== Build ==="
dotnet build -c Release --no-restore

echo "=== Unit Tests + Coverage ==="
# --results-directory must come BEFORE -- to be handled by dotnet test, not the test host
dotnet test -c Release --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory /app/coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover \
  || true  # 測試失敗不中斷 sonar end

echo "=== Coverage files found ==="
find /app -name "coverage.opencover.xml" 2>/dev/null || echo "No coverage files found"

echo "=== SonarQube End ==="
dotnet sonarscanner end \
  /d:sonar.token="${SONAR_TOKEN}"
