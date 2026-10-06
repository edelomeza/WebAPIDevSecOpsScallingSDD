---
name: 01-startup-health
description: Checks de salud y arranque resiliente con assembly integrity
---

## Propósito

Checks de salud y arranque resiliente.

## Cuándo usarla

Al arrancar la app y definir `/health*`.

## Precondiciones

`01-01`, `01-02` definidas.

## Pasos

1. Configurar `/health`, `/health/ready`, `/health-ui`.
2. Añadir assembly integrity SHA-256.
3. Migraciones tolerantes a fallo.
4. Warmup Dev con `PORT`, `DB_USER`, `DB_PASSWORD` override.

## Checklist

`/health/ready` 200 con DB, 503 sin DB; assembly integrity no tumba app en Dev.

## Criterios de done

`IntegrationTest/Health/HealthTests.cs` verde; assembly integrity en logs.

## Límites/trampas

No exponer stack traces en prod; fallo de migración no debe tumbar app.

## Referencias

`01-03`, `Program.cs`, `HealthTests`.
