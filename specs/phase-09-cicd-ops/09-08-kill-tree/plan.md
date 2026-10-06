# plan.md — 09-08-kill-tree

1. Mapeo

- Modificar: ChaosTest/Helpers/FaultInjector.psm1, run-chaos.ps1, PerformanceTest/Program.cs.

2. Guardarraíles

- Windows taskkill /PID /T /F; Linux pkill -TERM -P.
- No matar nodos MSBuild persistentes salvo limpieza real.

3. Pruebas

- Manual: tras chaos/perf, Get-Process dotnet no muestra el PID raíz ni hijos.

4. Secuencia

1. Detectar PID raíz.
2. Kill-tree.
3. Validar.
