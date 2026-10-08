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

## Checklist

12 tablas; rollback OK; seed idempotente.

## Criterios de done

`dotnet ef migrations` aplica y revierte.

## Límites/trampas

InMemory no genera `[Timestamp]` → `byte[]{1}`.

## Referencias

`02-01`, `02-02`.
