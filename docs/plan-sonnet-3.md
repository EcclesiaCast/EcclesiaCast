# Plan de trabajo para Sonnet — notas en el texto de la Biblia

Leelo entero antes de tocar nada. Valen las **reglas de trabajo** de
[plan-sonnet.md](plan-sonnet.md) (sección 2) y lo que agrega
[plan-sonnet-2.md](plan-sonnet-2.md) al principio: no abrir la aplicación ni
ninguna ventana visible, no escribir nunca en la biblioteca real
(`\\localhost\C$\Users\alexi\AppData\Roaming\EcclesiaCast`; para datos reales,
copiar sólo `ecclesiacast.db` a una carpeta temporal), rama `beta6`, sin push,
un commit por punto en inglés, castellano rioplatense para lo que se ve,
CHANGELOG y pendientes al día.

## Lo que se encontró (8/10/2026, en la biblioteca de Alexis)

Algunos archivos de Biblias traen notas de la edición impresa pegadas al
texto, y se proyectan. Revisados los 124.783 versículos de sus cuatro
versiones:

- **NTV**, 3 versículos:
  - Juan 7:52 termina con `---------- [Los manuscritos griegos más antiguos no incluyen Juan 7:53–8:11].`
  - Juan 8:11 termina con `----------`.
  - Marcos 16:8: `---------- [Los manuscritos más antiguos de Marcos terminan en el versículo 8. …] [Final corto de Marcos] … [Final largo de Marcos]`.
  - **Ojo:** 1 Samuel 10:27 tiene un párrafo entre corchetes
    (`[Nahas, rey de los amonitas, había estado oprimiendo…]`) que **es texto
    bíblico** (la NTV lo toma de los manuscritos del Mar Muerto). **No se
    toca.** No se puede borrar todo lo que esté entre corchetes.
- **TLA**: 2.019 versículos empiezan con su número y la numeración hebrea
  («8 (9) Dios mío…»), que ya esconde `VerseGrouping.StripVerseNumbers`.
  Quedan 3 con números **en el medio**: 1 Samuel 20:42 (`… juramento». 1 (21.1) Dicho esto…`),
  1 Reyes 22:43 (`… en todo. 43b (44) Sin embargo…`), 1 Crónicas 12:4
  (`… valientes; 4b (5) Jeremías…`).
- **RVR1960**: limpia. No tiene «Selah» ni títulos de Salmos («Al músico
  principal…»).

## 1. Limpieza al mostrar

Una regla en Core (por ejemplo `BibleTextCleaner`), con pruebas, que se
aplica **sólo a lo que se muestra**: el texto guardado no cambia (así se puede
volver atrás y la búsqueda sigue igual). Saca:

- Las **rayas**: 4 o más `-` o `_` seguidos.
- La **nota entre corchetes que viene justo después de una raya**.
- Los **títulos cortos entre corchetes**, como `[Final corto de Marcos]` y
  `[Final largo de Marcos]`: corchetes con poco texto (pocas palabras) y sin
  puntuación de oración. Un párrafo entre corchetes con oraciones (como el de
  1 Samuel 10:27) **queda**.
- Los **números de versículo en el medio del texto** como los de la TLA
  (`1 (21.1)`, `43b (44)`, `4b (5)`): un número, opcionalmente con letra o
  rango, seguido de otro entre paréntesis, entre espacios. Que no se lleve
  números del texto común («los doce», «7.000 hombres», «(Sal 23)»).
- Y los espacios dobles que queden.

Las pruebas usan los casos reales de arriba (los de la NTV y la TLA, y que
1 Samuel 10:27 quede igual).

Aplicala en **todos** los lugares donde se muestra el texto de un versículo:
las diapositivas (`LoadBiblePassage` / `VerseGrouping`), los resultados de
búsqueda (Ctrl+K y el `?` del buscador de la Biblia), la cajita de «➕ Playlist»,
el escenario, el control remoto y la salida web.

## 2. «Corregir este versículo»

Clic derecho sobre una diapositiva de la Biblia → **«Corregir el texto…»**.
Abre una ventanita con el texto **guardado** de ese versículo en la versión
principal, se edita y se guarda en la base para esa versión. Si la diapositiva
es un grupo de versículos que la versión cuenta juntos (TLA, `VerseGroup`), la
corrección se guarda en todos los versículos del grupo. Después se rearma la
grilla manteniendo lo que está en vivo (`RebuildSlidesPreservingLive`). Con
dos versiones activas, se corrige la principal (que lo diga la ventanita).

## 3. «Selah» y títulos de los Salmos

Ninguna Biblia de Alexis los tiene hoy, pero otros archivos sí. En el tema de
Biblia, una casilla **«Mostrar "Selah" y los títulos de los Salmos»**,
**destildada** de entrada (o sea: se ocultan). Con la casilla destildada:
- `Selah` como palabra suelta desaparece del texto mostrado.
- Si un archivo trae el título de un Salmo como versículo 0, no se muestra.
  Si lo trae pegado al principio del versículo 1 no hay forma segura de
  separarlo: eso se corrige a mano con el punto 2.

## Al terminar

Rearmá el instalador de la beta.7 como en `.github/workflows/release.yml`
(`dotnet publish … -p:Version=1.0.0-beta.7 --output publish` y después
`"%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0-beta.7 installer\EcclesiaCast.iss`;
borrá `publish` antes). Sumá a `docs/pendientes.md`, sección 1 «Para probar»,
cómo probarlo (por ejemplo Juan 8:11 y Marcos 16:8 en NTV, y corregir un
versículo). Informe final en castellano, para Alexis: qué quedó hecho (con
commits), cómo lo probaste, qué no pudiste probar y qué decidiste vos.
