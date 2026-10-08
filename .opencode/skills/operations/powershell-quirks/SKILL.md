---
name: powershell-quirks
description: Evitar falsos negativos en scripts PowerShell 5.1
---

## Propósito

Evitar falsos negativos en scripts.

## Pasos

`-UseBasicParsing`; `utf-8-sig`; validar CRLF en variables CI.
Para inspeccionar `mutation-report.json` (PowerShell enreda la navegación del JSON): script `.js` en temp + `node` (patrón probado `03-13/03-14`: contar por `status` y listar no-`Killed` con `mutatorName`+`location`).

## Checklist

Status 0 no implica OK; verificar con grupo control.

## Referencias

`agent.md` Fase 6.
