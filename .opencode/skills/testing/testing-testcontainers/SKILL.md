---
name: testing-testcontainers
description: SQL real en contenedor con puerto fijo para tests de base de datos
---

## Propósito

SQL real con puerto fijo.

## Pasos

`ContainerBuilder` + `WithPortBinding(hostPort,1433)`.

## Checklist

Restart sobrevive; migraciones aplican; rollback a "0".

## Límites/trampas

Orden v4 `(hostPort, containerPort)`.

## Referencias

`DatabaseTest`, `IntegrationTest`.
