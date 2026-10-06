# 04-01 — JWT & Refresh

## T1 — Emisión y rotación
- **Crear**: `RefreshTokenService.cs`, DTOs.
- **Modificar**: `Program.cs` (JwtBearer options, blacklist Redis).
- **Verificar**: `alg=none` → 401; token reutilizado → 401; blacklist tras logout.
