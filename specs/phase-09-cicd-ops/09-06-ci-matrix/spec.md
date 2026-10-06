# 09-06 — Matriz CI

## Contexto
Matriz de jobs de CI por evento disparador. **Depende de**: `09-01`.

## Requisitos
1. Documento `docs/ci-matrix.md` con job×evento, `needs`, `if`, timeout y artifact.
2. Artifacts tolerantes a fallos.

## Diseño
- Referencia para `.github/workflows/`.

## Contratos
- N/A.

## Tests
- Validación YAML local (`python -c "import yaml; yaml.safe_load(...)"`).

## Criterios
- Cada job con needs, if, timeout, artifact.

## Límites
- N/A.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
