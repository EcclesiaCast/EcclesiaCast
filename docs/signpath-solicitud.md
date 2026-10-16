> **Rechazada el 15/10/2026**: SignPath Foundation pidió más visibilidad pública antes de firmar. Este archivo queda como referencia por si se vuelve a pedir; el estado y las opciones están en [pendientes.md](pendientes.md).

# Solicitud a SignPath Foundation (para pegar en https://signpath.org/apply.html)

El formulario es en inglés: los textos de abajo están listos para pegar, campo
por campo y en el mismo orden que el formulario (revisado contra el formulario
del 15/10/2026). Los campos de datos tuyos (nombre, correo, casillas y el
reCAPTCHA) los llenás vos. **Todo lo de abajo es verdad**: no agregues nada que
no lo sea, porque la solicitud la revisa una persona.

## Campos

| Campo | Qué poner |
|---|---|
| **Homepage URL*** | `https://github.com/EcclesiaCast/EcclesiaCast` |
| **Download URL*** | `https://github.com/EcclesiaCast/EcclesiaCast` (el formulario pide que esa página diga que el proyecto usa SignPath Foundation: el README lo dice en «Code signing policy», y desde ahí se baja el instalador en «Releases») |
| **Privacy Policy URL** | `https://github.com/EcclesiaCast/EcclesiaCast/blob/main/docs/privacidad.md` |
| **Wikipedia URL** | vacío (no tiene) |
| **Tagline*** | `Free, open source Windows software to project song lyrics, Bible verses, announcements and videos in churches.` |
| **Category*** | *Productivity* (opciones: Accessibility, AI Tools, Audio, Developer Tools, Educational, Gaming, Graphics, Productivity, Science, Other) |
| **Description*** | ya la pegaste (ver «Description» abajo) |
| **Reputation*** | ver «Reputation» abajo |
| **Maintainer Type** | *Individual maintainer(s)* (las otras opciones: proyecto comunitario independiente, fundación sin fines de lucro, empresa, otro) |
| **Build System*** | *GitHub Actions* (la otra opción es GitLab CI/CD) |
| **First Name* / Last Name*** | los tuyos |
| **Email*** | el tuyo |
| **Company Name** | vacío |
| **Primary Discovery Channel*** | si hay una opción de asistente de IA / chatbot, esa; si no, *Other* |
| **Exact source (optional)** | `Claude (AI assistant) while researching free code signing options for an open source project` |
| Casilla del *Code of Conduct** | **la tildás vos después de leerlo**: dice que el certificado se emite a nombre de SignPath Foundation y que puede revocarse si se rompen las reglas |
| Casilla de *other communications* | como prefieras (es opcional) |
| Casilla de *store and process my personal data** | la tildás vos |
| reCAPTCHA | lo hacés vos |

## Description

EcclesiaCast is a free, open source Windows application for projecting song
lyrics, multi-version Bible verses, announcements, images and videos in
churches. It replaces two or three paid programs with a single free one. It is
written in C# / .NET 8 (WPF) and is used every week in a real church service.

## Reputation

EcclesiaCast is a young project, and I would rather say so plainly. The
repository was created in July 2026; the first public pre-release was on
19 July 2026, and there have been five releases since (the latest on
15 October 2026), all built by GitHub Actions from the public repository.
It is under active development (more than 240 commits, almost daily), licensed
GPL-3.0, and has more than a thousand automated tests, a user guide, a privacy
policy and a published code signing policy.

It is used every week at my church's services in Uruguay, in place of paid
projection software. It has no media coverage, stars or download statistics
yet: its users are small churches, a community that often cannot afford
commercial licenses and that today sees a "Unknown publisher" warning on every
install. Signing would remove that obstacle and let the in-app updater verify
who published each update.

Repository and releases: https://github.com/EcclesiaCast/EcclesiaCast
Code signing policy: https://github.com/EcclesiaCast/EcclesiaCast#code-signing-policy

## Lo que tiene que estar listo antes de mandarlo

- [x] El código en `main` en GitHub (hecho el 14/10/2026).
- [x] Una versión publicada con instalador (`v1.0.0-beta.15`).
- [x] Verificación en dos pasos en GitHub (hecho por Alexis).
- [x] El README tiene la sección «Code signing policy» y la política de privacidad.
- [x] El instalador sólo trae Biblias libres (RV1909, BLM, VBL, PDDPT) y librerías libres
      (VLC es LGPL); las Biblias con derechos (RVR1960, NVI…) **no** van adentro.

## Después de la aprobación

Está explicado en [pendientes.md](pendientes.md), sección «Firma digital del
instalador», pasos 3 y 4.
