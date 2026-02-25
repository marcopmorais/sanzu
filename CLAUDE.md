# CLAUDE.md — Sanzu Repository

## Project Overview

Sanzu is a multi-tenant SaaS platform for agency case management, billing, and compliance. It consists of:

- **Backend API**: ASP.NET Core 9.0 REST API (`Sanzu.API`)
- **Frontend**: Next.js 14 application with React 18 and TypeScript (`Sanzu.Web`)
- **Infrastructure**: Azure deployment via Bicep templates

The repository uses an allowlist `.gitignore` — only `api/`, `.github/`, `.gitignore`, `README.md`, and `CLAUDE.md` are tracked. All other top-level directories are intentionally ignored.

## Repository Structure

```
sanzu/
├── .github/workflows/ci.yml    # CI/CD pipeline (GitHub Actions)
├── api/
│   ├── Sanzu.sln                # .NET solution (5 projects)
│   ├── src/
│   │   ├── Sanzu.Core/          # Domain models, services, validators, interfaces
│   │   ├── Sanzu.Infrastructure/ # EF Core DbContext, repositories, migrations
│   │   ├── Sanzu.API/           # Controllers, middleware, auth, DI config
│   │   └── Sanzu.Web/           # Next.js frontend (components, pages, tests)
│   ├── tests/
│   │   └── Sanzu.Tests/         # xUnit tests (Unit/, Integration/, Admin/)
│   ├── scripts/                 # ci-local.sh, burn-in.sh, test-changed.sh
│   ├── infra/azure/main.bicep   # Azure infrastructure template
│   └── docs/                    # CI docs and secrets checklist
├── README.md
├── CLAUDE.md
└── LICENSE                      # MPL-2.0
```

## Build & Test Commands

All commands run from the repository root (`/home/user/sanzu`).

### Backend (.NET 9.0)

```bash
# Build
dotnet build ./api/Sanzu.sln

# Run all tests
dotnet test ./api/Sanzu.sln --no-restore

# Run unit tests only
dotnet test ./api/Sanzu.sln --filter "FullyQualifiedName~Unit"

# Run integration tests only
dotnet test ./api/Sanzu.sln --filter "FullyQualifiedName~Integration"

# Lint / format check (must pass in CI)
dotnet format ./api/Sanzu.sln --verify-no-changes --severity warn

# Auto-fix formatting
dotnet format ./api/Sanzu.sln --severity warn
```

### Frontend (Node.js 20.x)

```bash
cd api/src/Sanzu.Web

# Install dependencies
npm ci

# Unit tests (Vitest)
npm run test

# E2E tests (Playwright)
npm run test:e2e

# Build
npm run build

# Dev server
npm run dev
```

### Local CI Mirror

```bash
cd api && chmod +x scripts/*.sh && ./scripts/ci-local.sh
```

## Architecture

### Clean Architecture Layers

1. **Sanzu.Core** — Domain layer. Contains entities, enums, interfaces, DTOs (request/response models), validators (FluentValidation), and service implementations. Zero infrastructure dependencies.
2. **Sanzu.Infrastructure** — Data access layer. EF Core `SanzuDbContext` with 23+ DbSets, repository implementations, migrations, and DI registration via `AddInfrastructureServices()`.
3. **Sanzu.API** — Presentation layer. ASP.NET Core controllers, authentication handler, tenant middleware, audit filter, and service composition via `AddSanzuServices()`.
4. **Sanzu.Web** — Frontend. Next.js App Router with atomic design (atoms/molecules/organisms), TypeScript strict mode, path aliases (`@/components/*`, `@/lib/*`).

### Key Patterns

- **Multi-tenancy**: `TenantContextMiddleware` resolves tenant ID from auth claims (`tenant_id`, `org_id`, `organization_id`). `SanzuDbContext.CurrentOrganizationId` applies global query filters on tenant-scoped entities.
- **Authentication**: Custom `HeaderAuthenticationHandler` with header-based auth.
- **Authorization policies**: `TenantAdmin`, `SanzuAdmin`, `AdminFull`, `AdminOps`, `AdminFinance`, `AdminSupport`, `AdminViewer` — each maps to `PlatformRole` enum values.
- **Repository + Unit of Work**: All data access goes through repository interfaces (`ICaseRepository`, etc.) with `IUnitOfWork` for transaction boundaries.
- **Validation**: FluentValidation validators in `Sanzu.Core/Validators/`, registered individually in DI. Validators are called explicitly in service methods, not via pipeline.
- **API envelope**: Responses wrapped in `ApiEnvelope<T>.Success(data, meta)` with consistent structure.
- **Audit logging**: `AdminAuditActionFilter` on all controller actions; `IAuditRepository` for explicit audit events.

### Entry Point

`api/src/Sanzu.API/Program.cs` — Minimal API host setup. Registers services via `AddSanzuServices()`, configures authentication, tenant middleware, authorization, health endpoint (`/health`), and controller mapping.

## Coding Conventions

### C# / .NET

- **Target framework**: `net9.0` with `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`
- **Classes**: Use `sealed` on entity classes, service implementations, test classes, and non-base controllers
- **Namespaces**: File-scoped (`namespace Sanzu.Core.Services;`) — one type per file
- **DI**: Constructor injection everywhere; all services registered as `Scoped` in `ServiceRegistration.cs`
- **Naming**: PascalCase for public members, `_camelCase` for private fields, `Async` suffix on async methods
- **Controllers**: `[ApiController]` attribute, route pattern `api/v1/...`, return `IActionResult`, use `ProducesResponseType` attributes
- **Error handling**: Custom exceptions (`TenantAccessDeniedException`, `CaseStateException`, etc.) caught in controllers and mapped to appropriate HTTP status codes
- **Models**: Separate request/response DTOs in `Sanzu.Core/Models/Requests/` and `Sanzu.Core/Models/Responses/`

