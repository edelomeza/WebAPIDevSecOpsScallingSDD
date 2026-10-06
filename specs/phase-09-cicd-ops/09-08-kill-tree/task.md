# 09-08 — Kill Tree

## T1 — Kill-tree sin huérfanos
- **Modificar**: `ChaosTest/Helpers/FaultInjector.psm1`, `run-chaos.ps1`, `PerformanceTest/Program.cs`.
- **Verificar**: Windows `taskkill /PID /T /F`; Linux `pkill -TERM -P`; no MSBuild nodemod persistentes.
