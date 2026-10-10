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

Current status is honest: phase 03 is finished and signed 19/19 —
catalog CRUD + search/autocomplete, base auth (login, lockout, login-2FA
verify, opaque refresh/logout, real TOTP provisioning with OtpNet),
legacy sync sale with details, the minimal saga path (order → payment →
read-only invoice → filtered dashboard), uniform errors via middleware,
a living 56-row endpoint catalog with a drift-guard script, and 14 JSON
fixtures captured from the real wire are implemented and green; real JWT
issuance, saga runtime (bus/consumers/compensation), observability, and
the full CI/CD matrix are specified but pending. See `specs/` for the
source of truth and `Memoria.md` for the current state and decisions.

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
- `ContractTest` — 14 JSON fixtures captured from the real wire
  (`ContractTest/Fixtures/`, regenerate with `$env:CONTRACT_CAPTURE="1"`) +
  naming-convention test (`IsConventional`); Pact stays in phase 10
- `PerformanceTest` (NBomber), `ChaosTest` — placeholders
- Mutation testing via Stryker.NET (per-slice configs `stryker-0301.json`
  through `stryker-0316.json` plus `stryker-0309.json`, gate ≥ 80%)

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
segment, legacy-measured JSON naming (lowercase `str/int/dec/dte/bln`
prefixes + rest PascalCase + `id`/suffix — enforced by `IsConventional`
in `ContractTest`, NOT pure PascalCase), DTOs + FluentValidation. Auth is
explicit per endpoint in one of three valid patterns: `AdminPolicy`
(Admin role) on catalog and saga endpoints, bare `[Authorize]` (Bearer
without policy, e.g. legacy sale and logout — a `User` gets 200, not 403,
by design), or `[AllowAnonymous]` on login / refresh. The living endpoint
matrix is `docs/endpoints.md` (56 rows, machine-checked by
`scripts/check_endpoints.ps1` — the spec only links it, never duplicates it).

Controllers (`Controllers/V1/`, 17):

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
| `TwoFactorController` | `api/v1/two-factor` | `POST setup` (Bearer, no request DTO) + `POST verify` (Bearer, real OtpNet ±1 window) |
| `RefreshController` | `api/v1/auth` | `POST refresh` (anonymous, rotates opaque token) |
| `LogoutController` | `api/v1/auth` | `POST logout` (Bearer, revokes via `jti ?? hash`) |
| `VentaController` | `api/v1/ventas` | `POST` (Bearer, 201) + `GET {id}` (auxiliary) |
| `VentaDetalleController` | `api/v1/ventas/detalles` | `POST ~/ventas/{idVenta}/detalles` + `GET/DELETE {id}` + `GET autocomplete-productos` (Bearer, owner-only 403) |
| `VentasPedidoController` | `api/v1/ventas/pedido` | `POST` + `GET {id:guid}` (AdminPolicy, saga entry, no stock discount) |
| `VentasPagoController` | `api/v1/ventas/pago` | `POST` + `GET {id}` + `GET pedido/{pedidoId}` (AdminPolicy, duplicate → 409) |
| `VentasFacturaController` | `api/v1/ventas/factura` | `GET {id}` read-only (AdminPolicy, folio deferred to phase 06) |
| `VentasDashboardController` | `api/v1/ventas/dashboard` | `GET` with `desde/hasta/estadoSaga` filters (AdminPolicy) |

Platform endpoints: `/health` (liveness), `/health/ready` (readiness,
includes Redis), `/ping` (root, plain `"pong"`) + `/api/v1/ping`
(controller JSON), `/api/v1/probe/{timeout,error,forbidden}` (non-prod
only, gated by `EnableProviderStates`), `/scalar` + `/openapi`
(Development only), `POST /provider-states` (non-prod, gated by
`EnableProviderStates`).

Middleware order (`Program.cs`): `SecurityHeadersMiddleware` (outermost,
4 headers + CSP nonce; `scalar`/`openapi` exempt from CSP) →
`ExceptionHandlingMiddleware` (single try/catch, uniform `ErrorResponse`)
→ ForwardedHeaders → HSTS 365d (non-Dev, `MaxAge` via `AddHsts`) →
HttpsRedirection → CORS → OpenApi/Scalar (Dev) → Authentication →
Authorization → Controllers → HealthChecks.

## Testing and quality

CI order: restore → build → unit → integration → security → critic →
endpoints → contract → semgrep. Tests run with `--no-build` after a
Release build with 0 errors.

```powershell
dotnet build -c Release --no-restore
dotnet test UnitTest/UnitTest.csproj -c Release --no-build
dotnet test IntegrationTest/IntegrationTest.csproj -c Release --no-build
dotnet test SecurityTest/SecurityTest.csproj -c Release --no-build
dotnet test DatabaseTest/DatabaseTest.csproj -c Release --no-build
dotnet test ContractTest/ContractTest.csproj -c Release --no-build
powershell -File scripts/check_endpoints.ps1
powershell -File scripts/critic-guardrails.ps1
dotnet stryker -f stryker-0316.json
```

