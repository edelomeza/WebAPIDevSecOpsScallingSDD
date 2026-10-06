# 07-03 — Cadena de suministro

## Contexto
Seguridad de la cadena de suministro y endurecimiento de imágenes. **Depende de**: `09-01`.

## Requisitos
1. CI con Trivy + dockle (falla en HIGH/CRITICAL), ZAP en PR y main, RESTler (`fuzzing/`).
2. `scripts/hardening_summary.py` genera `hardening/README.md`.

## Diseño
- Reporte de hardening versionado como evidencia.

## Contratos
- N/A.

## Tests
- N/A (gates de CI; evidencia en fase 09).

## Criterios
- Sin HIGH/CRITICAL; ZAP sin alertas nuevas.

## Límites
- Solo escaneo (remediación en PRs dedicados).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
