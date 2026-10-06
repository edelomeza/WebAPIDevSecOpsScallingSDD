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

## Checklist

Build Release 0 errores; `/health` 200; pipeline en orden documentado.

## Criterios de done

App arranca en Dev con `/scalar`.

## Límites/trampas

Orden de middleware importa; HSTS solo no-Dev.

## Referencias

`01-01`, `01-02`, `01-03`.
