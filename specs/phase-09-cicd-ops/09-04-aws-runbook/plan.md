# plan.md — 09-04-aws-runbook

1. Mapeo

- Crear: deploy/aws/cloudformation.yml, scripts/deploy-*.sh, scripts/destroy-aws.sh.

2. Guardarraíles

- Orden ALB→EC2→SQS→migrar.
- SKIP_MIGRATION configurable.
- Reseed tras docker compose down -v.

3. Pruebas

- Runbook: stack arranca; /health 200.

4. Secuencia

1. CloudFormation.
2. Deploy scripts.
3. Destroy.
