---
name: 09-cicd-ops
description: Pipeline CI, nightly, Docker, AWS, chaos, Dependabot
---

## Propósito

Pipeline, nightly, Docker, AWS, chaos, Dependabot.

## Cuándo usarla

Fase 11.

## Pasos

CI orden restore→build→test×3; nightly Stryker 180min y chaos; Dockerfile; CloudFormation; Dependabot.

## Checklist

Jobs agregadores tolerantes; timeouts medidos.

## Criterios de done

Pipeline verde en main; nightly chaos ejecutado.

## Límites/trampas

Majors de artifacts en pareja; `ASPNETCORE_HTTP_PORTS` en .NET 10.

## Referencias

`09-01`…`09-03`.
