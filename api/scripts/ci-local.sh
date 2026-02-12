#!/bin/bash
set -euo pipefail

SOLUTION_PATH="Sanzu.sln"
TEST_PROJECT_PATH="tests/Sanzu.Tests/Sanzu.Tests.csproj"
CONFIGURATION="${CONFIGURATION:-Release}"
ITERATIONS="${ITERATIONS:-3}"

echo "Running local CI mirror for Sanzu API"
dotnet restore "$SOLUTION_PATH"
dotnet build "$SOLUTION_PATH" --configuration "$CONFIGURATION" --no-restore

echo "Running unit tests"
dotnet test "$TEST_PROJECT_PATH" \
  --configuration "$CONFIGURATION" \
  --no-build \
  --filter "FullyQualifiedName~Unit"

echo "Running integration tests"
dotnet test "$TEST_PROJECT_PATH" \
  --configuration "$CONFIGURATION" \
  --no-build \
  --filter "FullyQualifiedName~Integration"

echo "Running burn-in loop (${ITERATIONS} iterations)"
for i in $(seq 1 "$ITERATIONS"); do
  echo "Burn-in iteration ${i}/${ITERATIONS}"
  dotnet test "$TEST_PROJECT_PATH" \
    --configuration "$CONFIGURATION" \
    --no-build \
    --filter "FullyQualifiedName~Integration"
done

echo "Local CI mirror passed"
