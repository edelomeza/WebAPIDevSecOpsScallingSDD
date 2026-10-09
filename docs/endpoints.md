# Catálogo de endpoints (03-17, canónico)

Fuente única de verdad para método/ruta/auth/rate-limit/DTOs/códigos.
El `spec.md` de `03-17` solo enlaza aquí; no duplica la tabla.
Verificado por `scripts/check_endpoints.ps1` (job `endpoints` en CI).

Convenciones aplicadas en todo el catálogo (probadas `03-01`…`03-16`):

- Base `api/v1/...` (`api/v{version:apiVersion}/...` + `UrlSegmentApiVersionReader`).
- Errores uniformes post-`03-16`: `ErrorResponse { Error, Status, TraceId }`
  (`Detail` solo no-prod, omitido en prod). `400` es `ValidationProblem`
  (FluentValidation vía `ValidateAsync`) o `{ error }` manual en lecturas.
  `404` = `throw NotFoundException`; `403` = `throw ForbiddenAccessException`
  (`EnsureOwner`); `409` = `ConcurrencyConflictException`; `422` =
  `ValidationException` de FKs (único outlier: `EmpEmpleado` FK va a `422`
  desde el servicio en vez de `400`).
- Rate limit: TODO diferido a `04-04` (`NOTE (04-04)` por endpoint). La columna
  indica el target, no una policy vigente.
- `401` anónimo sin JWT; `403` con `AdminPolicy` sin rol `Admin`.
  (`AdminOnly` no existe en código: la policy real es solo `AdminPolicy`.)
- Paginación: `page`/`pageSize` sueltos (sin `QueryParams`).

## Auth (anónimo / Bearer)

| Método | Ruta | Auth | Rate limit (target `NOTE 04-04`) | Request DTO | Response | Códigos |
|---|---|---|---|---|---|---|
| POST | /api/v1/auth/login | Anónimo | Login 5/5min | LoginRequest | LoginResponse (`Token` o `Requires2fa`+`TempToken`) | 200/400/401/423 |
| POST | /api/v1/auth/login2fa/verify | Anónimo | Login2faVerify 10/5min | Login2FaVerifyRequest | Login2FaVerifyResponse (`Token`) | 200/400/401/423 |
| POST | /api/v1/auth/refresh | Anónimo | Global 1000/min | RefreshRequest | RefreshResponse (`Token`, `RefreshToken`) | 200/400/401 |
| POST | /api/v1/auth/logout | Bearer `[Authorize]` | Global 1000/min | LogoutRequest | — (204) | 204/400/401 |
| POST | /api/v1/two-factor/setup | Bearer `[Authorize]` | Global 1000/min | — (sin DTO: `Setup(CancellationToken)`) | TwoFactorSetupResponse | 200/401 |
| POST | /api/v1/two-factor/verify | Bearer `[Authorize]` | Global 1000/min | TwoFactorVerifyRequest | TwoFactorVerifyResponse (`Enabled=true`) | 200/400/401/423 |
| GET | /api/v1/ping | Anónimo (sin atributo) | — | — | `{ Status: "Pong", Version: "v1" }` (`PingResponse` local al controller) | 200 |
| GET | /ping | — (minimal API raíz, `Program.cs`) | — | — | `"pong"` (texto plano legacy) | 200 |

## Sondas `probe` (solo no-prod, gate `EnableProviderStates`)

Minimal APIs en `Program.cs` (no son controllers). Solo existen con
`EnableProviderStates=true` y fuera de `Production`.

| Método | Ruta | Auth | Request | Respuesta | Códigos |
|---|---|---|---|---|---|
| GET | /api/v1/probe/timeout | — | — | `throw TimeoutException` → 408 | 408 |
| GET | /api/v1/probe/error | — | — | `throw InvalidOperationException` → 500 | 500 |
| GET | /api/v1/probe/forbidden | — | — | `throw ForbiddenAccessException` → 403 | 403 |

## Clientes `api/v1/clientes` (`AdminPolicy`)

