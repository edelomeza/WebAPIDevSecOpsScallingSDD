# 04-04 — Matriz rate-limit/auth

## Contexto
Matriz explícita endpoint ↔ auth/rate-limit. **Depende de**: `04-02`, `04-03`.

## Requisitos
1. Implementar policies: Login 5/5min, Login2faVerify 10/5min, Global 1000/min, Admin 200/min, ConcurrentWrites 10.
2. Cada endpoint con auth y policy explícitas.

## Diseño
- `UseRateLimiter` en `Program.cs` antes de `UseAuthentication` (orden constitucional) con 5 policies
  (`Services.RateLimitOptions.*PolicyName`): `Login`/`Login2faVerify`/`Global`/`Admin` SlidingWindow por IP
  (`QueueLimit=0`), `ConcurrentWrites` por IP; `OnRejected` → 429 `ErrorResponse` uniforme + `Retry-After`
  best-effort. Opciones lazy vía `IOptionsMonitor` (misma lección que Redis 05-01 y DbContext 02-02:
  `builder.Configuration` en registro aún no incluye overrides de `WebApplicationFactory`).
- Matriz canónica en `docs/rate-limit-matrix.md` (49 filas con policy + exclusiones ping/health/probe);
  `docs/endpoints.md` solo enlaza, no duplica. Catálogo de referencia en `03-17`/`docs/endpoints.md`.

## Contratos
| Endpoint | Auth | Rate limit policy |
|---|---|---|
| POST /api/v1/auth/login | Anónimo | Login 5/5min |
| POST /api/v1/auth/login2fa/verify | Anónimo | Login2faVerify 10/5min |
| POST /api/v1/auth/refresh | Anónimo | Global 1000/min |
| POST /api/v1/auth/logout | Bearer | Global 1000/min |
| /api/v1/two-factor/* | Bearer | Global 1000/min |
| CRUD clientes/empleados/productos/estados-venta/usuarios | Bearer `AdminPolicy` (rol Admin; `AdminOnly` no existe en código) | Admin 200/min |
| GET /api/v1/ventas/search, GET {id}, GET detalles/* | Bearer | Global 1000/min |
| POST /api/v1/ventas, POST /detalles, DELETE detalles | Bearer | ConcurrentWrites 10 |
| /api/v1/ventas/pedido, /pago, /factura, /dashboard | Bearer `AdminPolicy` | Admin 200/min |

## Tests
- `SecurityTest/RateLimit/RateLimitTests` (4): 429 ante exceso en `Login`/`Global`/`Admin` + estructural
  reflection (toda action con `Http*` salvo `PingController` exige policy conocida).
- `UnitTest/RateLimit/RateLimitOptionsTests` (4): defaults = spec, `ApplyMultiplier`, clamp, nombres.
- Colaterales: `IntegrationTest/Login.FiveFailuresThenLockedOut` y `SecurityTest/Login.RepeatedUnknownFailuresLockOut`
  elevan `RateLimiting:LoginPermitLimit=100` vía config (6 logins > 5/5min; aserciones intactas).

## Criterios
- Cada endpoint tiene política explícita (verificado por test estructural + `docs/rate-limit-matrix.md` 49 filas).
- `dotnet build -c Release` 0/0; UnitTest 330/330; SecurityTest 81/81; IntegrationTest 99/99 (92+7 tras
  arrancar Docker); ContractTest 4/4; DatabaseTest 2/2; `critic-guardrails.ps1` PASS; `check_endpoints.ps1` OK.

## Límites
- Relajación de límites solo vía env `PERF_*` en perf (no en prod).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario (1 Revisor)
- **Fecha:** 10-Oct-2026
- **Detalle:** implementado 10-Oct-2026 (5 policies SlidingWindow/concurrencia + 429 uniforme +
  `docs/rate-limit-matrix.md` + 8 tests nuevos; suites verdes); revisado y aprobado por @usuario.
