# plan.md — 03-00-routing-versioning

1. Mapeo

- Modificar: Program.cs (AddApiVersioning), Controllers/* (rutas).

2. Decisiones

- DefaultApiVersion=1.0, UrlSegmentApiVersionReader, PropertyNamingPolicy=null, /scalar solo Dev.

3. Guardarraíles

- No mezclar PascalCase y camelCase.
- No exponer OpenAPI/Scalar en Prod.

4. Pruebas

- IntegrationTest/Routing/RoutingTests.cs.

5. Secuencia

1. Versioning.
2. JSON options.
3. Rutas.
