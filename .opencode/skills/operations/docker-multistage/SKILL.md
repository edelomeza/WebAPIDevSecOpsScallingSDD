---
name: docker-multistage
description: Build de imagen Docker reproducible y seguro multi-stage
---

## Propósito

Build reproducible y seguro.

## Pasos

sdk:10.0 restore/build → publish → aspnet:10.0 runtime; non-root; `ASPNETCORE_HTTP_PORTS`.

## Checklist

No copiar secretos; `.dockerignore`.

## Referencias

`Dockerfile`.
