# CI/CD Secrets Checklist

Configure these before expecting automatic Azure deployment from `.github/workflows/ci.yml`.

## Repository Variables

- `AZURE_WEBAPP_NAME`
  - Example: `sanzu-api-dev`
  - Where: GitHub Repository Settings -> Secrets and variables -> Actions -> Variables

## Repository Secrets

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Where: GitHub Repository Settings -> Secrets and variables -> Actions -> Secrets

## Azure Setup (OIDC Recommended)

1. Create an Azure AD app registration for GitHub Actions deployment.
2. Create a federated credential bound to your GitHub repo and branch/environment.
3. Grant the service principal access to the App Service resource (Contributor or least-privileged equivalent for deployment).
4. Store IDs in GitHub secrets and variable listed above.

## Validation Steps

1. Push to `main` or run workflow manually from Actions tab.
2. Confirm `build-and-test` and `package` jobs pass.
3. Confirm `deploy-azure` runs and does not fail configuration validation.
4. Confirm the post-deploy smoke test passes for `<deployed-webapp-url>/index.html`.
5. Open the App Service URL shown in workflow logs and verify the Sanzu frontend index page is reachable.

## Security Guardrails

- Do not commit secrets to source control.
- Use OIDC-based login (`azure/login`) instead of publish profiles where possible.
- Rotate credentials regularly and keep scope minimal.
