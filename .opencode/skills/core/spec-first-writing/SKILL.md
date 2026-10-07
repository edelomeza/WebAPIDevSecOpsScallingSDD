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
5. Criterios de aceptación medibles (comando o test + umbrales con gate, p. ej. Stryker ≥80%).
6. Límites conocidos + `Desviaciones registradas` en el bloque de aprobación.
7. `task.md`/`plan.md` operativos; al cerrar, fusionar `Detalle` al `spec.md` principal.

## Checklist

Contiene Entidad/DTOs/Validación/Endpoint/Servicio/Eventos/Tests/Criterios/Límites.

## Criterios de done

Criterios son verificables (comando o test); sin ambigüedad.

## Límites/trampas

No escribir specs horizontales por capa; no omitir "Límites conocidos".
