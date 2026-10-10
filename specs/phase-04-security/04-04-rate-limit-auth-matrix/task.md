# 04-04 — Rate Limit & Auth Matrix

## T1 — Matriz auth/rate-limit
- **Crear**: `docs/rate-limit-matrix.md`.
- **Verificar**: cada endpoint con política explícita.
- **Implementar** (alcance ampliado cerrado con usuario 10-Oct-2026): `RateLimitOptions` +
  `AddRateLimiter`/`UseRateLimiter` + `[EnableRateLimiting]` en 16 controllers +
  `SecurityTest/RateLimit` (429) + `UnitTest/RateLimit`; colaterales de lockout con límite elevado.
- **Estado**: ejecutado 10-Oct-2026, ✅ Aprobado (@usuario).
