# CI/CD Pipeline

This repository includes a full-stack GitHub Actions pipeline at `.github/workflows/ci.yml` to deploy Sanzu online from `main`: infrastructure, database, backend API, and frontend.

## What Runs

1. Lint and format validation (`dotnet format --verify-no-changes`)
2. API build and tests (`dotnet build`, unit, integration, optional burn-in)
3. Frontend build and tests (`npm ci`, `npm run test`, `npm run build`)
4. Package API and frontend artifacts
5. Provision/Update Azure infrastructure via Bicep (`api/infra/azure/main.bicep`)
6. Apply EF Core migrations to Azure SQL
7. Deploy API web app and frontend web app
8. Smoke tests (`/health` for API and `/` for frontend)

## Triggers

- `push` to `main` or `develop`
- `pull_request` to `main` or `develop`
- `workflow_dispatch` (manual run with optional deploy toggle)
- Weekly schedule (`0 2 * * 0`) for burn-in stability checks

## Azure Deployment Path

Deployment path:

- `provision-infra`: resource group, app service plan, API web app, frontend web app, Azure SQL server/database
- `migrate-database`: `dotnet ef database update`
- `deploy-api`: deploy published .NET API package
- `deploy-frontend`: deploy Next.js frontend package
- `smoke-tests`: validate endpoints are reachable

Auth model:

- GitHub OIDC via `azure/login@v2` (no publish profile required)

If required Azure settings are missing or invalid, the deploy job fails with a clear configuration error so `main` cannot silently pass without a publishable app.

## Local Mirror Commands

From repository root:

```bash
chmod +x api/scripts/*.sh
./api/scripts/ci-local.sh
```

Optional:

```bash
./api/scripts/burn-in.sh
./api/scripts/test-changed.sh
```

## Notes

- CI/CD is intentionally introduced early to get live signal on Azure infrastructure quickly.
- Current alignment for implementation sequencing is Epic 1, Story 1.2 (onboarding completion baseline), before Story 1.3 billing activation work.
