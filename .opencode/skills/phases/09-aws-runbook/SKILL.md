---
name: 09-aws-runbook
description: Despliegue temporal AWS paso a paso (Free Tier)
---

## Propósito

Despliegue temporal AWS paso a paso.

## Cuándo usarla

Al preparar/destruir la infra Free Tier.

## Precondiciones

`09-02`, `09-04` definidos; credenciales AWS.

## Pasos

1. Crear VPC, SG, ALB.
2. Lanzar EC2 con imagen Docker Hub.
3. Aprovisionar RDS SQL Server externo.
4. Crear colas SQS + DLQ.
5. Migrar DB (`SKIP_MIGRATION=false`), seed tras `down -v`.
6. Destruir al final (cron schedule-destroy).

## Checklist

Orden ALB→EC2→SQS→migrar; ventanas de bajo costo; `DB_USER`/`DB_PASSWORD` por env.

## Criterios de done

Stack arranca y `/health` 200; costos dentro de Free Tier.

## Límites/trampas

No dejar ALB/RDS corriendo; `docker compose down -v` exige reseed.

## Referencias

`09-04`, `MemoriaAWS.md`.
