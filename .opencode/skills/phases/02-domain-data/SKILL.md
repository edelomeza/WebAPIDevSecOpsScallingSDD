---
name: 02-domain-data
description: Modelar entidades, DbContext y migraciones con naming existente
---

## Propósito

Modelar entidades y migraciones.

## Cuándo usarla

Fase 2.

## Pasos

Entidades con naming (`Ven*`, prefijos), DbContext, RowVersion, migraciones, seed.
`VenPedidoPago/Factura` pre-existen desde fase 02 (no recrear; seed `F-SEED-1`); único filtrado `IS NOT NULL` admite `NULL` múltiples + InMemory no lo impone → pre-chequeo + `DbUpdateException→409` en servicio.
`04-02`: tabla `SegBloqueo` (`strNombre` único, `intIntentosFallidos`, `dteBloqueoHasta`) — bloquear por nombre cubre desconocidos (por `SegUsuario` no los cubre); `strPWD nvarchar(200)` basta (PHC Argon2id ≤120c); seed con placeholder/fake ⇒ reset password fail-closed; `PasswordHasher:` solo opciones sin secretos.

## Checklist

13 tablas (12 + `SegBloqueo`); rollback OK; seed idempotente.

## Criterios de done

`dotnet ef migrations` aplica y revierte.

## Límites/trampas

InMemory no genera `[Timestamp]` → `byte[]{1}`.

## Referencias

`02-01`, `02-02`, `04-02` (`SegBloqueo0402`, `PasswordHasherOptions`).