### TypeScript / React

- **TypeScript strict mode** enabled
- **Path aliases**: `@/components/*`, `@/lib/*` (configured in `tsconfig.json`)
- **Component hierarchy**: `components/atoms/`, `components/molecules/`, `components/organisms/`
- **App Router**: Pages in `app/` directory (`page.tsx` convention)
- **Testing**: Vitest + Testing Library for unit tests (`tests/unit/`), Playwright for E2E (`tests/e2e/`)

### Commit Messages

Follow the pattern: `feat: implement Story X.Y — Short Description`

Examples from history:
- `feat: implement Story 13.2 — Health Score Background Service and Classification`
- `feat: implement Story 12.1 — Internal Admin RBAC & Permissions Endpoint`
- `infra: output sql database name`
- `ci: provision and deploy full sanzu stack on azure`

Prefixes: `feat:`, `fix:`, `ci:`, `infra:`, `docs:`, `test:`, `refactor:`

## Testing Conventions

### Unit Tests (`tests/Sanzu.Tests/Unit/`)

- xUnit with `[Fact]` and `[Theory]` attributes
- FluentAssertions for assertions (`.Should().BeTrue()`, `.Should().Contain()`)
- Moq for mocking dependencies
- Test class naming: `{Subject}Tests` (e.g., `CaseValidatorsTests`)
- Test method naming: `{Method}_Should{Expected}_When{Condition}`
- Classes are `sealed`
- Organized by concern: `Unit/Validators/`, `Unit/Services/`, `Unit/Middleware/`

### Integration Tests (`tests/Sanzu.Tests/Integration/`)

- `CustomWebApplicationFactory` replaces SQL Server with EF Core InMemory database
- Each test gets a unique database instance (`sanzu-integration-{Guid}`)
- Tests exercise full HTTP pipeline via `WebApplicationFactory<Program>`
- Organized under `Integration/Controllers/`

### Frontend Tests

- Unit tests in `api/src/Sanzu.Web/tests/unit/` using Vitest + Testing Library
- E2E tests using Playwright (config: `playwright.config.ts`)

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) runs on:
- Push to `main` or `develop`
- Pull requests to `main` or `develop`
- Weekly schedule (Sunday 2 AM UTC)
- Manual workflow dispatch

### Pipeline Stages

1. **Lint** — `dotnet format --verify-no-changes --severity warn`
2. **Build & Test API** — Restore, build, unit tests, integration tests
3. **Build & Test Frontend** — `npm ci`, `npm run test`, `npm run build`
4. **Burn-in** (PR/schedule/manual only) — Run integration tests 10x to detect flaky tests
5. **Package** — `dotnet publish` for API; rsync for frontend
6. **Provision Infrastructure** — Azure Bicep deployment (main/manual only)
7. **Migrate Database** — EF Core migrations
8. **Deploy** — Azure Web Apps for both API and frontend
9. **Smoke Tests** — Health check on `/health` endpoint

### What Must Pass Before Merging

- `dotnet format` lint check (no warnings)
- All unit tests pass
- All integration tests pass
- Frontend unit tests pass
- Frontend builds successfully

## Database

- **ORM**: Entity Framework Core 9.0 with SQL Server provider
- **Dev connection**: LocalDB (`(localdb)\\mssqllocaldb`)
- **Migrations**: Located in `api/src/Sanzu.Infrastructure/Migrations/`
- **Adding a migration**: `dotnet ef migrations add <Name> --project api/src/Sanzu.Infrastructure --startup-project api/src/Sanzu.API`
- **Applying migrations**: `dotnet ef database update --project api/src/Sanzu.Infrastructure --startup-project api/src/Sanzu.API`

## Key Files Quick Reference

| File | Purpose |
|------|---------|
| `api/Sanzu.sln` | Solution file (5 projects) |
| `api/src/Sanzu.API/Program.cs` | API entry point, middleware pipeline |
| `api/src/Sanzu.API/Configuration/ServiceRegistration.cs` | All DI registrations |
| `api/src/Sanzu.Infrastructure/Data/SanzuDbContext.cs` | EF Core context, 23+ DbSets, query filters |
| `api/src/Sanzu.Core/Interfaces/` | 51 interface contracts |
| `api/src/Sanzu.Core/Validators/` | 37 FluentValidation validators |
| `api/src/Sanzu.Core/Services/` | 28 service implementations |
| `api/src/Sanzu.Core/Entities/` | 25 domain entities |
| `api/tests/Sanzu.Tests/Integration/CustomWebApplicationFactory.cs` | Test host with InMemory DB |
| `api/src/Sanzu.Web/package.json` | Frontend dependencies and scripts |
| `.github/workflows/ci.yml` | Full CI/CD pipeline |
| `api/infra/azure/main.bicep` | Azure infrastructure template |

## Important Notes

- The `.gitignore` uses an allowlist pattern — only explicitly listed paths are tracked. When adding new top-level files, you must add a negation rule (`!filename`) to `.gitignore`.
- `public partial class Program;` in `Program.cs` exists to support `WebApplicationFactory<Program>` in integration tests — do not remove it.
- All services are registered as `Scoped` (not Singleton or Transient) because of the multi-tenant `DbContext` dependency.
- The `HealthScoreBackgroundService` is the only `HostedService` — registered via `AddHostedService<>()`.
