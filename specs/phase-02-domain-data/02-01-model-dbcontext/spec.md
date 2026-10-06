# 02-01 — Modelo y DbContext

## Contexto
Modelado de entidades del dominio y `DbContext` de EF Core. **Depende de**: `01-01`.

## Requisitos
1. Crear las 13 entidades con naming `Ven*`/`Cli*`/`Emp*`/`Pro*`/`Seg*` y prefijos `str/int/dec/dte/bln`.
2. Crear `Context/AppDbContext.cs` con `DbSet<>` por entidad.
3. Configurar `RowVersion` como `rowversion` vía Fluent API en entidades auditables.
4. Soportar provider SQL Server e InMemory (mitigación `byte[]{1}` en InMemory).
5. Registrar `AppDbContext` en DI sin connection strings hardcodeados.

## Diseño
- `WebAPIDevSecOpsScallingSDD/Models/`: una clase por entidad + `IConcurrenteAuditable` (`byte[] RowVersion`).
- Auditables (11): `CliCliente`, `EmpEmpleado`, `ProProducto`, `SegUsuario`, `SegRefreshToken`, `VenPedido`, `VenPedidoDetalle`, `VenPedidoPago`, `VenPedidoFactura`, `VenVenta`, `VenVentaDetalle`.
- Catálogos sin auditar: `EmpCatTipoEmpleado`, `VenCatEstado`.
- `OnModelCreating` itera entidades `IConcurrenteAuditable` y aplica `IsRowVersion()`; decimales con precisión (18,2).
- `SaveChanges`/`SaveChangesAsync` fuerzan `RowVersion = new byte[]{1}` solo en provider InMemory.
- `Program.cs` registra con `UseInMemoryDatabase` (default `true`) o `UseSqlServer(ConnectionStrings:Default)`; la lectura de config es perezosa dentro del factory de DI (compatible con overrides de `WebApplicationFactory`).

## Contratos
- `ConnectionStrings:Default`, `UseInMemoryDatabase` (ver 01-04).
- Navegaciones 1:N y `LegacyVentaId` como FK lógica sin constraint.

## Tests
- `UnitTest/DbContext/DbContextTests.cs`: inicialización con SqlServer (dummy, sin conexión) e InMemory con `RowVersion` forzado.

## Criterios
- `dotnet build -c Release` 0 errores 0 advertencias.
- `dotnet test UnitTest` con `DbContextTests` verdes.

## Límites
- Sin migraciones (ver `02-02`); sin seed (ver `02-02`/`02-04`); sin endpoints (fase 03).

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** 13 entidades + `AppDbContext` con `RowVersion` e InMemory `byte[]{1}`; tests 10/10.
