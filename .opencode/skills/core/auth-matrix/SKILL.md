---
name: 04-auth-matrix
description: Matriz auth/rate-limit por endpoint + checklist OWASP ASVS L2
---

## Propósito

Matriz auth/rate-limit por endpoint + checklist OWASP ASVS L2.

## Cuándo usarla

Al proteger endpoints y al auditar seguridad.

## Precondiciones

`04-01`, `04-02`, `04-03`, `04-04`, `04-05` definidos.

## Pasos

1. Marcar cada endpoint con `[Authorize]` o `[Authorize(Policy = "AdminPolicy")]` (requiere rol `Admin`; es la policy real — `AdminOnly` no existe en código).
2. Rate limiting vigente desde `04-04` (ya no diferido): `Services/RateLimitOptions.cs` (defaults + `ApplyMultiplier` con clamp + `*PolicyName` consts) + `AddRateLimiter` 5 policies `QueueLimit=0` + `OnRejected` 429 `ErrorResponse` sin Detail + `Retry-After` best-effort; `UseRateLimiter` antes de `UseAuthentication`; `[EnableRateLimiting]` en 16 controllers (9×`Admin` a nivel clase, anónimas/Bearer con `Login`/`Login2faVerify`/`Global`, `Venta`/`VentaDetalle` clase `Global` + override `ConcurrentWrites` en POST/DELETE); relajación solo vía `PERF_RATELIMIT_MULTIPLIER`, nunca en prod. Canónico: `docs/rate-limit-matrix.md` (49 filas + exclusiones ping/health/probe/provider-states); `docs/endpoints.md` enlaza (columna = policy vigente).
3. Verificar claims (`NameIdentifier`/`sub` para ownership; `role` para `AdminPolicy` — emitir literal `"role"` porque `ClaimTypes.Role` no sobrevive al mapa outbound, inbound lo eleva a `Role`; flag `Authentication:UseJwtBearer` + `OnTokenValidated` anti-`blacklist:{jti}`, ver `phases/04-jwt-refresh`).
4. Matriz de tests probada: `Admin→2xx`, `User→403` con `AdminPolicy`, `User→200` en Bearer sin policy (desviación espejo `03-10/03-11`, no 403), anónimo→401 sin fugas (`Assert.DoesNotContain` del payload).
5. Fila `api/v1/two-factor` (`03-09`, detalle en `phases/04-totp-provisioning`): `Setup/Verify/Remove` con auth explícita + policy rate-limit explícita + `401` idéntico; llaves `2fa:{userId}` + `attempts:/lockout:`.
6. Revisar checklist ASVS L2 canónico `docs/asvs-l2-checklist.md` (10 capítulos V1–V9+V14, 6×Cubierto + 4×Parcial con deuda explícita de 04-01/04-02/04-03; cada fila con evidencia archivo+test; el `spec.md` solo enlaza, nunca duplica — ver `operations/drift-guards`).

## Checklist

- Matriz endpoint↔auth↔rate-limit completa.
- OWASP ASVS L2 con estado de cada ítem.
- Cada endpoint tiene auth y rate-limit explícitos.

## Criterios de done

- Cada endpoint tiene auth y rate-limit explícitos.
- ASVS L2 revisado y con evidencia.

## Límites/trampas

- No dejar endpoints sin rate-limit.
- No loggear JWT.
- No marcar ASVS como OK sin evidencia.
- Config rate-limit vía `IOptionsMonitor` lazy: la lectura eager de `builder.Configuration` en el registro no ve overrides de `WebApplicationFactory` (el 429 no disparaba; 3ª instancia de la lección Redis 05-01/DbContext 02-02).
- `[EnableRateLimiting]` de action prevalece sobre el de clase (verificado empíricamente por los 429); `new WebApplicationFactory<Program>().WithWebHostBuilder(...)` inline exige pragma CA2000 como en `CreateAdminFactory`.

## Referencias

`docs/rate-limit-matrix.md`, `docs/asvs-l2-checklist.md`, `docs/endpoints.md`, `phases/04-jwt-refresh`, `phases/04-totp-provisioning`, `operations/drift-guards`, `04-04`, `04-05`, `CHECKLIST_PR.md`.
