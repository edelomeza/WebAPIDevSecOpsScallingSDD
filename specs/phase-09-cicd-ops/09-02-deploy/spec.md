# 09-02 — Despliegue

## Contexto
Empaquetado Docker y despliegue CloudFormation. **Depende de**: `09-01`.

## Requisitos
1. `Dockerfile` multi-stage reproducible y seguro.
2. `deploy/docker-compose.local.yml`, `deploy/docker-compose.aws.yml`, `deploy/aws/cloudformation.yml`.
3. Compose para local, AWS y chaos.

## Diseño
- Imagen sin shell/curl garantizado; verificación de herramientas antes de sondear.

## Contratos
- N/A.

## Tests
- N/A (verificación de imagen en CI).

## Criterios
- Imagen multi-stage; compose local/aws/chaos.

## Límites
- AWS Free Tier temporal con destrucción programada (ver `09-04`).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
