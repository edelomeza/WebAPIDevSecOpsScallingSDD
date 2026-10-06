# 05-01 — Cache Redis

## Contexto
Cache-aside sobre Redis con fallback a IMemoryCache. **Depende de**: `01-02`, `01-03`, `01-04`.

## Requisitos
1. Claves `cache:{entidad}:*`, `blacklist:{jti}`, `attempts:{user}`, `lockout:{user}` con TTL 30–120 s.
2. Multiplexer afinado (`AbortOnConnectFail=false`, timeouts cortos, retry exponencial).
3. Fallback a IMemoryCache si Redis falla; latencia de fallback ≤500 ms.
4. Secretos (`password`, `secret`, `token`) nunca en caché.
5. Health: liveness `/health` siempre 200; readiness `/health/ready` 503 si Redis cae.

## Diseño
- DI con singleton `IConnectionMultiplexer` tolerante a fallos; `ICacheService` cache-aside resuelve desde Redis y cae a `IMemoryCache` si Redis no está disponible.
- `Set` hace write-through a Redis+memoria para que el fallback disponga del mismo dato; `Remove` limpia ambos; TTL acotado a 0–120 s.
- Prefijos obligatorios: `blacklist:`, `attempts:`, `lockout:`, `cache:`.
- `RedisHealthCheck` actúa sobre `/health/ready`; `/health` (liveness) solo chequea integridad del ensamblado y siempre responde 200.
- Note: el estándar original de la spec estaba invertido; se corrige aquí (Redis caído saca del pool de readiness, no tumba el proceso).

## Contratos
- Config: `Redis:ConnectionString` (default `localhost:6379,abortConnect=false`) en `appsettings.*.json`.
- Claves con prefijo estable y TTL legible; DI expone `ICacheService`, `IConnectionMultiplexer` singleton, `IMemoryCache`.

## Tests
- `SecurityTest/Cache/LockoutTests.cs` (4): lockout en fallback, clave con password rechazada, prefijo inválido, TTL fuera de rango.
- `IntegrationTest/Cache/CacheFallbackTests.cs` (2): set/get contra Redis real (Testcontainers), fallback ≤500 ms y `/health` 200 vs `/health/ready` 503 al caer Redis.
- `IntegrationTest/Health/HealthTests.cs`: `/health` 200; `/health/ready` 503 determinista cuando Redis no responde.

## Criterios
- `dotnet build -c Release` 0 errores 0 advertencias (9 proyectos, StackExchange.Redis 2.9.32, Testcontainers.Redis 4.13.0).
- `dotnet test SecurityTest` 8/8; `dotnet test IntegrationTest` 7/7 (Redis real); `dotnet test UnitTest` 8/8.

## Límites
Migraciones y credenciales de BD siguen pendientes de Fase 02; health UI y OTel en Fase 08; lockout/rate-limit real (pero no la infraestructura de claves) en Fase 04.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Cache-aside Redis+IMemoryCache con health liveness/readiness, tests con Testcontainers. Migrada a plantilla.
