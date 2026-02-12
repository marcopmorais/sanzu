# Sanzu Repository

This repository tracks the Sanzu API codebase and CI/CD automation.

## Tracked Content

- `.github/` (GitHub Actions workflows)
- `api/` (.NET solution, source, tests, scripts, and API docs)
- `.gitignore`
- `README.md`

All other top-level project folders are intentionally ignored in this repository.

## CI/CD

- Workflow: `.github/workflows/ci.yml`
- Pipeline covers lint/format checks, build, tests, burn-in stability checks, packaging, and Azure App Service deployment.
- Azure setup details: `api/docs/ci-secrets-checklist.md`

## Local Development

From repository root:

```bash
dotnet test ./api/Sanzu.sln --no-restore
```

Helper scripts:

- `api/scripts/ci-local.sh`
- `api/scripts/burn-in.sh`
- `api/scripts/test-changed.sh`
