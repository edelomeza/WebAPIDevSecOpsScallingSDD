---
name: 05-cache-redis
description: Cache-aside con fallback IMemoryCache y degradación medida
---

## Propósito

Cache-aside con fallback.

## Cuándo usarla

En cada slice con lectura repetida (`03-01`…`03-12` lo aplican).

## Pasos

Esquema canónico probado: `cache:{ent}:{id}` + páginas `cache:{ent}:page:{version}:...` (o `search`/`autocomplete` versionados), TTL 60s (`CacheService` exige 0–120s); `{ent}:version` rotada en writes (sin wildcard: `Remove` por id + `Set(version+1)`); excepción probada `03-14`: servicio GET-only sin `VersionKey` ni `InvalidateAsync` (sin writes que invalidar → 100% Stryker al primer run); llaves SIEMPRE interpoladas `$"..."` (concatenar `string+int` no compila bajo mutación y Sonar); `AsNoTracking` + `OrderBy(id)`; si el write muta stock, invalidar también `producto:{id}` + `producto:version` (stale 60s cazado por test en `03-11`).
Multiplexer (`AbortOnConnectFail=false`, `ConnectTimeout=2000`, `SyncTimeout=1000`, retry exponencial 5000) resuelto desde `IConfiguration` en runtime (no eager: rompe overrides de `WebApplicationFactory`); `RedisHealthCheck` solo en `/health/ready` (503 al caer), liveness siempre 200.

## Checklist

Multiplexer options + lectura runtime; asserts de llave exacta/TTL/valores-versión + test caché-stale en Unit; prohibidos `password/secret/token` en llaves.

## Checklist

Multiplexer options (`AbortOnConnectFail=false`, timeouts, retry exponencial).

## Criterios de done

Redis caído → fallback ≤500ms, `/health` 503.

## Límites/trampas

`healthChecks.AddRedis()` crea multiplexer con defaults.
`CacheService` NO expone `Increment` atómico: imposible secuencial `F-{año}-{seq}` con TTL máx 120s (causa del diferimiento `03-14`→fase 06; ver matriz en `03-saga-endpoints`).

## Referencias

`05-01`.
