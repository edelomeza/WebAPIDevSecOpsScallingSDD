---
name: 03-legacy-sales
description: Venta/venta-detalle síncrono con stock en la misma transacción
---

## Propósito

Implementar venta/venta-detalle síncrono con stock en misma Tx.

## Cuándo usarla

Al crear `03-10` y `03-11`.

## Precondiciones

`03-01`, `03-05`, `03-04`, `03-03`, `02-01`.

## Pasos

1. Crear `VenVenta`/`VenVentaDetalle` con FK a cliente/usuario/estado/producto.
2. Validar cliente/usuario/estado antes de crear venta.
3. Descontar stock en la misma transacción EF.
4. Manejar race condition: 5 POST paralelos con `existencia=1` → 1 éxito.
5. En delete de `VenVentaDetalle` restaurar stock.

## Checklist

Tx única para venta+stock; restore stock en delete; ownership 403 en detalle.

## Criterios de done

Venta 201 con stock actualizado; delete restaura stock; race test pasa.

## Límites/trampas

No descontar stock fuera de Tx; no olvidar ownership en detalle.

## Referencias

`03-10`, `03-11`, `RaceConditionTests`.
