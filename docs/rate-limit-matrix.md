# Matriz rate-limit/auth (04-04, canónica)

Fuente única de verdad para auth + policy de rate-limit por endpoint.
`docs/endpoints.md` sigue siendo el canónico de método/ruta/DTOs/códigos; esta matriz no duplica esa tabla.
Verificado por `SecurityTest/RateLimit/RateLimitTests.EveryControllerActionHasExplicitRateLimitPolicy`
(reflection: toda action con `Http*` —salvo `PingController`— exige `[EnableRateLimiting]` con policy conocida).

Enforcement: `UseRateLimiter` en `Program.cs` antes de `UseAuthentication` (orden constitucional).
Rechazo: `429` con `ErrorResponse { Error, Status: 429, TraceId }` (sin `Detail`, sin secretos) + `Retry-After`
best-effort cuando el limitador provee metadata. Partición por IP (`RemoteIpAddress`, fallback `"unknown"`).

## Policies (valores spec; override por `RateLimiting:*`, relajación solo perf vía `PERF_RATELIMIT_MULTIPLIER`)

| Policy | Tipo | Límite | Ventana | Segmentos |
|---|---|---|---|---|
| `Login` | SlidingWindow | 5 | 300 s (5 min) | 5 |
| `Login2faVerify` | SlidingWindow | 10 | 300 s (5 min) | 5 |
| `Global` | SlidingWindow | 1000 | 60 s (1 min) | 4 |
| `Admin` | SlidingWindow | 200 | 60 s (1 min) | 4 |
| `ConcurrentWrites` | Concurrencia | 10 | — | — |

- `QueueLimit = 0` en las 5: el exceso se rechaza de inmediato (sin cola).
- `PERF_RATELIMIT_MULTIPLIER` (env, default 1): entero `>1` multiplica los 5 límites; `<1` se trata como 1.
  Solo se fija en entorno perf; nunca en prod. Clave registrada en `01-04/config-reference`.
- Nombres literales centralizados en `Services.RateLimitOptions` (`*PolicyName`); los controllers los
  referencian por constante, no por string suelto.

## Auth / rate-limit por endpoint

Auth real: `AdminPolicy` = `[Authorize(Policy = "AdminPolicy")]` (rol `Admin`); `AdminOnly` no existe en código.

