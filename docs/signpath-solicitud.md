# Solicitud a SignPath Foundation (para pegar en https://signpath.org/apply.html)

El formulario es en inglés: los textos de abajo ya están listos para pegar.
Lo único que va a pedir de tu parte es tu nombre y tu correo; eso lo escribís
vos. **Antes de mandarlo**, mirá la lista de «Lo que tiene que estar listo».

## Textos para pegar

**Project name:** EcclesiaCast

**Repository URL:** https://github.com/EcclesiaCast/EcclesiaCast

**Homepage / download page:** https://github.com/EcclesiaCast/EcclesiaCast/releases

**License:** GPL-3.0 (OSI-approved), no dual licensing.

**Short description:**
EcclesiaCast is a free, open source Windows application for projecting song
lyrics, multi-version Bible verses, announcements, images and videos in
churches. It replaces two or three paid programs with a single free one. It is
written in C# / .NET 8 (WPF) and is used every week in a real church service.

**What would be signed:** the Windows installer (`EcclesiaCast-<version>-setup.exe`,
built with Inno Setup) and the application executable inside it
(`EcclesiaCast.App.exe`), built by GitHub Actions from the public repository
(`.github/workflows/release.yml`) and approved by hand before each release.

**Build system:** GitHub Actions (windows-latest). The signing request is sent
from the same workflow with the official `signpath/github-action-submit-signing-request` action.

**Code signing policy page:** https://github.com/EcclesiaCast/EcclesiaCast#code-signing-policy

**Privacy policy:** https://github.com/EcclesiaCast/EcclesiaCast/blob/main/docs/privacidad.md

**Team / approvers:** one maintainer, Alexis Pérez (@alexisperez7); approver: the
same person. Contributions from anyone else come in by pull request and are
reviewed by the maintainer.

## Lo que tiene que estar listo antes de mandarlo

- [ ] El código de este repositorio (rama `beta6`) **subido a `main`** en GitHub:
      hoy `main` está 145 cambios atrás y SignPath mira ahí el flujo de
      publicación, la política de firma y la política de privacidad.
- [ ] Tener publicada una versión con instalador (SignPath pide que el
      proyecto «ya esté publicado en la forma que se va a firmar»). La última
      publicada es la `v1.0.0-beta.5`.
- [ ] Verificación en dos pasos activada en tu cuenta de GitHub (se la piden a
      todo el equipo).
- [ ] Que el proyecto no incluya ninguna parte que no sea libre. El instalador
      sólo trae Biblias libres (RV1909, BLM, VBL) y librerías libres (VLC es LGPL);
      las Biblias con derechos (RVR1960, NVI…) **no** van adentro.

## Después de la aprobación

Está explicado en [pendientes.md](pendientes.md), sección «Firma digital del
instalador», pasos 3 y 4.
