# plan.md — 02-04-test-data

1. Mapeo

- Crear: UnitTest/Common/TestDataFactory.cs, seeds en IntegrationTest/Common/*.
- Modificar: specs 03-10, 03-12.

2. Decisiones

- Admin, cliente 1, producto 1 (existencia=1), venta seed, PERF_LOGIN_USER.

3. Guardarraíles

- No crear datos de prueba en runtime de producción.
- PERF_LOGIN_USER solo para perf env.

4. Pruebas

- UnitTest/Common/TestDataFactoryTests.cs.

5. Secuencia

1. Seeds base.
2. Datos para race/saga/perf.
