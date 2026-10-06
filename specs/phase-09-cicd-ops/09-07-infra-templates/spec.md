# 09-07 — Plantillas de infra

## Contexto
Plantillas de infraestructura como referencia versionada. **Depende de**: `09-02`.

## Requisitos
1. `Dockerfile`, `deploy/docker-compose.*.yml`, `.semgrep/semgrep.yaml`, `.editorconfig`, `sonar-project.properties.example` en verde.

## Diseño
- Plantillas canónicas reutilizables por entornos.

## Contratos
- N/A.

## Tests
- N/A (verificación en CI: Docker/Semgrep/SonarCloud green).

## Criterios
- Docker/Semgrep/SonarCloud green.

## Límites
- Ejemplo SonarCloud fuera de Git si lleva secretos.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
