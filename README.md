# WebAPIDevSecOpsScallingSDD

Web API built with **.NET 10** (ASP.NET Core + Entity Framework Core) with a
DevSecOps and scalability focus: versioned catalog endpoints, password login
with lockout and two-factor step-up, Redis cache-aside with in-memory fallback,
and mutation-score quality gates.

Development follows **Spec-Driven Development (SDD)**: every change starts
from a spec in `specs/` following `specs/_template.md`
(Context / Requirements / Design / Contracts / Tests / Criteria / Limits +
Approval), with the Definition of Done in
`specs/phase-00-constitution/00-02-sdd-governance/spec.md` — verifiable
acceptance criteria per command or test, mandatory spec↔code↔test
traceability, and `Memoria.md` as the living state log.

Current status is honest: catalog CRUD + base auth (login, lockout, login-2FA
verify) are implemented and green; real JWT issuance, TOTP setup, saga
runtime, observability, and CI pipelines are specified but pending. See
`specs/` for the source of truth and `Memoria.md` for the current state and
decisions.

## Stack

Runtime (`WebAPIDevSecOpsScallingSDD/WebAPIDevSecOpsScallingSDD.csproj`,
`net10.0`):

- `Asp.Versioning.Mvc` + `Asp.Versioning.Mvc.ApiExplorer` 10.2.1
- `FluentValidation` 12.1.1 (DTO validators)
- `Microsoft.EntityFrameworkCore.SqlServer` / `.InMemory` 10.0.12
  (SQL Server or InMemory via `UseInMemoryDatabase`)
- `StackExchange.Redis` 2.9.32 (cache-aside with `IMemoryCache` fallback)
- `Scalar.AspNetCore` 2.17.13 + `Microsoft.AspNetCore.OpenApi` 10.0.12
  (Dev-only API reference)

Tests (xUnit 2.9.3 + coverlet):

- `UnitTest` — service/validator logic with fakes
- `IntegrationTest` — `WebApplicationFactory` + Testcontainers
  (`Testcontainers.MsSql` / `Testcontainers.Redis` 4.13.0)
- `SecurityTest` — anonymous auth behavior (reuses `UnitTest` helpers)
- `DatabaseTest` — migrations and seed on real SQL Server
- `ContractTest`, `PerformanceTest` (NBomber), `ChaosTest` — placeholders
- Mutation testing via Stryker.NET (`stryker-0301.json` … `stryker-0307.json`)

Solution format and guardrails:

- `WebAPIDevSecOpsScallingSDD.slnx` uses the new `.slnx` XML format
  (not `.sln`) with 9 projects.
- `Directory.Build.props`: `AnalysisMode=All`, `TreatWarningsAsErrors`,
  `SonarAnalyzer.CSharp`, `NuGetAudit`.
- `nuget.config` with Package Source Mapping (nuget.org only).

## Getting started

```powershell
Copy-Item WebAPIDevSecOpsScallingSDD/appsettings.Example.json WebAPIDevSecOpsScallingSDD/appsettings.json
dotnet restore
dotnet build -c Release --no-restore
dotnet run --project WebAPIDevSecOpsScallingSDD/WebAPIDevSecOpsScallingSDD.csproj
```

Notes:

- `UseInMemoryDatabase: true` (default) runs without SQL Server; set it to
  `false` with a real `ConnectionStrings:Default` for SQL Server.
- Docker Desktop must be running: `IntegrationTest` (Redis) and
  `DatabaseTest` (SQL Server) use Testcontainers.
- `docker compose -f deploy/docker-compose.local.yml up -d` is the intended
  local-infra command, but the compose file is still pending (phase 09).

## Configuration

Real keys from `WebAPIDevSecOpsScallingSDD/appsettings.Example.json`
(placeholders only, no secrets in Git):

- `ConnectionStrings:Default`, `UseInMemoryDatabase`, `SkipMigration`
- `Jwt:Key` (≥32 bytes placeholder), `Jwt:Issuer`, `Jwt:Audience`
- `Kestrel:Limits`, `Cors:AllowedOrigins` (single origin)
- `AssemblyIntegrity:ExpectedSha256` (startup integrity check)
- `Redis:ConnectionString`, `EnableProviderStates` (non-prod provider states)

Local secrets go in untracked files (`appsettings.Local.json`,
`appsettings.*.Local.json`, `*.secrets.json`, `.env` — see `.gitignore`).
Tracked `appsettings.json` / `appsettings.Production.json` must keep
placeholder values only. To use real keys locally without risking a commit,
each developer runs (and never commits the result):

```powershell
git update-index --skip-worktree WebAPIDevSecOpsScallingSDD/appsettings.json WebAPIDevSecOpsScallingSDD/appsettings.Production.json
```

Revert with `--no-skip-worktree`. Mid-term plan: move `AppSettingsTests`
to env vars or a tracked `appsettings.Test.json` with fake data so the real
files can be ignored entirely.

## Architecture and endpoints

Conventions: `api/v{version}/[controller]`, `ApiVersion("1.0")` via URL
segment, PascalCase JSON (`PropertyNamingPolicy = null`), DTOs +
FluentValidation, `AdminPolicy` (Admin role) on catalog writes.

