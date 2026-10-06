---
name: kill-tree
description: Matar árboles de proceso sin dejar huérfanos
---

## Propósito

Matar árboles de proceso sin dejar huérfanos.

## Pasos

Windows `taskkill /PID /T /F`; Linux `pkill -TERM -P`.

## Checklist

No matar nodos MSBuild persistentes salvo limpieza real.

## Referencias

`ChaosTest`, `agent.md`.
