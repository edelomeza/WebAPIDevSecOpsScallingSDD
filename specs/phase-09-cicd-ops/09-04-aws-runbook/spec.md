# 09-04 — Runbook AWS

## Contexto
Despliegue temporal en AWS Free Tier con runbook paso a paso. **Depende de**: `09-02`.

## Requisitos
1. Scripts `scripts/deploy-aws.sh` y `scripts/destroy-aws.sh` + `deploy/aws/cloudformation.yml`.
2. Orden: ALB → EC2 → SQS → migrar; `SKIP_MIGRATION`; reseed.
3. Destruir/recrear para control de costos (cron schedule-deploy/destroy).

## Diseño
- Stack temporal: ALB 80/443, EC2, RDS, SQS + DLQ.

## Contratos
- Env: `StackName`, `PORT`, `DB_USER`/`DB_PASSWORD` (ver 01-04).

## Tests
- Smoke `/health` 200 tras despliegue.

## Criterios
- Stack arranca; /health 200.

## Límites
- Free Tier temporal; secretos solo por env/CI.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
