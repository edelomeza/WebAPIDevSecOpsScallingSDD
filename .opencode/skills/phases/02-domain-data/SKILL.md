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

## Checklist

12 tablas; rollback OK; seed idempotente.

## Criterios de done

`dotnet ef migrations` aplica y revierte.

## Límites/trampas

InMemory no genera `[Timestamp]` → `byte[]{1}`.

## Referencias

`02-01`, `02-02`.
