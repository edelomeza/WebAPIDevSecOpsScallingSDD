---
name: chaos-verification
description: Validar escenarios de caos (redis-kill, sql-kill, redis-latency)
---

## Propósito

Validar caos (redis-kill, sql-kill, redis-latency).

## Pasos

FaultInjector; verify con retry 5×5s; grupo control sin fallo; logs como prueba de vida.

## Checklist

PowerShell `-UseBasicParsing`; exit 0/1/2.

## Límites/trampas

4 causas de `verify FAIL status 0`; imágenes aspnet sin curl.

## Referencias

`ChaosTest/run-chaos.ps1`.
