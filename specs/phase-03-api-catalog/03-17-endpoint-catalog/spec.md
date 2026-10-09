# 03-17 — Catálogo de endpoints

## Contexto
Catálogo consolidado y vivo de todos los endpoints de la API. **Depende de**: `03-00`, `03-16`.

## Requisitos
1. Mantener la tabla método/ruta/auth/rate limit/request DTO/response/códigos.
2. Actualizarla en cada slice que añada o cambie endpoints.

## Diseño
- Documento de referencia; sin código.

## Contratos

Tabla canónica: `docs/endpoints.md` (56 filas a 09-Oct-2026: 7 auth/misc + 1 ping
raíz + 3 sondas `probe` solo no-prod + 31 CRUD/search/autocomplete + 14 ventas/saga).
Este spec no duplica la tabla: la verifica vía `scripts/check_endpoints.ps1`.

Correcciones aplicadas sobre la tabla anterior (obsoleta):
1. `AdminOnly+AdminPolicy` → `AdminPolicy` (`AdminOnly` no existe en código).
2. `TwoFactorSetupRequest` eliminado: `POST /two-factor/setup` no lleva DTO
   (`Setup(CancellationToken)`; solo existe `TwoFactorSetupResponse`).
3. `UsuarioCreateDto/UsuarioDto` → `SegUsuario*` (nombres reales).
4. Añadidas las rutas que faltaban: `search`/`autocomplete` (clientes,
   empleados, productos, usuarios), `ventas/search`,
   `ventas/detalles/autocomplete-productos`, `GET detalles/{id}`,
   `GET pedido/{id:guid}`, `probe/*` (minimal APIs en `Program.cs`,
   gate `EnableProviderStates` + no-prod), `ping`.
5. Rate limit como `NOTE 04-04` (targets, no policies vigentes).
6. Códigos post-`03-16`: `ErrorResponse` uniforme, `422` FKs, `409`
   concurrencia/duplicado, `403` vía `ForbiddenAccessException`,
   pago-por-pedido sin 404 (lista vacía).

## Tests
- Revisión contra Pact/contratos en fase 10 (`ContractTest`).

## Criterios
- `docs/endpoints.md` lista las 56 filas (toda action de `Controllers/V1/` + `MapGet` de `Program.cs`: ping raíz + 3 `probe`).
- `scripts/check_endpoints.ps1` en verde (toda ruta del código tiene fila exacta método+ruta).
- Sin `AdminOnly`, sin `TwoFactorSetupRequest`, sin `Usuario*`: nombres reales.

## Límites
- Las policies de rate limit se implementan en `04-04`.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @usuario
- **Fecha:** 2026-10-09
- **Detalle:** ejecutado en `phase03.11` (opción B), mergeado PR #17 (`8a497a6`); firma sin cambios sobre la evidencia (tabla 56 filas + script verde).
