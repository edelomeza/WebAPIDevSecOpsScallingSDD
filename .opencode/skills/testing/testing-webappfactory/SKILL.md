---
name: testing-webappfactory
description: Integration/Security tests con WebApplicationFactory<Program>
---

## Propósito

Integration/Security con `WebApplicationFactory<Program>`.

## Pasos

`public partial class Program` (pragmas `S1118`/`CA1515`); `UseInMemoryDatabase=true`; `Reset()` de estáticos (`TokenBlacklist` estático cruza tests sin `Reset()`).
`TestAuthHandler` ("Test"): rol vía header `X-Test-Role`, usuario vía `X-Test-UserId` (sin header = comportamiento intacto); fábrica Admin con `ConfigureTestServices` + pragma `CA2000`.
Sin endpoint `POST` para una entidad: sembrar vía `factory.Services.CreateScope()` + `GetRequiredService<AppDbContext>()` (comparte el store InMemory del servidor; precedente `03-07` 2FA, aplicado `03-14`).

## Checklist

Key ≥32B; InMemory; `IntegrationTest/xunit.runner.json` en serie (`parallelizeTestCollections: false`).
Matriz por endpoint: `Admin→2xx`, `User→403` con `AdminPolicy`, `User→200` en Bearer sin policy (documentar como desviación espejo `03-10/03-11`), anónimo→401 sin fugas (`Assert.DoesNotContain` del payload).
Todo test que cree datos los borra al final (`DELETE` con `RowVersion` de la creación): el store InMemory tiene nombre fijo compartido por factories del proceso (`TotalCount==1` es frágil en paralelo). Datos con `Guid` único por test (pedidos/pagos/facturas sin endpoint `DELETE` acumulan filas: no asertar conteos exactos sobre ellas).

## Límites/trampas

TokenBlacklist estático cruza tests. Docker Desktop detenido → `DockerUnavailableException`: arrancar daemon antes del rerun (recurrencia `03-02/03-11/03-12/03-13`). Pedidos/entidades sin endpoint DELETE acumulan filas: no asertar conteos exactos sobre ellas.

## Referencias

`IntegrationTest`, `SecurityTest`.
