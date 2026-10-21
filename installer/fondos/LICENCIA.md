# Fondos en video

Estos videos vienen con el instalador (la casilla «Agregar 10 fondos en video»)
y el programa los pone en la pestaña **Fondos de ejemplo**. Duran 20 segundos,
están en 1920×1080 a 30 cuadros por segundo, no tienen sonido y están hechos
para repetirse: el último cuadro empalma con el primero, así el bucle no se
nota.

No son fotos ni filmaciones de nadie: los dibujó el programita de la carpeta
`generador`, con degradés, ruido y figuras. Son del proyecto EcclesiaCast y se
ofrecen en **dominio público**
([CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/deed.es)):
cualquier iglesia los puede usar, cambiar y repartir, sin pedir permiso y sin
nombrar a nadie.

| Archivo | Qué se ve |
|---|---|
| `Amanecer.mp4` | Cielo de amanecer, del azul al naranja; las nubes finas se mueven y el sol respira |
| `Aurora.mp4` | Aurora verde que se mece sobre cerros oscuros, con estrellas que titilan |
| `Luces azules.mp4` | Luces azules y violetas fuera de foco que flotan despacio |
| `Luces doradas.mp4` | Luces doradas fuera de foco que flotan despacio |
| `Niebla.mp4` | Niebla gris azulada que se desplaza, con luz en el medio |
| `Noche estrellada.mp4` | Cielo de noche con estrellas que titilan y la Vía Láctea tenue |
| `Ondas violeta.mp4` | Líneas de luz rosa y violeta que ondulan abajo |
| `Profundo.mp4` | Bajo el agua: reflejos que se mueven, rayos de luz y partículas que suben |
| `Rayos de luz.mp4` | Rayos de luz que bajan desde arriba y brillan, sobre azul oscuro |
| `Tres cruces.mp4` | Tres cruces en un cerro, contra el atardecer; las nubes se mueven |

## Volver a dibujarlos

Con el SDK de .NET 8 y ffmpeg instalados:

```powershell
powershell -File installer\fondos\generar.ps1            # todos
powershell -File installer\fondos\generar.ps1 "Aurora"   # sólo uno
```

Para cambiar uno, se toca su receta en `generador\Program.cs`. Si cambia la
cantidad, hay que cambiar también el «10» de la casilla en
`installer\EcclesiaCast.iss` (una prueba lo controla). Los videos van al
repositorio: conviene no regenerarlos sin necesidad, porque cada versión nueva
suma su peso a la historia de git.
