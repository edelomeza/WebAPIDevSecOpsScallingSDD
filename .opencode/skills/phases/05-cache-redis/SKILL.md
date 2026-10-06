---
name: 05-cache-redis
description: Cache-aside con fallback IMemoryCache y degradación medida
---

## Propósito

Cache-aside con fallback.

## Cuándo usarla

Fase 2.5.

## Pasos

AddStackExchangeRedisCache tuned; TTL 30–120s; invalidación en writes; IMemoryCache fallback; `/health` behavior.

## Checklist

Multiplexer options (`AbortOnConnectFail=false`, timeouts, retry exponencial).

## Criterios de done

Redis caído → fallback ≤500ms, `/health` 503.

## Límites/trampas

`healthChecks.AddRedis()` crea multiplexer con defaults.

## Referencias

`05-01`.
