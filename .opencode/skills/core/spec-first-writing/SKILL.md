---
name: spec-first-writing
description: Redactar sub-specs verticales con criterios de aceptación testables
---

## Propósito

Redactar sub-specs verticales con criterios de aceptación testables.

## Cuándo usarla

Antes de implementar cualquier sub-spec; al refinar una spec existente.

## Precondiciones

Haber leído `00-01` y el orden de fases.

## Pasos

1. Definir contexto y actor (+ `**Depende de**: NN-NN`).
2. Requisitos funcionales numerados.
3. Diseño (entidad/DTOs/servicios/eventos; si hay fake-first, declararlo + `NOTE`s + skill `core/deferred-scope-fakes`).
4. Tests esperados con rutas reales (`UnitTest/X/`, `IntegrationTest/...`, `SecurityTest/...`).
5. Criterios de aceptación medibles (comando o test + umbrales con gate, p. ej. Stryker ≥80%; post-`03-16`: exigir `ErrorResponse`/`Detail` solo no-prod + `throw NotFound/Forbidden` + sin try/catch + `probe` gateadas + factories `Staging`; gate Fase 1: Fase A `slice-scaffolder` + `security-reviewer`/`operations/critic-guardrails` 4 bloqueantes).
6. Límites conocidos + `Desviaciones registradas` en el bloque de aprobación.
7. `task.md`/`plan.md` operativos; al cerrar, fusionar `Detalle` al `spec.md` principal.

## Checklist

Contiene Entidad/DTOs/Validación/Endpoint/Servicio/Eventos/Tests/Criterios/Límites.

## Criterios de done

Criterios son verificables (comando o test); sin ambigüedad.

## Límites/trampas

No escribir specs horizontales por capa; no omitir "Límites conocidos"; no duplicar tablas vivas en el spec (probado `03-17`: el canónico vive en `docs/endpoints.md` y el spec solo enlaza + registra correcciones; la duplicación se pudre y la cubre `operations/drift-guards`). Canónicos fase 04 con la misma regla: `docs/rate-limit-matrix.md` (49 filas, el spec `04-04` solo enlaza) y `docs/asvs-l2-checklist.md` (10 capítulos, el `Contratos` de `04-05` solo enlaza). Checklist Entidad/DTOs/…: pedir fila en tabla `01-04` para `Authentication:UseJwtBearer`/`PasswordHasher:`/`RateLimiting:*` y delta-sobre-slice-previo (precedente `04-01`: no recrea `03-08`, solo el swap JWT).