Controllers (`Controllers/V1/`, 8):

| Controller | Route | Endpoints |
|---|---|---|
| `PingController` | `api/v1/ping` | `GET /` probe |
| `CliClienteController` | `api/v1/clientes` | CRUD + `search` + `autocomplete` |
| `EmpEmpleadoController` | `api/v1/empleados` | CRUD + `search` |
| `ProProductoController` | `api/v1/productos` | CRUD + `search` |
| `SegUsuarioController` | `api/v1/usuarios` | CRUD + `search` + `autocomplete` |
| `VenCatEstadoController` | `api/v1/estados-venta` | CRUD |
| `LoginController` | `api/v1/auth` | `POST login` (anonymous) |
| `Login2FaController` | `api/v1/auth` | `POST login2fa/verify` (anonymous) |

Platform endpoints: `/health` (liveness), `/health/ready` (readiness,
includes Redis), `/ping`, `/scalar` + `/openapi` (Development only),
`POST /provider-states` (non-prod, gated by `EnableProviderStates`).

Middleware order (`Program.cs`): ForwardedHeaders → HSTS (non-Dev) →
HttpsRedirection → CORS → OpenApi/Scalar (Dev) → Authentication →
Authorization → Controllers → HealthChecks.

## Testing and quality

CI order: restore → build → unit → integration → security → database →
contract → mutation (nightly) → performance (nightly) → chaos (nightly).
Tests run with `--no-build` after a Release build with 0 errors.

```powershell
dotnet build -c Release --no-restore
dotnet test UnitTest/UnitTest.csproj -c Release --no-build
dotnet test IntegrationTest/IntegrationTest.csproj -c Release --no-build
dotnet test SecurityTest/SecurityTest.csproj -c Release --no-build
dotnet test DatabaseTest/DatabaseTest.csproj -c Release --no-build
dotnet stryker --config-file stryker-0307.json
```

Measured state (06-Oct-2026):

- Build Release 0 errors / 0 warnings
- Unit 150/150, Integration 47/47, Security 43/43, Database 2/2
- Stryker 100% on `LoginService` (`stryker-0306.json`) and on
  `Login2FaService` + `TotpService` (`stryker-0307.json`)
- Real coverage gate is the mutation score (line coverage threshold
  reference: 45% via `scripts/check_coverage.py`, stub until phase 07)

Rules learned the hard way:

- Always `dotnet build` after Stryker before any `--no-build` test run
  (Stryker leaves mutant binaries in `bin/`).
- Stryker runs take ~15 min per service config; first runs rarely hit 100%.
- `ignore-mutations Boolean` does **not** filter `||→&&` (Logical) mutants.
- Tests sharing the InMemory database must delete their data
  (DELETE with `RowVersion`) to avoid cross-test pollution.
- No `ConfigureAwait(false)` in `[Fact]` bodies (xUnit1030); helpers only.

## Key decisions

- **Reduced scope with NOTEs** (same pattern as login T1): temp/token are
  opaque 32-byte values (`NOTE 04-01` → real HS256 JWT with `2fa_temp`
  claim); TOTP is a deterministic fake accepting `123456`
  (`NOTE 03-09` → real OtpNet with ±1 step window).
- **Cache TTL capped at 120s** (`CacheService` throws above 2 min), so the
  2FA temp and lockout use 120s with `NOTE (04-02)` for the real 5 min /
  15 min values.
- **Temp key `cache:login2fa:{hex}`**: hex (not Base64) so issued temps can
  never contain `CacheService` forbidden patterns
  (`password/secret/token`); attacker-controlled temps are hex-gated to a
  clean 401 instead of a 500.
- **Anti-enumeration**: identical 401 bodies for unknown user vs wrong
  password (and bad vs unknown temp), plus dummy-hash/dummy-TOTP verification
  on misses; secrets never in DTOs, cache keys, or logs.
- **Lockout after 5 failures** (`attempts:{user}` counter +
  `lockout:{user}` flag); 1–5 → 401/401, next → 423.
- Naming uses `2Fa` (not `2fa`) to satisfy Sonar S101.

## SDD workflow and roadmap

- New work: write/extend the spec from `specs/_template.md`, implement the
  vertical slice (entity → DTO → validator → controller → service →
  tests), keep Stryker at 100% for touched services, reconcile the
  spec/plan with real file names and test counts, update `Memoria.md`.
- One PR = one feature/fix; no secrets, no TODOs without an issue, no dead
  code, no unused usings.
- Roadmap: phases 04 (JWT/credentials/hardening/rate-limit), 06 (saga
  runtime), 07 (quality/supply chain), 08 (observability), 09 (CI/CD, AWS,
  chaos), 10 (testing strategy) are specified but pending; phases 00, 01,
  02, 02-5, and 03-00…03-07 are approved and implemented.
- Known gaps (not code, just not built yet): `deploy/` has no
  compose/CloudFormation/Grafana files, `.github/workflows/` is empty,
  `scripts/check_coverage.py` and `.semgrep` rules are stubs, and the PR
  checklist lives in `.github/pull_request_template.md`.
