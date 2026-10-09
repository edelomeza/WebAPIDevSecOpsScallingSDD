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
Post-`03-16` (canon en `phases/03-errors-middleware`): `10` factories a `Staging` (en `Dev` la página de excepciones devolvería HTML); `Production` exige override `UseInMemoryDatabase=true`; sondas `GET /api/v1/probe/{timeout,error,forbidden}` gateadas `EnableProviderStates`+no-prod (`Errors 8/8`). `03-09`: stubs controlables en unit vs TOTP real solo en integración (`TwoFactor 6/6 + Login2Fa 4/4`). `03-15`: asserts `>=1` en dashboard por store compartido (no `TotalCount==1`).

## Checklist

Key ≥32B; InMemory; `IntegrationTest/xunit.runner.json` en serie (`parallelizeTestCollections: false`).
Matriz por endpoint: `Admin→2xx`, `User→403` con `AdminPolicy`, `User→200` en Bearer sin policy (documentar como desviación espejo `03-10/03-11`), anónimo→401 sin fugas (`Assert.DoesNotContain` del payload).
Todo test que cree datos los borra al final (`DELETE` con `RowVersion` de la creación): el store InMemory tiene nombre fijo compartido por factories del proceso (`TotalCount==1` es frágil en paralelo). Datos con `Guid` único por test (pedidos/pagos/facturas sin endpoint `DELETE` acumulan filas: no asertar conteos exactos sobre ellas).

## Límites/trampas

TokenBlacklist estático cruza tests. Docker Desktop detenido → `DockerUnavailableException`: arrancar daemon antes del rerun (recurrencia `03-02/03-11/03-12/03-13`). Pedidos/entidades sin endpoint DELETE acumulan filas: no asertar conteos exactos sobre ellas.

## Referencias

`IntegrationTest`, `SecurityTest`, `phases/03-errors-middleware`, `phases/04-totp-provisioning`.
