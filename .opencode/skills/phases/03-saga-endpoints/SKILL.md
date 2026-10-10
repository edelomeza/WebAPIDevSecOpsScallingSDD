---
name: 03-saga-endpoints
description: Endpoints saga: pedido, pago, factura, dashboard
---

## Propósito

Endpoints saga: pedido, pago, factura, dashboard.

## Cuándo usarla

Al crear `03-12`, `03-13`, `03-14`, `03-15`.

## Precondiciones

`03-12…03-15` pueden ejecutarse en modo mínimo fake-first SIN `06-02/06-03/06-04/04-04` previos (vía `NOTE`s + skill `core/deferred-scope-fakes`); el bus real llega en fase 06.
`VenPedidoPago`/`VenPedidoFactura` ya existen desde fase 02 (no recrear; seed `F-SEED-1`); el catálogo `03-17` puede ya coincidir (verificar antes de tocar).

## Pasos

1. `VenPedido` con estado temporal (`"Creado"` + `NOTE 06-04`) y publicación `PedidoCreadoEvent` vía `IPedidoEventPublisher` fake (`NOTE 06-01`); el pedido NO descuenta stock (lo valida el consumer en `06-02`).
2. `VenPedidoPago` con `strIdTransaccion` único filtrado (`IS NOT NULL` admite múltiples `NULL`, probado `NullTransaccionAllowsDuplicates`); opcionales con `Trim` + vacío→`null`; duplicado no-nulo → pre-chequeo + `DbUpdateException→ConcurrencyConflictException→409` (InMemory NO impone índices únicos; el `catch` es la red para SQL real).
3. `VenPedidoFactura`: folio `F-{año}-{seq}` desde Redis SOLO si hay `Increment` atómico (hoy `CacheService` solo expone `Get/Set/Remove` + TTL máx 120s → diferir a fase 06, probado `03-14`). Matriz decidida: (a) GET-only mínimo (`GetByIdAsync`, sin `VersionKey`/`InvalidateAsync`, desbloquea `03-15`); (b) GET+POST con folio fake `Get+Set` + `UNIQUE` como guardián→409; (c) extender `ICacheService.IncrementAsync` (rompe interfaz compartida).
4. Dashboard con métricas de saga y depth de cola (probado `03-15`): agregados + filtros sueltos `desde/hasta/estadoSaga`; cola fake `0` + `NOTE 06-01`; caché `cache:dashboard:*` TTL 60s sin `VersionKey`; sanitizar `password/secret/token/":"` en la llave; fechas como `Ticks` invariante.
5. Proteger con `AdminPolicy` + `[EnableRateLimiting]` explícito (`04-04` vigente: `Admin` en clase saga, `Venta`/`VentaDetalle` clase `Global` + override `ConcurrentWrites` en POST/DELETE; fila en `docs/rate-limit-matrix.md`).
6. Post-`03-16` (canon en `phases/03-errors-middleware`): sin try/catch en controllers saga, `throw NotFound/Forbidden` + `ErrorResponse` uniforme, factories en `Staging`, sondas `probe` gateadas; gate Fase 1 vía `operations/critic-guardrails`.

## Detalle por endpoint (espejo resumido; canónico arriba)

1. `VenPedido` con estados y publicación `PedidoCreadoEvent`.
2. `VenPedidoPago` con `strIdTransaccion` único filtrado (`IS NOT NULL` admite múltiples `NULL`, probado `NullTransaccionAllowsDuplicates`); opcionales con `Trim` + vacío→`null`; duplicado no-nulo → pre-chequeo + `DbUpdateException→ConcurrencyConflictException→409` (InMemory NO impone índices únicos; el `catch` es la red para SQL real).
3. `VenPedidoFactura`: folio `F-{año}-{seq}` desde Redis SOLO si hay `Increment` atómico (hoy `CacheService` solo expone `Get/Set/Remove` + TTL máx 120s → diferir a fase 06, probado `03-14`). Matriz decidida: (a) GET-only mínimo (`GetByIdAsync`, sin `VersionKey`/`InvalidateAsync`, desbloquea `03-15`); (b) GET+POST con folio fake `Get+Set` + `UNIQUE` como guardián→409; (c) extender `ICacheService.IncrementAsync` (rompe interfaz compartida).
4. Dashboard (ver paso 4 arriba).
5. Proteger con `AdminPolicy` (es la policy real; `AdminOnly` no existe en código).

## Checklist

Endpoints protegidos; estados correctos; folio desde Redis o diferido con `NOTE 06-02/06-04` y matriz (a)/(b)/(c) registrada; slice GET-only válido sin `VersionKey`.

## Criterios de done

Pedido→pago→factura con estados correctos; dashboard responde.

## Límites/trampas

No exponer saga sin auth; no olvidar DLQ/compensación.

## Referencias

`03-12…03-15`, `03-17`, `06-02`, `06-04`, `05-01` (límites `CacheService`), `phases/03-errors-middleware`, `operations/critic-guardrails`, `core/auth-matrix`, `docs/rate-limit-matrix.md`.
