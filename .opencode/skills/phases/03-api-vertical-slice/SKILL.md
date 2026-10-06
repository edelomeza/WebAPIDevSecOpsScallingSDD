---
name: 03-api-vertical-slice
description: Patrón por endpoint (entidad→DTO→validation→controller→service→tests)
---

## Propósito

Patrón por endpoint (entidad→DTO→validation→controller→service→tests).

## Cuándo usarla

Cada spec `03-0x` de catálogo/entidad.

## Pasos

Seguir plantilla de vertical slice; auth `AdminOnly`+`AdminPolicy`.

## Checklist

CRUD completo, PagedResult, FluentValidation, cache-aside, tests 3 suites.

## Criterios de done

Endpoint responde con códigos correctos; rate limit aplicado.

## Límites/trampas

No exponer entity directo; no cachear password.

## Referencias

`03-01`…`03-05`, `03-00`, `03-16`.
