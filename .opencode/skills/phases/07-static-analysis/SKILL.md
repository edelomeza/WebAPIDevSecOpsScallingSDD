---
name: 07-static-analysis
description: Análisis estático con Semgrep, SonarAnalyzer y límites de complejidad
---

## Propósito

Análisis estático en build.

## Cuándo usarla

Fase 9.

## Precondiciones

`01-01` definido.

## Pasos

1. Añadir reglas Semgrep en `.semgrep/semgrep.yaml`.
2. Ajustar `Directory.Build.props` con SonarAnalyzer (`AnalysisMode=All`, `TreatWarningsAsErrors`).
3. Limitar complejidad ciclomática ≤10 (`S1541`) y cognitiva ≤15 (`S3776`).
4. Aplicar cheat-sheet de reglas que rompieron builds reales (`03-01`…`03-16`): ver tabla en `testing/analyzer-quickref` (incluye `S3041`, waiver `Secret` enrollment `03-09`, canon sin try/catch `03-16`).
5. Ejecutar `dotnet build` y CI.

## Checklist

Semgrep sin hallazgos nuevos; SonarAnalyzer sin violaciones S; `required` en valores input (S6964); colecciones `IReadOnlyList` (CA2227/CA1002); `NOTE (XX-YY)`, nunca `TODO` (S1135); sin try/catch en controllers (canon `03-16`).

## Criterios de done

Build Release pasa; CI verde en job de análisis.

## Límites/trampas

No suprimir reglas sin evidencia; no crear alias que oculten warnings.

## Referencias

`07-01`, `Directory.Build.props`, `.editorconfig`.