| Método | Ruta | Auth | Policy |
|---|---|---|---|
| POST | /api/v1/auth/login | Anónimo | `Login` |
| POST | /api/v1/auth/login2fa/verify | Anónimo | `Login2faVerify` |
| POST | /api/v1/auth/refresh | Anónimo | `Global` |
| POST | /api/v1/auth/logout | Bearer `[Authorize]` | `Global` |
| POST | /api/v1/two-factor/setup | Bearer `[Authorize]` | `Global` |
| POST | /api/v1/two-factor/verify | Bearer `[Authorize]` | `Global` |
| GET | /api/v1/clientes | `AdminPolicy` | `Admin` |
| GET | /api/v1/clientes/search | `AdminPolicy` | `Admin` |
| GET | /api/v1/clientes/autocomplete | `AdminPolicy` | `Admin` |
| GET | /api/v1/clientes/{id:int} | `AdminPolicy` | `Admin` |
| POST | /api/v1/clientes | `AdminPolicy` | `Admin` |
| PUT | /api/v1/clientes/{id:int} | `AdminPolicy` | `Admin` |
| DELETE | /api/v1/clientes/{id:int} | `AdminPolicy` | `Admin` |
| GET | /api/v1/empleados | `AdminPolicy` | `Admin` |
| GET | /api/v1/empleados/search | `AdminPolicy` | `Admin` |
| GET | /api/v1/empleados/{id:int} | `AdminPolicy` | `Admin` |
| POST | /api/v1/empleados | `AdminPolicy` | `Admin` |
| PUT | /api/v1/empleados/{id:int} | `AdminPolicy` | `Admin` |
| DELETE | /api/v1/empleados/{id:int} | `AdminPolicy` | `Admin` |
| GET | /api/v1/productos | `AdminPolicy` | `Admin` |
| GET | /api/v1/productos/search | `AdminPolicy` | `Admin` |
| GET | /api/v1/productos/{id:int} | `AdminPolicy` | `Admin` |
| POST | /api/v1/productos | `AdminPolicy` | `Admin` |
| PUT | /api/v1/productos/{id:int} | `AdminPolicy` | `Admin` |
| DELETE | /api/v1/productos/{id:int} | `AdminPolicy` | `Admin` |
| GET | /api/v1/estados-venta | `AdminPolicy` | `Admin` |
| GET | /api/v1/estados-venta/{id:int} | `AdminPolicy` | `Admin` |
| POST | /api/v1/estados-venta | `AdminPolicy` | `Admin` |
| PUT | /api/v1/estados-venta/{id:int} | `AdminPolicy` | `Admin` |
| DELETE | /api/v1/estados-venta/{id:int} | `AdminPolicy` | `Admin` |
| GET | /api/v1/usuarios | `AdminPolicy` | `Admin` |
| GET | /api/v1/usuarios/search | `AdminPolicy` | `Admin` |
| GET | /api/v1/usuarios/autocomplete | `AdminPolicy` | `Admin` |
| GET | /api/v1/usuarios/{id:int} | `AdminPolicy` | `Admin` |
| POST | /api/v1/usuarios | `AdminPolicy` | `Admin` |
| PUT | /api/v1/usuarios/{id:int} | `AdminPolicy` | `Admin` |
| DELETE | /api/v1/usuarios/{id:int} | `AdminPolicy` | `Admin` |
| GET | /api/v1/ventas/search | Bearer `[Authorize]` | `Global` |
| GET | /api/v1/ventas/{id:int} | Bearer `[Authorize]` | `Global` |
| POST | /api/v1/ventas | Bearer `[Authorize]` | `ConcurrentWrites` |
| GET | /api/v1/ventas/detalles/autocomplete-productos | Bearer `[Authorize]` | `Global` |
| GET | /api/v1/ventas/detalles/{id:int} | Bearer `[Authorize]` | `Global` |
| POST | /api/v1/ventas/{idVenta:int}/detalles | Bearer `[Authorize]` | `ConcurrentWrites` |
| DELETE | /api/v1/ventas/detalles/{id:int} | Bearer `[Authorize]` | `ConcurrentWrites` |
| GET | /api/v1/ventas/pedido/{id:guid} | `AdminPolicy` | `Admin` |
| POST | /api/v1/ventas/pedido | `AdminPolicy` | `Admin` |
| GET | /api/v1/ventas/pago/pedido/{pedidoId:guid} | `AdminPolicy` | `Admin` |
| GET | /api/v1/ventas/pago/{id:int} | `AdminPolicy` | `Admin` |
| POST | /api/v1/ventas/pago | `AdminPolicy` | `Admin` |
| GET | /api/v1/ventas/factura/{id:int} | `AdminPolicy` | `Admin` |
| GET | /api/v1/ventas/dashboard | `AdminPolicy` | `Admin` |

Total: 49 filas con policy explícita (17 controllers).

## Exclusiones (sin rate-limit, documentado el porqué)

| Endpoint | Motivo |
|---|---|
| GET /api/v1/ping, GET /ping | sonda anónima de vida; debe responder siempre |
| /health, /health/ready | sondas del orquestador; un 429 aquí = reinicios en cascada |
| /api/v1/probe/*, POST /provider-states | solo no-prod tras `EnableProviderStates`; herramientas de test |

## Evidencia de tests

- `SecurityTest/RateLimit/RateLimitTests` (4): `Login` 2×401 + 3º 429 uniforme; `Global` en refresh
  2×401 + 3º 429 sin eco del token; `Admin` con JWT real rol `Admin` 2×200 + 3º 429; estructural
  reflection toda-action-con-policy.
- `UnitTest/RateLimit/RateLimitOptionsTests` (4): defaults = spec, `ApplyMultiplier`, clamp `<1`, nombres.
- Colaterales ajustados (límite elevado vía config, sin cambiar aserciones): `IntegrationTest/Login`
  `FiveFailuresThenLockedOut` (`LoginPermitLimit=100`), `SecurityTest/Login` `RepeatedUnknownFailuresLockOut`.
- Lección registrada: `builder.Configuration` en registro de servicios aún no incluye overrides de
  `WebApplicationFactory` (tercera instancia tras Redis 05-01 y DbContext 02-02); las opciones se resuelven
  lazy vía `IOptionsMonitor` en el particionador por request.
