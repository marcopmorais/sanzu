# CI/CD Pipeline

This repository now includes a GitHub Actions pipeline at `.github/workflows/ci.yml` to run quality checks early and deploy the Sanzu frontend web app to Azure App Service as soon as `main` is updated.

## What Runs

1. Lint and format validation (`dotnet format --verify-no-changes`)
2. Build (`dotnet build` on `Sanzu.sln`)
3. Unit tests (`FullyQualifiedName~Unit`)
4. Integration tests (`FullyQualifiedName~Integration`)
5. Burn-in loop (10 integration test iterations on PR/schedule/manual runs)
6. Package and deploy frontend static site (`main` and manual dispatch) to Azure App Service

## Triggers

- `push` to `main` or `develop`
- `pull_request` to `main` or `develop`
- `workflow_dispatch` (manual run with optional deploy toggle)
- Weekly schedule (`0 2 * * 0`) for burn-in stability checks

## Azure Deployment Path

Deployment job: `deploy-azure`

- Environment: `azure-dev`
- Deployment target: Azure App Service (`AZURE_WEBAPP_NAME`)
- Auth model: GitHub OIDC with `azure/login@v2`
- Deployment action: `azure/webapps-deploy@v3`
- Deployed package source: `web/sanzu-brand-frontend`
- Post-deploy verification: smoke test against `<deployed-webapp-url>/index.html`

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
