# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

All backend commands run from `api/` directory unless noted otherwise.

```bash
# Build
dotnet build Sanzu.sln --configuration Release --no-restore

# Run all tests (from repo root)
dotnet test ./api/Sanzu.sln --no-restore

# Run all tests (from api/)
dotnet test ./Sanzu.sln --no-restore

# Run a single test class
dotnet test tests/Sanzu.Tests/Sanzu.Tests.csproj --filter "FullyQualifiedName~AdminRbacTests"

# Run a single test method
dotnet test tests/Sanzu.Tests/Sanzu.Tests.csproj --filter "FullyQualifiedName~AdminRbacTests.PlatformRole_Should_HaveAllSixValues"

# Format check (lint)
dotnet format Sanzu.sln --verify-no-changes --severity warn

# EF migrations — add a new migration
dotnet ef migrations add Story_X_Y_DescriptionHere --project src/Sanzu.Infrastructure --startup-project src/Sanzu.API

# EF migrations — apply to dev database
dotnet ef database update --project src/Sanzu.Infrastructure/Sanzu.Infrastructure.csproj --startup-project src/Sanzu.API/Sanzu.API.csproj --context SanzuDbContext

# Local CI mirror (restore → build → unit → integration → burn-in)
./scripts/ci-local.sh
```

Frontend commands run from `api/src/Sanzu.Web/`:

```bash
npm ci && npm run dev      # Install + dev server (port 3000)
npm run build              # Production build
npm run lint               # ESLint
npm run test               # Vitest unit tests
npm run test:e2e           # Playwright e2e tests
```

## Architecture

### Solution structure (api/)

- **Sanzu.Core** — Domain layer: entities, interfaces, services, validators, enums, DTOs. No EF dependency.
- **Sanzu.Infrastructure** — Data layer: EF Core DbContext (`SanzuDbContext`), repositories, migrations. SQL Server (LocalDB for dev, InMemory for tests).
- **Sanzu.API** — ASP.NET Core 9 Web API host. Controllers, middleware, auth handlers, DI setup.
- **Sanzu.Web** — Next.js 14 frontend (App Router, React 18, TypeScript, Tailwind). Atomic design: atoms/molecules/organisms.
- **Sanzu.Tests** — xUnit + Moq + FluentAssertions. Unit and integration tests in one project.

### Authentication & authorization

Custom header-based auth scheme (`HeaderTenantAuth`) — no JWT in development. Three headers:
- `X-User-Id` (GUID) — **required** for auth to succeed
- `X-Tenant-Id` (GUID) — **required** for auth to succeed
- `X-User-Role` (string) — needed for policy authorization

Missing either `X-User-Id` or `X-Tenant-Id` returns `AuthenticateResult.NoResult()` → 401.

Authorization policies defined in `Configuration/ServiceRegistration.cs`: `TenantAdmin`, `SanzuAdmin`, `AdminFull`, `AdminOps`, `AdminFinance`, `AdminSupport`, `AdminViewer`. PlatformRole enum has 6 values: `AgencyAdmin`, `SanzuAdmin`, `SanzuOps`, `SanzuFinance`, `SanzuSupport`, `SanzuViewer`.

### API route patterns

- **Tenant-scoped:** `api/v1/tenants/{tenantId:guid}/...` (e.g., `api/v1/tenants/{tenantId:guid}/cases`). The `:guid` constraint is required — omitting it causes 404s that look like auth problems.
- **Admin internal:** `api/v1/admin/...` (e.g., `api/v1/admin/platform`, `api/v1/admin/me/permissions`)
- **Public:** `api/v1/tenants/signup`, `api/v1/public/...`

### Response envelope

All success responses wrap in `ApiEnvelope<T>.Success(data, BuildMeta())` returning `{ data, errors, meta }` where meta contains `{ "timestamp": DateTime.UtcNow }`. Error responses use standard `ProblemDetails`/`ValidationProblemDetails` — **not** the envelope. The `ApiEnvelope<T>.Failure()` factory exists but is unused in controllers.

### Multi-tenancy

`SanzuDbContext.CurrentOrganizationId` is set by `TenantContextMiddleware` from auth claims. All tenant-scoped entities use EF global query filters. When `null`, all data is visible (admin/public contexts).

Repository methods named `...ForPlatformAsync()` call `IgnoreQueryFilters()` to bypass tenant scoping. Standard repo methods silently filter by the current tenant. Always use `ForPlatform` variants in admin/platform services.

### Middleware pipeline (Program.cs)

`UseAuthentication()` → `TenantContextMiddleware` → `UseAuthorization()` → Controllers. `AdminAuditActionFilter` is a global action filter that auto-logs audit events for all `api/v1/admin/*` routes.

### Repository + Unit of Work

`AuditRepository.CreateAsync()` only adds to the EF change tracker — **must** wrap in `IUnitOfWork.ExecuteInTransactionAsync()` to persist. `EfUnitOfWork` handles `SaveChangesAsync()` + transaction commit; InMemory provider (tests) skips the transaction.

### Controller patterns

No base controller class — all controllers are `sealed class` extending `ControllerBase` directly. The following helper methods are **copy-pasted into every controller** (follow the same pattern when adding new ones):

