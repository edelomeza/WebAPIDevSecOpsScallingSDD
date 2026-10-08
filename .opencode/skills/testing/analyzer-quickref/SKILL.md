---
name: analyzer-quickref
description: Tabla regla→síntoma→fix de analizadores Sonar/xUnit que rompieron builds en 03-01…03-15
---

## Propósito

Lookup en 1 línea por regla durante cada slice (el porqué vive en `07-static-analysis`).

## Tabla

| Regla | Síntoma | Fix aplicado |
|---|---|---|
| S6964 | DTO input con `int/decimal` no-nullable sin `required` (under-posting) | `required` en valores de DTOs de input |
| CA2227 + CA1002 | Colección mutable expuesta en DTO | `IReadOnlyList<T>` con setter |
| S3267 | Bucle que solo valida (`ContainsKey`+`throw`) | Fusionar validación+cómputo en un `foreach` con `TryGetValue` |
| CA2007 / xUnit1030 | `ConfigureAwait(false)`; `new` inline en `await using` dentro de `[Fact]` | Solo en helpers/servicios; PROHIBIDO en cuerpos `[Fact]`; `await using` con `new` inline → extraer helper `CreateThrowingContext` |
| CA1305 | `int.ToString()` / fechas en llaves | Interpolar `$"{id}"`, fechas `"o"` |
| CA1308 | `ToLowerInvariant` | Pasar casing explícito / comparación ordinal |
| CA1861 | `new[]{...}` en asserts | `Assert.Single` + índice |
| xUnit2013 | `Assert.Equal` para tamaño de colección | `Assert.Single` / `Assert.Equal` sobre `.Count` |
| S101 | Tipo `*2fa*` minúsculas | `2Fa` en nombres de tipos |
| S1135 | `TODO` rompe build | `NOTE (XX-YY)` con spec dueña |
| S1118 / CA1515 | `Program` parcial para WebApplicationFactory | Pragmas `disable/restore` documentados |
| CA2000 | `WebApplicationFactory` en test | Pragma `disable` con comentario dispose-wrapper |
| AV0029 / AV0030 | `AddOpenApi/WithDocumentPerVersion` inexistentes v10.2.1 | `NoWarn` en csproj API |
| S1541 / S3776 | Complejidad >10/15 | Extraer método; controller delega en servicio |
| S3241 | Helper que retorna valor que ningún caller usa | Cambiar retorno a `void` |

## Límites/trampas

No suprimir reglas sin evidencia; no crear alias que oculten warnings.

## Referencias

`07-static-analysis`, `Directory.Build.props`, `Memoria.md` (`03-01`…`03-15`).
