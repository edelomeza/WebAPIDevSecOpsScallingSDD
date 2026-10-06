# 04-04 — Matriz rate-limit/auth

## Contexto
Matriz explícita endpoint ↔ auth/rate-limit. **Depende de**: `04-02`, `04-03`.

## Requisitos
1. Implementar policies: Login 5/5min, Login2faVerify 10/5min, Global 1000/min, Admin 200/min, ConcurrentWrites 10.
2. Cada endpoint con auth y policy explícitas.

## Diseño
- `UseRateLimiter` con las policies; catálogo de referencia en `03-17`.

## Contratos
| Endpoint | Auth | Rate limit policy |
|---|---|---|
| POST /api/v1/auth/login | Anónimo | Login 5/5min |
| POST /api/v1/auth/login2fa/verify | Anónimo | Login2faVerify 10/5min |
| POST /api/v1/auth/refresh | Anónimo | Global 1000/min |
| POST /api/v1/auth/logout | Bearer | Global 1000/min |
| /api/v1/two-factor/* | Bearer | Global 1000/min |
| CRUD clientes/empleados/productos/estados-venta/usuarios | Bearer AdminOnly + AdminPolicy | Admin 200/min |
| POST /api/v1/ventas, /detalles, DELETE detalles | Bearer | ConcurrentWrites 10 |
| /api/v1/ventas/pedido, /pago, /factura, /dashboard | Bearer AdminOnly + AdminPolicy | Admin 200/min |

## Tests
- `SecurityTest/RateLimit/` (429 ante exceso).

## Criterios
- Cada endpoint tiene política explícita.

## Límites
- Relajación de límites solo vía env `PERF_*` en perf (no en prod).

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