- `TryGetActorUserId(out Guid)` — extracts user ID from `ClaimTypes.NameIdentifier` / `"sub"` / `"user_id"` claims
- `BuildValidationProblem(ValidationException, string)` — converts FluentValidation errors to `ValidationProblemDetails`
- `BuildMeta()` — returns `{ "timestamp": DateTime.UtcNow }`

### DI registration

Infrastructure services: `AddInfrastructureServices()` in Sanzu.Infrastructure. All application services, validators, auth, policies: `AddSanzuServices()` in `Configuration/ServiceRegistration.cs`. FluentValidation validators are manually registered.

### Domain model notes

- All enums stored as **strings** in the database (`.HasConversion<string>()`)
- `UserRole.TenantId` is **nullable** — `null` means a platform-wide Sanzu internal role; non-null means an agency-scoped role
- `CaseNumber` format: `CASE-XXXXX` (zero-padded, sequential per tenant, unique constraint on `(TenantId, CaseNumber)`)
- `RoleType` has a DB check constraint `CK_UserRoles_RoleType` — adding a new `PlatformRole` enum value requires a migration to drop and recreate this constraint

## Test patterns

**Integration tests:** Use `CustomWebApplicationFactory` (replaces SQL Server with InMemory). Seed data via `_factory.Services.CreateScope()` into `SanzuDbContext`. Set auth headers manually. `Organization` entity has **no `Slug` property** — use the `Location` field in test seeds.

**Unit tests:** Mock all repositories with `Mock<IRepository>()`, assert via FluentAssertions.

**Frontend tests:** Vitest for unit tests (`tests/unit/`), Playwright for e2e (`tests/e2e/`). Test files named per story: `story-{epic}-{story}-{slug}.test.tsx`.

**Frontend API client:** Files in `lib/api-client/generated/` are **manually written** despite the "auto-generated" comment header. When adding a new API endpoint, manually create or update the corresponding file there. No OpenAPI codegen tooling exists.

**Test isolation:** Each `CustomWebApplicationFactory` instance gets a unique in-memory database (GUID name). Tests in the same class share one factory via `IClassFixture`, so they **share database state** — use unique emails/names per test to avoid collisions.

## Git Flow & Versioning

### Branch structure

| Branch | Purpose | Deploys to |
|--------|---------|------------|
| `main` | Production-ready code | Azure (auto-deploy on push) |
| `develop` | Integration branch for next release | CI runs, no deploy |
| `feature/<epic>-<short-name>` | Epic/feature work | — |
| `hotfix/<description>` | Urgent production fixes | — |
| `release/<version>` | Release stabilization | — |

### Workflow

1. **Feature development:** Branch from `develop` → `feature/<epic>-<short-name>` (e.g., `feature/epic-14-billing-overhaul`). Commit per story within the feature branch using `feat: implement Story X.Y — <title>`. Do **not** push until all stories in the epic are complete.
2. **Integration:** When all stories in an epic are done, push the feature branch and open a PR into `develop`. CI runs lint → build → unit tests → integration tests → burn-in (10 iterations). Squash-merge or merge commit — keep story-level commits visible.
3. **Release:** Branch from `develop` → `release/<version>` (e.g., `release/1.2.0`). Only bug fixes allowed on release branches. When stable, merge into `main` **and** back into `develop`. Tag `main` with `v<version>`.
4. **Hotfix:** Branch from `main` → `hotfix/<description>`. Fix, test, merge into `main` **and** `develop`. Tag `main` with the patched version.
5. **Deployment:** Merges to `main` trigger the full CI/CD pipeline: lint → build → test → package → provision Azure infra → migrate DB → deploy API + frontend → smoke tests.

### Version tagging

Use [SemVer](https://semver.org/): `v<major>.<minor>.<patch>`
- **major** — breaking API changes or major platform shifts
- **minor** — new epics/features (maps to completed epics)
- **patch** — hotfixes and bug fixes

```bash
# Tag a release after merging to main
git tag -a v1.2.0 -m "Release 1.2.0 — Epic 14 Billing Overhaul"
git push origin v1.2.0
```

### CI triggers (`.github/workflows/ci.yml`)

- **Push to `main` or `develop`** — full lint + build + test pipeline
- **PRs into `main` or `develop`** — full pipeline + burn-in flaky test detection
- **Push to `main` only** — triggers packaging + Azure deployment
- **Weekly schedule** (Sunday 02:00 UTC) — full pipeline + burn-in
- **Manual dispatch** — optional `deploy_to_azure` toggle

### Branch naming examples

```
feature/epic-14-billing-overhaul
feature/epic-15-reporting-dashboard
hotfix/fix-tenant-filter-null-ref
release/1.3.0
```

## Conventions

- Admin controllers go in `Controllers/Admin/`, route prefix `api/v1/admin/`
- Admin audit event naming: `Admin.{Entity}.{Action}` (PascalCase, dot-separated) — entity is controller name with "Admin" prefix stripped
- Commit message style: `feat: implement Story X.Y — <title>`
- `.next/` build artifacts must never be staged in git
- `_bmad-output/` is gitignored — planning artifacts are not committed
- Top-level PDLC folders (`01-*` through `10-*`) are read-only reference material
- Health check endpoint: `GET /health`
- OpenAPI available in development at `/openapi/v1.json`
