# 03-17 — Catálogo de endpoints

## Contexto
Catálogo consolidado y vivo de todos los endpoints de la API. **Depende de**: `03-00`, `03-16`.

## Requisitos
1. Mantener la tabla método/ruta/auth/rate limit/request DTO/response/códigos.
2. Actualizarla en cada slice que añada o cambie endpoints.

## Diseño
- Documento de referencia; sin código.

## Contratos
| Método | Ruta | Auth | Rate limit | Request DTO | Response | Códigos |
|---|---|---|---|---|---|---|
| POST | /api/v1/auth/login | Anónimo | Login 5/5min | LoginRequest | LoginResponse | 200/401/423/429 |
| POST | /api/v1/auth/login2fa/verify | Anónimo | Login2faVerify 10/5min | Login2faVerifyRequest | Login2faVerifyResponse | 200/401/429 |
| POST | /api/v1/auth/refresh | Anónimo | Global 1000/min | RefreshRequest | RefreshResponse | 200/401 |
| POST | /api/v1/auth/logout | Bearer | Global 1000/min | LogoutRequest | Empty | 204/401 |
| POST | /api/v1/two-factor/setup | Bearer | Global 1000/min | TwoFactorSetupRequest | TwoFactorSetupResponse | 200/401 |
| POST | /api/v1/two-factor/verify | Bearer | Global 1000/min | TwoFactorVerifyRequest | TwoFactorVerifyResponse | 200/401 |
| GET/POST | /api/v1/clientes | AdminOnly+AdminPolicy | Admin 200/min | CliClienteCreateDto | PagedResult<CliClienteDto> | 200/201/400/401/403/429 |
| PUT/DELETE | /api/v1/clientes/{id} | AdminOnly+AdminPolicy | Admin 200/min | CliClienteUpdateDto/DeleteDto | CliClienteDto/204 | 204/400/401/403/404/409 |
| GET/POST | /api/v1/empleados | AdminOnly+AdminPolicy | Admin 200/min | EmpEmpleadoCreateDto | PagedResult<EmpEmpleadoDto> | 200/201/400/401/403/429 |
| PUT/DELETE | /api/v1/empleados/{id} | AdminOnly+AdminPolicy | Admin 200/min | EmpEmpleadoUpdateDto/DeleteDto | EmpEmpleadoDto/204 | 204/400/401/403/404/409 |
| GET/POST | /api/v1/productos | AdminOnly+AdminPolicy | Admin 200/min | ProProductoCreateDto | PagedResult<ProProductoDto> | 200/201/400/401/403/429 |
| PUT/DELETE | /api/v1/productos/{id} | AdminOnly+AdminPolicy | Admin 200/min | ProProductoUpdateDto/DeleteDto | ProProductoDto/204 | 204/400/401/403/404/409 |
| GET/POST | /api/v1/estados-venta | AdminOnly+AdminPolicy | Admin 200/min | VenCatEstadoCreateDto | PagedResult<VenCatEstadoDto> | 200/201/400/401/403/429 |
| PUT/DELETE | /api/v1/estados-venta/{id} | AdminOnly+AdminPolicy | Admin 200/min | VenCatEstadoUpdateDto/DeleteDto | VenCatEstadoDto/204 | 204/400/401/403/404/409 |
| GET/POST | /api/v1/usuarios | AdminOnly+AdminPolicy | Admin 200/min | UsuarioCreateDto | PagedResult<UsuarioDto> | 200/201/400/401/403/429 |
| PUT/DELETE | /api/v1/usuarios/{id} | AdminOnly+AdminPolicy | Admin 200/min | UsuarioUpdateDto/DeleteDto | UsuarioDto/204 | 204/400/401/403/404/409 |
| POST | /api/v1/ventas | Bearer | ConcurrentWrites 10 | VenVentaCreateDto | 201 Created | 201/400/401/409/422 |
| POST | /api/v1/ventas/{id}/detalles | Bearer | ConcurrentWrites 10 | VenVentaDetalleCreateDto | 201 Created | 201/400/401/403/404 |
| DELETE | /api/v1/ventas/detalles/{id} | Bearer | ConcurrentWrites 10 | — | 204 | 204/401/403/404 |
| POST | /api/v1/ventas/pedido | AdminOnly+AdminPolicy | Admin 200/min | PedidoCreateDto | PedidoResponseDto | 201/400/401/403 |
| GET | /api/v1/ventas/pago/{id} | AdminOnly+AdminPolicy | Admin 200/min | — | VenPedidoPagoResponseDto | 200/401/403/404 |
| GET | /api/v1/ventas/factura/{id} | AdminOnly+AdminPolicy | Admin 200/min | — | VenPedidoFacturaResponseDto | 200/401/403/404 |
| GET | /api/v1/ventas/dashboard | AdminOnly+AdminPolicy | Admin 200/min | — | DashboardDto | 200/401/403 |

## Tests
- Revisión contra Pact/contratos en fase 10 (`ContractTest`).

## Criterios
- Todos los endpoints listados.

## Límites
- Las policies de rate limit se implementan en `04-04`.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