| Método | Ruta | Query / Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/clientes | `page=1`, `pageSize=20` | PagedResult\<CliClienteDto\> | 200/400/401/403 |
| GET | /api/v1/clientes/search | `texto?`, `page`, `pageSize` (texto requerido, ≤100) | PagedResult\<CliClienteDto\> | 200/400/401/403 |
| GET | /api/v1/clientes/autocomplete | `texto?`, `maxResultados=10` (requerido, ≤100) | IReadOnlyList\<CliClienteAutocompleteDto\> | 200/400/401/403 |
| GET | /api/v1/clientes/{id:int} | — | CliClienteDto | 200/401/403/404 |
| POST | /api/v1/clientes | CliClienteCreateDto | CliClienteDto | 201/400/401/403/409/422 |
| PUT | /api/v1/clientes/{id:int} | `id` ruta + CliClienteUpdateDto (id ruta=body) | CliClienteDto | 200/400/401/403/404/409/422 |
| DELETE | /api/v1/clientes/{id:int} | `id` ruta + CliClienteDeleteDto en body | — (204) | 204/400/401/403/404/409 |

## Empleados `api/v1/empleados` (`AdminPolicy`, sin autocomplete)

| Método | Ruta | Query / Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/empleados | `page`, `pageSize` | PagedResult\<EmpEmpleadoDto\> | 200/400/401/403 |
| GET | /api/v1/empleados/search | `texto?`, `idTipoEmpleado?`, `page`, `pageSize` (texto ≤50; `idTipoEmpleado>0`) | PagedResult\<EmpEmpleadoDto\> | 200/400/401/403 |
| GET | /api/v1/empleados/{id:int} | — | EmpEmpleadoDto | 200/401/403/404 |
| POST | /api/v1/empleados | EmpEmpleadoCreateDto | EmpEmpleadoDto | 201/400/401/403/409/422 |
| PUT | /api/v1/empleados/{id:int} | `id` ruta + EmpEmpleadoUpdateDto | EmpEmpleadoDto | 200/400/401/403/404/409/422 |
| DELETE | /api/v1/empleados/{id:int} | `id` ruta + EmpEmpleadoDeleteDto en body | — (204) | 204/400/401/403/404/409 |

## Productos `api/v1/productos` (`AdminPolicy`)

| Método | Ruta | Query / Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/productos | `page`, `pageSize` | PagedResult\<ProProductoDto\> | 200/400/401/403 |
| GET | /api/v1/productos/search | `texto?`, `page`, `pageSize` (requerido, ≤50) | PagedResult\<ProProductoDto\> | 200/400/401/403 |
| GET | /api/v1/productos/{id:int} | — | ProProductoDto | 200/401/403/404 |
| POST | /api/v1/productos | ProProductoCreateDto | ProProductoDto | 201/400/401/403/409/422 |
| PUT | /api/v1/productos/{id:int} | `id` ruta + ProProductoUpdateDto | ProProductoDto | 200/400/401/403/404/409/422 |
| DELETE | /api/v1/productos/{id:int} | `id` ruta + ProProductoDeleteDto en body | — (204) | 204/400/401/403/404/409 |

## Estados de venta `api/v1/estados-venta` (`AdminPolicy`, sin search)

| Método | Ruta | Query / Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/estados-venta | `page`, `pageSize` | PagedResult\<VenCatEstadoDto\> | 200/400/401/403 |
| GET | /api/v1/estados-venta/{id:int} | — | VenCatEstadoDto | 200/401/403/404 |
| POST | /api/v1/estados-venta | VenCatEstadoCreateDto | VenCatEstadoDto | 201/400/401/403/409/422 |
| PUT | /api/v1/estados-venta/{id:int} | `id` ruta + VenCatEstadoUpdateDto | VenCatEstadoDto | 200/400/401/403/404/409/422 |
| DELETE | /api/v1/estados-venta/{id:int} | `id` ruta + VenCatEstadoDeleteDto en body | — (204) | 204/400/401/403/404/409 |

## Usuarios `api/v1/usuarios` (`AdminPolicy`, DTOs `SegUsuario*`)