Measured state (09-Oct-2026):

- Build Release 0 errors / 0 warnings
- Unit 289/289, Integration 85/85, Security 62/62, Database 2/2, Contract 4/4
- `check_endpoints.ps1` OK (56 routes), `critic-guardrails.ps1` PASS
- Stryker ≥ 80% gate on every slice (100% on most services, e.g.
  `LoginService`, `Login2FaService`, `VentasFacturaService`,
  `VentasDashboardService`, `ExceptionHandlingMiddleware`; 82–95% on broader
  services such as `VentaService`, `VentasPedidoService`,
  `VentaDetalleService`, `VentasPagoService` — residue is relational-only
  branches outside `UnitTest` scope; `TwoFactorService` 93.41%)
- Real coverage gate is the mutation score (line coverage threshold
  reference: 45% via `scripts/check_coverage.py`, stub until phase 07)

Rules learned the hard way:

- Always `dotnet build` after Stryker before any `--no-build` test run
  (Stryker leaves mutant binaries in `bin/`).
- Stryker runs take ~15 min per service config; first runs rarely hit 100%.
- `ignore-mutations Boolean` does **not** filter `||→&&` (Logical) mutants.
- Tests sharing the InMemory database must delete their data
  (DELETE with `RowVersion`) to avoid cross-test pollution; collections run
  in series via `IntegrationTest/xunit.runner.json`.
- No `ConfigureAwait(false)` in `[Fact]` bodies (xUnit1030); helpers only.
- Reproduce pristine-restore NU1100 failures with an empty packages folder
  (`dotnet restore --force --no-cache /p:RestorePackagesPath=<empty>`):
  a warm cache hides source-mapping gaps.
- `semgrep scan` rejects `--metrics=off` (only `semgrep ci` accepts it);
  pin actions to immutable SHAs (SAST flags mutable tags).
- EF InMemory elevates `TransactionIgnoredWarning` to an error under
  warnings-as-errors: guard explicit transactions with `IsRelational`.
- Kill `catch` mutants with a `ThrowingContext : AppDbContext` subclass
  (`ThrowOnSave` flag) instead of widening `UnitTest` scope to MsSql.
- `NOTE (XX-YY)` for deferred scope, never `TODO`: S1135 plus
  `TreatWarningsAsErrors` turns `TODO` into a build error.

## Key decisions

- **Reduced scope with NOTEs** (same pattern as login T1): temp/token are
  opaque 32-byte values (`NOTE 04-01` → real HS256 JWT with `2fa_temp`
  claim); TOTP provisioning is real (OtpNet, ±1 step window, secret
  protected with DataProtection — enrollment secret returned once, covered
  by a documented critic waiver).
- Naming uses `IsConventional`, not pure PascalCase: lowercase legacy
  prefixes (`str/int/dec/dte/bln` + uppercase/digit), `id` alone or with a
  PascalCase suffix (`idCliCliente`), rest PascalCase (`RowVersion`) —
  measured from the wire in 03-18 after pure PascalCase failed the test.
- **Cache TTL capped at 120s** (`CacheService` throws above 2 min), so the
  2FA temp uses 120s with `NOTE (04-02)` for the real 5 min value; login
  lockout is persistent since `04-02` (table `SegBloqueo`, 15 min, outside
  the ephemeral cache).
- **Temp key `cache:login2fa:{hex}`**: hex (not Base64) so issued temps can
  never contain `CacheService` forbidden patterns
  (`password/secret/token`); attacker-controlled temps are hex-gated to a
  clean 401 instead of a 500.
- **Anti-enumeration**: identical 401 bodies for unknown user vs wrong
  password (and bad vs unknown temp), plus dummy-hash/dummy-TOTP verification
  on misses; secrets never in DTOs, cache keys, or logs.
- **Lockout after 5 failures** (persistent `SegBloqueo` row per name,
  15 min, since `04-02`; 2FA/2FA-verify paths still use `attempts:/lockout:`
  cache keys); 1–5 → 401/401, next → 423.
- **Passwords hashed with Argon2id** (64 MB/3 iterations, own PHC format,
  since `04-02`): BCrypt `$2a$/$2b$` only verified for migration
  (`NeedsRehash`), corrupt hashes fail closed to generic 401, never 500.
- Naming uses `2Fa` (not `2fa`) to satisfy Sonar S101.
- **Opaque refresh/logout** (`NOTE 04-01` → real HS256 JWT): refresh tokens
  are SHA-256 hex persisted with rotation link (`strReplacedByTokenHash`)
  and reuse → 401; logout revokes `blacklist:{jti}` from the `jti`/`sub`
  claim with hash-of-refresh fallback, TTL 120s (`NOTE 04-02`).
