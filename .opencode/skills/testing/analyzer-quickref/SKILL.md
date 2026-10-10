---
name: analyzer-quickref
description: Tabla regla→síntoma→fix de analizadores Sonar/xUnit que rompieron builds en 03-01…04-04
---

## Propósito

Lookup en 1 línea por regla durante cada slice (el porqué vive en `07-static-analysis`).

## Tabla

| Regla | Síntoma | Fix aplicado |
|---|---|---|
| S6964 | DTO input con `int/decimal` no-nullable sin `required` (under-posting) | `required` en valores de DTOs de input |
| CA2227 + CA1002 | Colección mutable expuesta en DTO | `IReadOnlyList<T>` con setter |
| S3267 | Bucle que solo valida (`ContainsKey`+`throw`) | Fusionar validación+cómputo en un `foreach` con `TryGetValue` |
| CA2007 / xUnit1030 | `ConfigureAwait(false)`; `new` inline en `await using` dentro de `[Fact]` | Solo en helpers/servicios; PROHIBIDO en cuerpos `[Fact]`; `await using` con `new` inline → extraer helper `CreateThrowingContext`; en `ContractTest` el flujo de captura llama helpers sin `ConfigureAwait` en el `[Fact]` |
| CA1305 | `int.ToString()` / fechas en llaves | Interpolar `$"{id}"`, fechas `"o"` |
| CA1308 | `ToLowerInvariant` | Pasar casing explícito / comparación ordinal |
| CA1861 | `new[]{...}` en asserts | `Assert.Single` + índice |
| xUnit2013 | `Assert.Equal` para tamaño de colección | `Assert.Single` / `Assert.Equal` sobre `.Count` |
| S101 | Tipo `*2fa*` minúsculas; clase `Argon2id` con d minúscula | `2Fa` en nombres de tipos; `Argon2Id` con D mayúscula (`04-02`) |
| S1135 | `TODO` rompe build | `NOTE (XX-YY)` con spec dueña; caza también `todo` en minúsculas dentro de comentarios (redecir "cada hash…", `04-02`) |
| S1118 / CA1515 | `Program` parcial para WebApplicationFactory; tipos públicos en proyecto de tests (`ContractAuthHandler`) | Pragmas `disable/restore` documentados; `NoWarn CA1515` en csproj del proyecto de tests (precedente `UnitTest`, aplicado `ContractTest` `03-18`) |
| CA2000 | `WebApplicationFactory` en test | Pragma `disable` con comentario dispose-wrapper; `new WAF().WithWebHostBuilder(...)` inline → pragma como en `CreateAdminFactory` (`04-04`) |
| AV0029 / AV0030 | `AddOpenApi/WithDocumentPerVersion` inexistentes v10.2.1 | `NoWarn` en csproj API |
| S1541 / S3776 | Complejidad >10/15 | Extraer método; controller delega en servicio |
| S3241 | Helper que retorna valor que ningún caller usa | Cambiar retorno a `void` (`InsertFactura→void`, `03-15`) |
| S3041 | `StringBuilder` con un solo `Append` en tests | Simplificar a concatenación/interpolación |
| critic `Secret` | Secreto TOTP en DTO de enrollment (`TwoFactorDtos.cs:5`) | Waiver documentado: 1 vez por spec `03-09`, jamás logs/caché/DB en claro + tests `DoesNotContain` |
| canon `03-16` | try/catch en controllers | Único try/catch en `ExceptionHandlingMiddleware` (ver `phases/03-errors-middleware`) |
| `ContractTest` 03-18 | Handler/refresh en captura | `ContractAuthHandler` propio (no referenciar `IntegrationTest`); refresh vía `IRefreshTokenService.CreateAsync` scoped (el login no devuelve refresh) |
| CA1865 / CA1845 | `EndsWith(string)` / `string.Concat` con spans en JWT (`04-01`) | `EndsWith(char)` + `string.Concat/AsSpan` |
| CS8601 | Indexer `IConfiguration` nullable a parámetro no-nullable (`04-01`) | Local + ternaria |
| CA1707 | Migración con `_` (`04-02`) | Renombrar a `SegBloqueo0402` |
| S3358 | Ternaria anidada en helper de test (`04-03`) | Extraer `GetHeader`; `HstsOptions` vive en `Microsoft.AspNetCore.HttpsPolicy` (no `Builder`); HSTS por `AddHsts` (`UseHsts(Action)` no existe) |

## Límites/trampas

No suprimir reglas sin evidencia; no crear alias que oculten warnings.

## Referencias

`07-static-analysis`, `Directory.Build.props`, `Memoria.md` (`03-01`…`04-04`).
