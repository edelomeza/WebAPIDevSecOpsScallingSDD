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

## Checklist

Restart sobrevive; migraciones aplican; rollback a "0".

## Límites/trampas

Orden v4 `(hostPort, containerPort)`.

## Referencias

`DatabaseTest`, `IntegrationTest`.
