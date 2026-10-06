# plan.md — 09-07-infra-templates

1. Mapeo

- Modificar: Dockerfile, deploy/docker-compose.*.yml, .semgrep/semgrep.yaml, .editorconfig, sonar-project.properties.example.

2. Guardarraíles

- Non-root; no secretos; .dockerignore/.gitignore defensivos.

3. Pruebas

- Docker/Semgrep/SonarCloud green.

4. Secuencia

1. Dockerfile.
2. Compose.
3. Semgrep.
4. Editorconfig.
5. Sonar properties.
