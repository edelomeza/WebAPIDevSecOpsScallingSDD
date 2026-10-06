# 07-01 — Análisis estático

## Contexto
Análisis estático y límites de complejidad en build. **Depende de**: `01-01`.

## Requisitos
1. Semgrep (`semgrep ci --config=auto --config=.semgrep/semgrep.yaml --error`) sin hallazgos nuevos.
2. SonarAnalyzer vía `Directory.Build.props` sin violaciones.
3. Complejidad ciclomática ≤10 por método / ≤15 por clase.

## Diseño
- Reglas custom en `.semgrep/semgrep.yaml`; `.editorconfig` para reglas CA.

## Contratos
- N/A.

## Tests
- N/A (gates de CI; evidencia en fase 09).

## Criterios
- Semgrep sin hallazgos nuevos; SonarAnalyzer sin violaciones S.

## Límites
- Solo análisis estático (calidad de tests en `07-02`).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
