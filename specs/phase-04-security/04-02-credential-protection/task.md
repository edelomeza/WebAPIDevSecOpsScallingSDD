# 04-02 — Credential Protection

## T1 — Protección de credenciales
- **Crear**: `PasswordHasherService.cs`, DTOs.
- **Verificar**: Argon2id/BCrypt; fake hash; lockout 5→15min; policies documentadas; password no en cache/logs.
