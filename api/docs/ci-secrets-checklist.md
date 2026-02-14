# CI/CD Secrets Checklist

Configure these before expecting full-stack Azure deployment from `.github/workflows/ci.yml`.

## Repository Variables

- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION`
- `AZURE_APP_SERVICE_PLAN_NAME`
- `AZURE_API_WEBAPP_NAME`
- `AZURE_FRONTEND_WEBAPP_NAME`
- `AZURE_SQL_SERVER_NAME`
- `AZURE_SQL_DATABASE_NAME`

Where: GitHub Repository Settings -> Secrets and variables -> Actions -> Variables

## Repository Secrets

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_SQL_ADMIN_LOGIN`
- `AZURE_SQL_ADMIN_PASSWORD`

Where: GitHub Repository Settings -> Secrets and variables -> Actions -> Secrets

## Azure Setup (OIDC Recommended)

1. Create an Azure AD app registration for GitHub Actions deployment.
2. Create a federated credential bound to your GitHub repo and branch/environment.
3. Grant the service principal access at least at Resource Group scope (Contributor or least-privileged equivalent) so it can deploy infrastructure + web apps.
4. Store IDs in GitHub secrets and variable listed above.

## Validation Steps

1. Push to `main` or run workflow manually from Actions tab.
2. Confirm API and frontend build/test jobs pass.
3. Confirm `provision-infra` and `migrate-database` pass.
4. Confirm `deploy-api` and `deploy-frontend` pass.
5. Confirm smoke tests pass for API and frontend URLs.
6. Open frontend URL and verify Sanzu is reachable online.

## Security Guardrails

- Do not commit secrets to source control.
- Use OIDC-based login (`azure/login`) instead of publish profiles where possible.
- Rotate credentials regularly and keep scope minimal.
