# 02-03 — Esquema

## Contexto
Esquema detallado y verificable de las 13 tablas. **Depende de**: `02-01`. Fuente de verdad: `WebAPIDevSecOpsScallingSDD/Migrations/*_InitialCreate.cs` + `*_UniqueConstraints.cs`. No cambiar tipos sin migración.

## Requisitos
1. Documentar las 13 entidades con columnas, tipos SQL, nullabilidad, PKs, FKs e índices.
2. Aplicar 2 índices únicos: `VenPedidoPago.strIdTransaccion` (filtrado `IS NOT NULL`) y `VenPedidoFactura.strFolioFactura`.
3. Verificar el esquema contra SQL Server real.

## Diseño
- PK `id` (`int IDENTITY(1,1)`, salvo `VenPedido.id uniqueidentifier`); `RowVersion rowversion` en 11 auditables (catálogos sin ella); `decimal(18,2)`; `nvarchar(n)`; `datetime2`; `bit`.
- FKs con `Cascade`, salvo `EmpEmpleado → EmpCatTipoEmpleado` (nullable, sin cascade).
- `LegacyVentaId` como FK lógica sin constraint.
- 12 índices IX sobre FKs (`IX_<Tabla>_<columna>`) + 2 UNIQUE.

## Contratos
Esquema por tabla (columna, tipo, null, PK/FK/índice):
- CliCliente: id (int IDENTITY PK), strNombreCliente nvarchar(100) NOT NULL, strDireccionCliente nvarchar(200) NULL, RowVersion rowversion NOT NULL, strCorreoElectronico nvarchar(100) NOT NULL, strNumeroTelefono nvarchar(10) NOT NULL, strCreadoPorUsuario nvarchar(50) NULL.
- EmpCatTipoEmpleado (catálogo, sin RowVersion): id (int IDENTITY PK), strValor nvarchar(50) NOT NULL, strDescripcion nvarchar(150) NOT NULL.
- EmpEmpleado: id (int IDENTITY PK), strNombre nvarchar(50) NOT NULL, strAPaterno/strAMaterno nvarchar(50) NULL, strCURP nvarchar(18) NULL, idEmpCatTipoEmpleado int NULL FK → EmpCatTipoEmpleado (sin cascade), RowVersion rowversion NOT NULL.
- ProProducto: id (int IDENTITY PK), strNombreProducto nvarchar(50) NOT NULL, strURLImagen nvarchar(300) NULL, strDescripcion nvarchar(250) NULL, intNumeroExistencia int NOT NULL, decPrecio decimal(18,2) NOT NULL, RowVersion rowversion NOT NULL, strCreadoPorUsuario nvarchar(50) NULL.
- SegUsuario: id (int IDENTITY PK), strNombre nvarchar(50) NOT NULL, strPWD nvarchar(200) NOT NULL (hash, nunca en claro), strCorreoElectronico nvarchar(50) NOT NULL, dteFechaRegistro datetime2 NULL, bln2FAHabilitado bit NOT NULL, str2FASecreto nvarchar(200) NULL, RowVersion rowversion NOT NULL.
- SegRefreshToken: id (int IDENTITY PK), idSegUsuario int NOT NULL FK → SegUsuario (cascade), strTokenHash nvarchar(64) NOT NULL, dteExpiresAt/dteCreatedAt datetime2 NOT NULL, dteRevokedAt datetime2 NULL, strReplacedByTokenHash nvarchar(64) NULL, RowVersion rowversion NOT NULL.
- VenCatEstado (catálogo, sin RowVersion): id (int IDENTITY PK), strValor nvarchar(50) NOT NULL, strDescripcion nvarchar(200) NULL.
- VenPedido: id (uniqueidentifier PK, sin IDENTITY), idCliCliente int NOT NULL FK → CliCliente (cascade), dteFechaPedido datetime2 NOT NULL, decTotal decimal(18,2) NOT NULL, strEstadoSaga nvarchar(50) NOT NULL, strMotivoRechazo nvarchar(500) NULL, LegacyVentaId int NULL (FK lógica sin constraint), RowVersion rowversion NOT NULL.
- VenPedidoDetalle: id (int IDENTITY PK), idVenPedido uniqueidentifier NOT NULL FK → VenPedido (cascade), idProProducto int NOT NULL FK → ProProducto (cascade), intCantidad int NOT NULL, decPrecioUnitario decimal(18,2) NOT NULL, RowVersion rowversion NOT NULL.
- VenPedidoPago: id (int IDENTITY PK), idVenPedido uniqueidentifier NOT NULL FK → VenPedido (cascade), decMonto decimal(18,2) NOT NULL, strMetodoPago nvarchar(50) NULL, strIdTransaccion nvarchar(100) NULL UNIQUE (filtrado IS NOT NULL), strEstado nvarchar(20) NOT NULL, dteFechaPago datetime2 NOT NULL, RowVersion rowversion NOT NULL.
- VenPedidoFactura: id (int IDENTITY PK), idVenPedido uniqueidentifier NOT NULL FK → VenPedido (cascade), strFolioFactura nvarchar(50) NOT NULL UNIQUE, strRFC nvarchar(13) NULL, decTotal decimal(18,2) NOT NULL, dteFechaEmision datetime2 NOT NULL, strEstado nvarchar(20) NOT NULL, RowVersion rowversion NOT NULL.
- VenVenta: id (int IDENTITY PK), idCliCliente/idSegUsuario/idVenCatEstado int NOT NULL FK → CliCliente/SegUsuario/VenCatEstado (cascade), dteFechaHoraCompra datetime2 NULL, strClaveVenta nvarchar(10) NOT NULL, RowVersion rowversion NOT NULL.
- VenVentaDetalle: id (int IDENTITY PK), idVenVenta int NOT NULL FK → VenVenta (cascade), idProProducto int NOT NULL FK → ProProducto (cascade), intPiezaVenta int NOT NULL, decTotalVenta decimal(18,2) NOT NULL, RowVersion rowversion NOT NULL.
- UNIQUE: `IX_VenPedidoPago_strIdTransaccion` (filtrado), `IX_VenPedidoFactura_strFolioFactura`.

## Tests
- `DatabaseTest/SchemaTests.cs` (MsSql puerto fijo): 13 tablas, 13 PKs, 12 FKs, 11 `rowversion`, 6 `decimal(18,2)`, 2 únicos.

## Criterios
- 13 tablas con columnas/tipos/PKs/FKs/índices verificados contra SQL Server real; 2 índices únicos aplicados.

## Límites
- Valores de estados saga (`strEstadoSaga`, `strEstado`) pendientes de fase 06; no se enumeran aquí.

## Aprobación y Control de Cambios
- **Estado:** ✅ Aprobado
- **Revisores:** @arquitecto-principal (1 Revisor)
- **Fecha:** 04-Oct-2026
- **Detalle:** esquema reescrito desde migración + `UniqueConstraints`; `SchemaTests` verde.
