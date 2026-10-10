---
name: 01-foundation
description: Dejar compilando la solución con pipeline de middleware correcto
---

## Propósito

Dejar compilando la solución con pipeline correcto.

## Cuándo usarla

Fase 1.

## Pasos

Crear `.slnx`, `Directory.Build.props`, NuGet, `appsettings*.json`, Program.cs con middleware ordenado, Serilog, Kestrel, CORS/HSTS, health.
Orden vigente post-04: `SecurityHeadersMiddleware` outermost absoluta → `ExceptionHandlingMiddleware` → resto `01-02` (HSTS 365d solo `MaxAge` vía `AddHsts`, no existe `UseHsts(Action)`; gate no-Dev) → `UseRateLimiter` antes de `UseAuthentication` (`AddRateLimiter` 5 policies, ver `core/auth-matrix`) → JwtBearer tras flag `Authentication:UseJwtBearer` (`true` default, `false`=Anonymous legacy, `Test` en Integration/Contract; `TokenValidationParameters` estrictos + `OnTokenValidated` anti-`blacklist:{jti}`). Claves nuevas en tabla `01-04`: `Authentication:UseJwtBearer`, `PasswordHasher:MemoryKBytes/Iterations` (sin secretos), `RateLimiting:*` + env `PERF_RATELIMIT_MULTIPLIER` fuera del JSON.

## Checklist

Build Release 0 errores; `/health` 200; pipeline en orden documentado.

## Criterios de done

App arranca en Dev con `/scalar`.

## Límites/trampas

Orden de middleware importa; HSTS solo no-Dev; `UseHsts(Action<HstsOptions>)` no existe (configurar por `AddHsts`); rate-limit sin `UseRateLimiter` antes de `Auth` no aplica; lectura eager de config en el registro no ve overrides WAF (usar `IOptionsMonitor` lazy).

## Referencias

`01-01`, `01-02`, `01-03`, `01-04`, `core/auth-matrix`, `phases/04-jwt-refresh`, `phases/04-security-core`.
