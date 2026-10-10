# plan.md — 04-04-rate-limit-auth-matrix

1. Mapeo

- Crear: docs/rate-limit-matrix.md (canónica auth/policy, 49 filas + exclusiones; `endpoints.md` enlaza).
- Código: `Services/RateLimitOptions.cs` (defaults spec + `ApplyMultiplier` + `*PolicyName`),
  `AddRateLimiter`/`UseRateLimiter` en `Program.cs` (antes de `UseAuthentication`),
  `[EnableRateLimiting]` en 16 controllers (+ overrides `ConcurrentWrites` en 3 writes),
  sección `RateLimiting` en `appsettings.Example.json`.

2. Decisiones

- Matriz endpoint↔auth/rate-limit (49 filas; `AdminOnly` no existe → solo `AdminPolicy`).
- SlidingWindow por IP + 429 `ErrorResponse` uniforme; `PERF_RATELIMIT_MULTIPLIER` multiplica todo (solo perf).
- Opciones lazy vía `IOptionsMonitor` (tercera instancia de la lección Redis/DbContext).
- Alcance: doc + código + tests (cerrado con usuario 10-Oct-2026; la task pedía solo doc).

3. Guardarraíles

- Cada endpoint con política explícita (test estructural reflection).
- Sin `token`/secretos en particiones/logs; 429 genérico (no rompe anti-enumeración).

4. Secuencia

1. Listar endpoints (56 filas `endpoints.md` → 49 con policy + 7 exclusiones documentadas).
2. Asignar auth + policy (ejecutado 10-Oct-2026; evidencia en Criterios de `spec.md`).
