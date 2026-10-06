# 03-08 — Refresh & Logout

## T1 — Rotación y blacklist
- **Crear**: `RefreshController`, `LogoutController`, `RefreshTokenService`.
- **Verificar**: refresh rota; token reutilizado → 401; logout revoca y blacklist `jti`.
