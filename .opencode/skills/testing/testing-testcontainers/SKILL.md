---
name: testing-testcontainers
description: SQL real en contenedor con puerto fijo para tests de base de datos
---

## Propósito

SQL real con puerto fijo.

## Pasos

`ContainerBuilder` + `WithPortBinding(hostPort,1433)`. Puertos fijos por suite para permitir restart: MsSql `14333` (migraciones), `14334` (provider-states), `14335` (schema), `14336` (race `03-10`); Redis real para `CacheFallbackTests` y `503` determinista en `/health/ready`.

## Checklist

Restart sobrevive; migraciones aplican; rollback a "0"; daemon Docker corriendo (ver `docker version` Server) antes de ejecutar.


## Límites/trampas

Orden v4 `(hostPort, containerPort)`.
Evidencia ambiental (sin cambio funcional): Docker detenido `03-15` (misma regla `03-02/11/12/13`); Docker ausente `03-09` (Testcontainers no ejecutables, solo `TwoFactor 6/6` sin Docker); `55/56` en `03-16` solo por Redis-Docker ambiental.

## Referencias

`DatabaseTest`, `IntegrationTest`.
