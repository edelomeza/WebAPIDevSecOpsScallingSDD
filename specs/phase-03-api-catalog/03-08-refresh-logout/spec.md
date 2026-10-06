# 03-08 — Refresh y logout

## Contexto
Rotación de refresh tokens y revocación con blacklist en logout. **Depende de**: `04-01`, `03-06`.

## Requisitos
1. `POST /api/v1/auth/refresh` con `RefreshRequest/RefreshResponse`; rota el token (emite nuevo, revoca el anterior vía `strReplacedByTokenHash`).
2. `POST /api/v1/auth/logout` (Bearer) con `LogoutRequest`; revoca y registra `jti` en blacklist.
3. Validadores `RefreshRequestValidator`, `LogoutRequestValidator`.

## Diseño
- Refresh tokens hasheados en `SegRefreshToken`; blacklist `blacklist:{jti}` en caché/Redis.

## Contratos
- Refresh → 200/401; logout → 204/401.

## Tests
- `UnitTest/RefreshToken/`, `IntegrationTest/Refresh/`, `SecurityTest/Refresh/`.

## Criterios
- Refresh rota; logout revoca y blacklist `jti`.

## Límites
- Emisión JWT en `04-01`.

## Aprobación y Control de Cambios
- **Estado:** 🚧 Borrador
- **Revisores:** —
- **Fecha:** —
- **Detalle:** pendiente de ejecución.
