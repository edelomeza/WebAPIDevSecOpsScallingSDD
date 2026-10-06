# 09-04 — AWS Runbook

## T1 — Runbook AWS
- **Crear**: `deploy/aws/cloudformation.yml`, `scripts/deploy-*.sh`, `scripts/destroy-aws.sh`.
- **Verificar**: ALB→EC2→SQS→migrar; `SKIP_MIGRATION`; reseed tras `down -v`.