| Método | Ruta | Query / Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/usuarios | `page`, `pageSize` | PagedResult\<SegUsuarioDto\> | 200/400/401/403 |
| GET | /api/v1/usuarios/search | `texto?`, `page`, `pageSize` (requerido, ≤50; solo `strNombre`, anti-enumeración) | PagedResult\<SegUsuarioDto\> | 200/400/401/403 |
| GET | /api/v1/usuarios/autocomplete | `texto?`, `maxResultados=10` | IReadOnlyList\<SegUsuarioAutocompleteDto\> | 200/400/401/403 |
| GET | /api/v1/usuarios/{id:int} | — | SegUsuarioDto (sin secretos) | 200/401/403/404 |
| POST | /api/v1/usuarios | SegUsuarioCreateDto | SegUsuarioDto | 201/400/401/403/409/422 |
| PUT | /api/v1/usuarios/{id:int} | `id` ruta + SegUsuarioUpdateDto (nunca toca `strPWD`) | SegUsuarioDto | 200/400/401/403/404/409/422 |
| DELETE | /api/v1/usuarios/{id:int} | `id` ruta + SegUsuarioDeleteDto en body | — (204) | 204/400/401/403/404/409 |

## Venta legacy `api/v1/ventas` (Bearer, stock en la misma Tx)

| Método | Ruta | Query / Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/ventas/search | `strClaveVenta?` (≤10), `strNombreCliente?` (≤100), `dteFechaInicio?`, `dteFechaFin?` (`inicio>fin`→400), `page`, `pageSize` | PagedResult\<VenVentaDto\> | 200/400/401 |
| GET | /api/v1/ventas/{id:int} | — | VenVentaDto | 200/401/404 |
| POST | /api/v1/ventas | VenVentaCreateDto | VenVentaDto | 201/400/401/409/422 |

## Venta detalle (Bearer; base `api/v1/ventas/detalles`)

| Método | Ruta | Query / Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/ventas/detalles/autocomplete-productos | `texto?` (requerido, ≤50), `maxResultados=10` | IReadOnlyList\<ProProductoAutocompleteDto\> | 200/400/401 |
| GET | /api/v1/ventas/detalles/{id:int} | — | VenVentaDetalleDto | 200/401/404 |
| POST | /api/v1/ventas/{idVenta:int}/detalles | `idVenta` ruta + VenVentaDetalleCreateDto (ruta absoluta `~/...`, ownership 403 si ajeno) | VenVentaDetalleDto | 201/400/401/403/404/409 |
| DELETE | /api/v1/ventas/detalles/{id:int} | `id` ruta + VenVentaDetalleDeleteDto en body (restaura stock) | — (204) | 204/400/401/403/404/409 |

## Pedido saga `api/v1/ventas/pedido` (`AdminPolicy`, sin descuento de stock)

| Método | Ruta | Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/ventas/pedido/{id:guid} | — | PedidoResponseDto | 200/401/403/404 |
| POST | /api/v1/ventas/pedido | PedidoCreateDto | PedidoResponseDto | 201/400/401/403/409/422 |

## Pago saga `api/v1/ventas/pago` (`AdminPolicy`)

| Método | Ruta | Body | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/ventas/pago/pedido/{pedidoId:guid} | — | IReadOnlyList\<PagoResponseDto\> (vacía si no hay; sin 404) | 200/401/403 |
| GET | /api/v1/ventas/pago/{id:int} | — | PagoResponseDto | 200/401/403/404 |
| POST | /api/v1/ventas/pago | PagoCreateDto | PagoResponseDto | 201/400/401/403/409/422 |

## Factura saga `api/v1/ventas/factura` (`AdminPolicy`, solo lectura)

| Método | Ruta | Response | Códigos |
|---|---|---|---|
| GET | /api/v1/ventas/factura/{id:int} | VenPedidoFacturaResponseDto | 200/401/403/404 |

## Dashboard saga `api/v1/ventas/dashboard` (`AdminPolicy`)

| Método | Ruta | Query | Response | Códigos |
|---|---|---|---|---|
| GET | /api/v1/ventas/dashboard | `desde?`, `hasta?`, `estadoSaga?` (`DashboardFilterDto`; `Hasta>=Desde`) | DashboardDto | 200/400/401/403 |

Total: 56 filas (7 auth/misc + 1 ping raíz + 3 probe + 31 catálogo CRUD/search + 14 ventas/saga).
