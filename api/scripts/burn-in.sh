#!/bin/bash
set -euo pipefail

TEST_PROJECT_PATH="tests/Sanzu.Tests/Sanzu.Tests.csproj"
CONFIGURATION="${CONFIGURATION:-Release}"
ITERATIONS="${ITERATIONS:-10}"

echo "Starting burn-in run (${ITERATIONS} iterations)"
for i in $(seq 1 "$ITERATIONS"); do
  echo "Burn-in iteration ${i}/${ITERATIONS}"
  dotnet test "$TEST_PROJECT_PATH" \
    --configuration "$CONFIGURATION" \
    --filter "FullyQualifiedName~Integration"
done

echo "Burn-in run completed without failures"