- **Legacy sync sale** (Bearer, owner in body + `NOTE 04-01` → `sub` claim):
  server-side totals (`decPrecio × piezas`), FK triple-check → 422, any
  stock/concurrency conflict → 409; details share one explicit transaction
  and restore stock on delete.
- **Minimal saga, fake-first** (`NOTEs 06-01…06-04` → MassTransit/bus in
  phase 06): order creation does **not** discount stock (validated later by
  the stock consumer), emits `PedidoCreadoEvent` via
  `FakePedidoEventPublisher`; duplicate non-null `strIdTransaccion` → 409
  (multiple `NULL`s allowed by the filtered unique index); invoice is
  GET-only (no atomic `Increment` in `CacheService`, folio `F-{año}-{seq}`
  deferred); dashboard aggregates per-entity dates with `queue depth 0`
  (`NOTE 06-01`) and sanitizes `password/secret/token/":"` from cache keys.

## SDD components

Spec-Driven Development here is machinery, not paperwork. Integrated
components (each one earned by a slice that needed it):

| Component | What it is | Where it lives |
|---|---|---|
| Spec template | Mandatory 7-section spec + Approval block | `specs/_template.md` |
| Governance / DoD | Borrador-with-evidence vs signed approval, testable criteria | `specs/phase-00-constitution/00-02-sdd-governance/spec.md` |
| `Memoria.md` | Living log: state, decisions, lessons per phase | repo root |
| Traceability | 4-layer pending log (`NOTE` + spec + task + Memoria) | `.opencode/skills/core/traceability/SKILL.md` |
| Deferred scope | Fake-first with traceable `NOTE (XX-YY)`, never `TODO` | `.opencode/skills/core/deferred-scope-fakes/SKILL.md` |
| Living catalogs | Canonical doc the spec links (never duplicates): 56-row endpoints | `docs/endpoints.md` |
| Contract fixtures | 14 JSON captured from the real wire (`CONTRACT_CAPTURE=1`) | `ContractTest/Fixtures/` |
| Drift guards | Canonical doc + extractor script + parallel CI job | `.opencode/skills/operations/drift-guards/SKILL.md` (`check_endpoints.ps1`, jobs `endpoints`/`contract`) |
| Critic gate | 4 blocking diff-scoped checks, pre-push | `scripts/critic-guardrails.ps1` (skill `operations/critic-guardrails`) |
| Skills | 48 rule packs; agents link them, never copy | `.opencode/skills/` |

## Sub-agents

Hybrid convention: agents define role, permissions and done-criteria;
technical rules live in skills (agents only link `SKILL.md`). Manual
invocation in dev (`@slice-scaffolder`, …); CI runs only the lightweight
`critic` job. Identity and convention: `.opencode/agents/README.md`.

| Agent | Role | Permissions |
|---|---|---|
| `slice-scaffolder` | Phase A: compilable vertical-slice skeleton intra-PR (+ fake→real swap variant for phase 04); never committed without its Phase B | edit+bash allow |
| `security-reviewer` | Pre-push gate: critiques without editing or running Stryker (+ phase-04 checks: JWT, hashing, rate-limit, headers, secrets in logs) | edit deny |
| `traceability-clerk` | Living 04-04/04-05 matrices + addenda: reports drift, never edits (brought forward from Phase 2 for phase 04) | edit deny |

## SDD workflow and roadmap

- New work: write/extend the spec from `specs/_template.md`, implement the
  vertical slice (entity → DTO → validator → controller → service →
  tests), keep Stryker ≥ 80% for touched services plus a restorative
  `dotnet build` after every run, reconcile the
  spec/plan with real file names and test counts, update `Memoria.md`.
- One PR = one feature/fix; no secrets, no TODOs without an issue, no dead
  code, no unused usings. Pre-push gate: `pwsh scripts/critic-guardrails.ps1`
  (4 blocking diff-scoped checks; 401 parity / complexity / Stryker need test
  evidence). Manual sub-agents live in `.opencode/agents/` (hybrid: they link
  `.opencode/skills/`, never copy); CI runs only the lightweight `critic` job.
- Roadmap: phases 00, 01, 02, and 03-00…03-18 are approved and implemented
  (phase 03 signed 19/19). Still pending: PR #18 merge, phases 04
  (JWT/credentials/hardening/rate-limit), 06 (saga runtime), 07
  (quality/supply chain), 08 (observability), 09 (CI/CD, AWS, chaos),
  10 (Pact provider verification).
- Known gaps (not code, just not built yet): `deploy/` has no
  compose/CloudFormation/Grafana files, `scripts/check_coverage.py` and
  `.semgrep` custom rules are stubs, and the 8-check PR checklist lives in
  `.github/pull_request_template.md`. `.github/workflows/ci-pr.yml` runs
  build → unit/security/integration → critic/endpoints/contract → semgrep
  (mutation/perf/chaos stay nightly).
