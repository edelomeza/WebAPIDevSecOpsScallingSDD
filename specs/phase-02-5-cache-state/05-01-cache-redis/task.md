# 05-01 — Cache & Redis

## T1 — Cache-aside con fallback
- **Modificar**: `Program.cs`, `Services/CacheService.cs`.
- **Verificar**: fallback ≤500ms; `/health` 503; no cachear password.
