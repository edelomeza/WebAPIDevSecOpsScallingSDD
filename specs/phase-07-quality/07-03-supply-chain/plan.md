# plan.md — 07-03-supply-chain

1. Mapeo

- Modificar: CI (Trivy, dockle, ZAP, RESTler).
- Crear: scripts/hardening_summary.py, hardening/README.md.

2. Guardarraíles

- Sin HIGH/CRITICAL.
- ZAP sin alertas nuevas.
- Hardening report tolerante.

3. Pruebas

- CI: jobs verdes o informativos documentados.

4. Secuencia

1. Trivy/dockle.
2. ZAP.
3. RESTler.
4. Hardening report.
