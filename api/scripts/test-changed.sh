#!/bin/bash
set -euo pipefail

TEST_PROJECT_PATH="tests/Sanzu.Tests/Sanzu.Tests.csproj"
BASE_REF="${1:-HEAD~1}"

if ! git rev-parse --verify "$BASE_REF" >/dev/null 2>&1; then
  echo "Base ref '$BASE_REF' not found. Running full test suite."
  dotnet test "$TEST_PROJECT_PATH" --configuration Release
  exit 0
fi

CHANGED_FILES="$(git diff --name-only "${BASE_REF}..HEAD")"
echo "Changed files:"
echo "$CHANGED_FILES"

if ! echo "$CHANGED_FILES" | grep -Eq '^(src|tests)/'; then
  echo "No backend changes detected under src or tests. Skipping tests."
  exit 0
fi

if echo "$CHANGED_FILES" | grep -Eq '^tests/.*/Unit/'; then
  echo "Running unit tests impacted by test changes."
  dotnet test "$TEST_PROJECT_PATH" \
    --configuration Release \
    --filter "FullyQualifiedName~Unit"
fi

if echo "$CHANGED_FILES" | grep -Eq '^tests/.*/Integration/'; then
  echo "Running integration tests impacted by test changes."
  dotnet test "$TEST_PROJECT_PATH" \
    --configuration Release \
    --filter "FullyQualifiedName~Integration"
fi

if echo "$CHANGED_FILES" | grep -Eq '^src/'; then
  echo "Source changes detected. Running full backend test suite."
  dotnet test "$TEST_PROJECT_PATH" --configuration Release
fi
