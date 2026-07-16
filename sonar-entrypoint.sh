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
# Test failure doesn't abort immediately -- sonar end still runs so the (partial)
# report reaches SonarQube -- but TEST_RC is checked below and fails the build.
TEST_RC=0
dotnet test -c Release --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory /app/coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover \
  || TEST_RC=$?

echo "=== Coverage files found ==="
find /app -name "coverage.opencover.xml" 2>/dev/null || echo "No coverage files found"

echo "=== SonarQube End ==="
dotnet sonarscanner end \
  /d:sonar.token="${SONAR_TOKEN}"

echo "=== Quality Gate check ==="
RT=$(find /app -name report-task.txt 2>/dev/null | head -1)
if [ -z "$RT" ]; then
  echo "report-task.txt not found -- scanner did not run"
  exit 1
fi
CE_TASK_ID=$(grep '^ceTaskId=' "$RT" | cut -d= -f2-)
echo "CE task id: $CE_TASK_ID"
ANALYSIS_ID=""
i=1
while [ "$i" -le 60 ]; do
  RESP=$(curl -s -u "${SONAR_TOKEN}:" "${SONAR_HOST_URL:-http://sonarqube:9000/sonarqube}/api/ce/task?id=${CE_TASK_ID}")
  ST=$(echo "$RESP" | grep -o '"status":"[A-Z_]*"' | head -1 | cut -d'"' -f4)
  echo "  CE status: ${ST:-?} (try $i)"
  if [ "$ST" = "SUCCESS" ]; then
    ANALYSIS_ID=$(echo "$RESP" | grep -o '"analysisId":"[^"]*"' | head -1 | cut -d'"' -f4)
    break
  elif [ "$ST" = "FAILED" ] || [ "$ST" = "CANCELED" ]; then
    echo "CE task ended $ST"
    exit 1
  fi
  sleep 5
  i=$((i + 1))
done
if [ -z "$ANALYSIS_ID" ]; then
  echo "CE task did not finish in time"
  exit 1
fi
GATE=$(curl -s -u "${SONAR_TOKEN}:" "${SONAR_HOST_URL:-http://sonarqube:9000/sonarqube}/api/qualitygates/project_status?analysisId=${ANALYSIS_ID}")
GST=$(echo "$GATE" | grep -o '"status":"[A-Z]*"' | head -1 | cut -d'"' -f4)
echo "Quality gate: ${GST:-UNKNOWN}"
if [ "$GST" != "OK" ]; then
  echo "$GATE"
  exit 1
fi

if [ "$TEST_RC" -ne 0 ]; then
  echo "Unit tests FAILED (rc=$TEST_RC)"
  exit "$TEST_RC"
fi
echo "All checks passed."
