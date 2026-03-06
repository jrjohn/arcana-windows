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
  /d:sonar.coverage.exclusions="**/tests/**,**/Tests/**" \
  /d:sonar.cs.opencover.reportsPaths="/app/coverage/**/coverage.opencover.xml"

echo "=== Build ==="
dotnet build -c Release --no-restore

echo "=== Unit Tests + Coverage ==="
dotnet test -c Release --no-build \
  --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover \
  --results-directory /app/coverage \
  || true  # 測試失敗不中斷 sonar end

echo "=== SonarQube End ==="
dotnet sonarscanner end \
  /d:sonar.token="${SONAR_TOKEN}"
