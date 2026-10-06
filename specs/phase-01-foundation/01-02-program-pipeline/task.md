# 01-02 — Program Pipeline

## T1 — Pipeline y DI
- **Modificar**: `WebAPIDevSecOpsScallingSDD/Program.cs`.
- **Verificar**: middleware en orden fijo; DI registrada; `IntegrationTest/Middleware/MiddlewareTests.cs`; `UnitTest/DI/ServiceRegistrationTests.cs`.
- **Guardarraíles**: no reordenar middleware; HSTS solo no-Dev.
