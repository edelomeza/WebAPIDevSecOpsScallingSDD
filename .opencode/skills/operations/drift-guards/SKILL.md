---
name: drift-guards
description: Documentos canónicos + scripts extractores + jobs CI que impiden drift
---

## Propósito

Dueña del patrón anti-drift nacido en `03-17`: documento canónico + script
extractor que falla si falta fila + job CI paralelo. Sin guard, todo catálogo
se pudre (el spec `03-17` original lo demostró con 7 derivas).

## Cuándo usarla

Al crear un documento que refleja el código (endpoints, fixtures, matriz
auth); al añadir un guard nuevo (el de fixtures/Pact en fase 10 vive aquí).

## Pasos

1. Elegir fuente única (p. ej. `docs/endpoints.md`): el `spec.md` solo enlaza
   + registra correcciones, NUNCA duplica la tabla (ver
   `core/spec-first-writing`).
2. Escribir el extractor en PowerShell 5.1 sin dependencias (el job CI ya usa
   `pwsh`; ver `operations/powershell-quirks`): `[regex]::Matches` sobre el
   código (`[HttpX]`+`[Route]`, `MapGet`), normalización cuidadosa
   (minúsculas fuera de `{placeholders}` para respetar `{idVenta:int}`),
   comparación por token exacto (`| VERBO | ruta |`).
3. El primer run debe cazar gaps reales o el guard no muerde (probado
   `check_endpoints.ps1`: 3 faltas — placeholders + `/ping` no inventariado
   → `OK (56 rutas)` tras fixes).
4. Job CI paralelo sin `needs`, mismo SHA checkout, `shell: pwsh`, `exit 0/1`
   (precedentes: `endpoints`, `contract`; ver `phases/09-ci-matrix`).
5. Registrar el guard en esta skill + entrada en `Memoria.md`.

## Checklist

- Documento canónico con conteo exacto; script verde local; job en CI;
  spec enlazando (sin duplicar).

## Criterios de done

`script exit 0` local + job verde en el PR.

## Límites/trampas

- No guards con dependencias nuevas (rompen jobs ligeros); no normalizar
  dentro de `{placeholders}`; no ámbito full-repo sin reclasificar deuda.

## Guards vigentes

- `scripts/check_endpoints.ps1` ↔ `docs/endpoints.md` (56 rutas, job `endpoints`, `03-17`).
- `docs/rate-limit-matrix.md` (49 filas + exclusiones ping/health/probe/provider-states, `04-04`): canónico enlazado desde `docs/endpoints.md` (columna = policy vigente) y desde `spec.md` sin duplicar; drift lo reporta `traceability-clerk` (sin extractor propio: la fuente es el atributo `[EnableRateLimiting]` + `RateLimitOptions.*PolicyName`).
- `docs/asvs-l2-checklist.md` (10 capítulos V1–V9+V14, 6×Cubierto + 4×Parcial con deuda 04-01/04-02/04-03, `04-05`): canónico doc-only enlazado desde `spec.md` sin duplicar; todo `Cubierto` exige evidencia archivo+test, sin cobertura ficticia.

## Referencias

`scripts/check_endpoints.ps1`, `docs/endpoints.md`, `phases/09-ci-matrix`,
`operations/powershell-quirks`, `operations/critic-guardrails`,
`specs/phase-03-api-catalog/03-17-endpoint-catalog/spec.md`.
