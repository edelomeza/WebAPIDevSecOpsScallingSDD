# 03-18 — Contratos JSON

## Contexto
Fixtures JSON reales por endpoint para contratos y Pact. **Depende de**: `03-00`.

## Requisitos
1. Proveer fixtures JSON capturados del wire real (no inventados) para `LoginResponse`, `RefreshResponse`, `PagedResult<CliClienteDto>`, `CliClienteAutocompleteDto`, `SegUsuarioDto`, `CliClienteDto`, `ProProductoDto`, `VenCatEstadoDto`, `VenVentaDto`, `PedidoResponseDto`, `PagoResponseDto`, `DashboardDto`, `ErrorResponse` y ping.
2. Cubrir `Login2FaVerifyResponse` en fase 10 (requiere TOTP enrolado; `ITotpService` es `OtpNetTotpService` real en `Program.cs`, sin fake que permita captura determinista hoy).
3. Nombres corregidos: `PagoResponseDto` (no `VenPedidoPagoResponseDto`), `Login2FaVerifyResponse` (S101, `2Fa`).

## Diseño
- Fuente única: `ContractTest/Fixtures/*.json` (14 archivos). `IntegrationTest/Common/` no existe y no se crea: los fixtures viven solo en `ContractTest`.
- Captura real: `FixtureCaptureTests` levanta la API con `WebApplicationFactory` (`Staging` + `ContractAuthHandler`, InMemory, sin Docker) y guarda el cuerpo tal cual sale del wire. Solo escribe con `CONTRACT_CAPTURE=1`; sin la variable, el flujo corre igual y valida status codes (cobertura viva sin ensuciar git).
- Convención de nombres medida (corrige el "PascalCase estricto" original): prefijos legacy en minúsculas (`str/int/dec/dte/bln` + mayúscula o dígito, p. ej. `strNombreCliente`, `bln2FAHabilitado`) + resto PascalCase (`RowVersion`, `TotalCount`) + `id` solo o con sufijo PascalCase (`idCliCliente`). Implementada en `IsConventional` (`JsonContractTests`); el primer run del test la descubrió (`id`, `strNombre`, `bln2FAHabilitado`, `idCliCliente` habrían fallado con PascalCase puro).
- Sin secretos en fixtures: test `NoSecretsLeaked` (`strpwd`/`2fasecreto`/`password`, insensible a mayúsculas).

## Contratos
- Fixtures: `ping`, `segusuario`, `cliente`, `producto`, `estado-venta`, `clientes-paged`, `clientes-autocomplete`, `login-response`, `refresh-response` (vía `IRefreshTokenService.CreateAsync` scoped, el login no devuelve refresh), `pedido`, `pago`, `venta`, `dashboard`, `error-404`.

## Tests
- `ContractTest`: `FixtureCaptureTests` (flujo 14 capturas + asserts de status) + `JsonContractTests` (existencia, convención de nombres, sin secretos). `xunit.runner.json` en serie (store InMemory compartido).
- PactNet y verificación provider contra proceso real quedan para fase 10 (sin paquete `Pact*` hoy; verificado con `grep`).

## Criterios
- `dotnet test ContractTest -c Release --no-build` 4/4 verde.
- 14/14 fixtures existen y pasan convención + sin secretos.
- `Login2FaVerifyResponse` y PactNet diferidos con dueño (fase 10), no como deuda oculta.

## Límites
- Verificación Pact contra proceso real en fase 10.
- `Login2FaVerifyResponse` sin fixture hasta enrolar TOTP real (fase 10).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario
- **Fecha:** 2026-10-09
- **Detalle:** ejecutado en `phase03.11` (fixtures + test, captura real, todo en `ContractTest`); firma sin cambios sobre la evidencia (4/4 + 14 fixtures; PactNet/`Login2FaVerifyResponse` → fase 10).
