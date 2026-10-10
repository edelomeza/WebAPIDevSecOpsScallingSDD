# 01-04 — Referencia de configuración

## Contexto
Referencia exhaustiva de configuración de la API. **Depende de**: `01-01`.

## Requisitos
1. Tabla canónica de claves con default y estado por fase.
2. Cada clave del JSON de ejemplo corresponde a código real.
3. Las claves futuras solo se documentan en esta tabla (no en `appsettings.Example.json`).
4. No se commitean secretos.

## Diseño

| Clave / env var | Default | Estado / Fase | Descripción |
|---|---|---|---|
| `ConnectionStrings:Default` | — | [Core / Implementado] | SQL Server (EF Core) |
| `Jwt:Key` | `PLACEHOLDER_HS256_KEY_MIN_32_BYTES` | [Core / Implementado] | HS256, ≥32 bytes |
| `Jwt:Issuer` / `Jwt:Audience` | placeholder | [Core / Implementado] | validación issuer/audience |
| `Authentication:UseJwtBearer` | `true` | [Fase 04 / Implementado] | `true` = JwtBearer HS256; `false` = esquema `Anonymous` legacy (solo pruebas locales) |
| `UseInMemoryDatabase` | `true` (local) / `false` (prod) | [Core / Implementado] | tests/local |
| `Kestrel:Limits:MaxRequestBodySize` | `10485760` | [Core / Implementado] | 10 MB máx. ante DoS |
| `Kestrel:Limits:KeepAliveTimeout` | `00:02:00` | [Core / Implementado] | keep-alive defensivo |
| `Cors:AllowedOrigins` | `["https://yourproductiondomain.com"]` | [Core / Implementado] | CORS restringido |
| `AssemblyIntegrity:ExpectedSha256` | `""` | [Core / Implementado] | SHA-256 esperado del assembly en runtime |
| `PasswordHasher:MemoryKBytes` | `65536` | [Fase 04 / Implementado] | Argon2id memoria (64 MB), sin secretos |
| `PasswordHasher:Iterations` | `3` | [Fase 04 / Implementado] | Argon2id pasadas, sin secretos |
| `Redis:ConnectionString` | `localhost:6379,abortConnect=false` | [Fase 05 / Implementado] | multiplexer Redis afinado |
| `SkipMigration` | `false` | [Fase 02 / Implementado] | migraciones solo en tests (sin Migrate en arranque) |
| `EnableProviderStates` | `false` | [Fase 02 / Implementado] | gate de POST /provider-states (no-prod) |
| `Transport` | `InMemory` | [Fase 06 / Pendiente] | `InMemory` local / `SQS` prod |
| `StackName` | — | [Fase 09 / Pendiente] | stack CloudFormation |
| `PORT` | `8080` | [Fase 09 / Pendiente] | puerto Kestrel por env |
| `DB_USER` / `DB_PASSWORD` | — | [Fase 02 / Pendiente] | override de conexión |
| `PERF_LOGIN_USER` / `PERF_*` | — | [Fase 10 / Pendiente] | usuario NBomber / relajar rate limits solo en perf |
| `Observability:ConsoleExport` | `false` | [Fase 08 / Pendiente] | gate del exporter OTel |

- `appsettings.Example.json` = plantilla canónica local con placeholders seguros.
- `appsettings.Production.json` = defaults defensivos sin secretos (los valores reales llegan por env/CI).
- Secretos fuera de Git: `.gitignore` cubre `appsettings.Local.json` y overrides.

## Contratos
- Archivos: `WebAPIDevSecOpsScallingSDD/appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`, `appsettings.Example.json`.
- Env vars (futuras): `PORT`, `DB_USER`, `DB_PASSWORD`, `PERF_*`, `CORS_ALLOWED_ORIGIN`.

## Tests
- `UnitTest/Common/AppSettingsTests.cs` (3): Example.json válido y sin secretos; appsettings.json sin patrones sensibles; Example.json sin claves futuras.

## Criterios
- `dotnet build -c Release` 0 errores 0 advertencias.
- `dotnet test UnitTest` 8/8 verdes.
- Toda clave de `appsettings.Example.json` corresponde a código funcional.

## Límites
Transport/PERF_* en el JSON de ejemplo hasta que el código las consuma. `Redis:ConnectionString` ya es ciudadano de primer nivel (Fase 05). `JWT:Key` real y credenciales de BD solo por env en despliegue.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** Migrada a plantilla `_template.md`; tabla con columna Estado/Fase; novedad `appsettings.Production.json`. (+`Authentication:UseJwtBearer` 09-Oct-2026, ver `04-01`; +`PasswordHasher:*` 09-Oct-2026, ver `04-02`; sin cambio de estado.)
