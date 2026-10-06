# plan.md — 09-02-deploy

1. Mapeo

- Crear/Modificar: Dockerfile, deploy/docker-compose.*.yml, deploy/aws/cloudformation.yml.

2. Guardarraíles

- Imagen multi-stage; no root; ASPNETCORE_HTTP_PORTS.
- Sin secretos en imagen.

3. Pruebas

- Docker build verde.
- Dockle sin HIGH/CRITICAL.

4. Secuencia

1. Dockerfile.
2. Compose.
3. CloudFormation.
