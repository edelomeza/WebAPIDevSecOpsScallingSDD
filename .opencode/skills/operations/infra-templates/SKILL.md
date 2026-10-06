---
name: 09-infra-templates
description: Templates de infraestructura como referencia
---

## Propósito

Templates de infraestructura como referencia.

## Cuándo usarla

Al crear o revisar `Dockerfile`, compose, semgrep, editorconfig, sonar.

## Precondiciones

`09-02`, `09-07` definidos.

## Pasos

1. Dockerfile multi-stage (sdk→publish→aspnet).
2. `docker-compose.local.yml`, `.aws.yml`, `.chaos.yml`.
3. `.semgrep/semgrep.yaml` con reglas SAST.
4. `.editorconfig` con CA3000+.
5. `sonar-project.properties.example` con thresholds reales.

## Checklist

No root, no secretos, `ASPNETCORE_HTTP_PORTS`; `.dockerignore` y `.gitignore` defensivos.

## Criterios de done

Build Docker verde; Semgrep/dockle/Sonar consumen templates.

## Límites/trampas

Imágenes aspnet sin curl; majors de actions en pareja.

## Referencias

`09-07`, `Dockerfile`, `deploy/`, `.semgrep/`, `.editorconfig`.
