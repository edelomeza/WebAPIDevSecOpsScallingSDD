---
name: redis-tuning
description: Tuning del multiplexer Redis para evitar reconnection storm
---

## Propósito

Evitar reconnection storm.

## Pasos

`AbortOnConnectFail=false`, `ConnectTimeout=2000`, `SyncTimeout=1000`, `AsyncTimeout=500`, `ConnectRetry=1`, `ReconnectRetryPolicy=ExponentialRetry(5000)`.

## Checklist

Fallback ≤500ms; `/health` refleja caída.

## Referencias

`Program.cs`, `05-01`.
