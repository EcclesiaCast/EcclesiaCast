# Fondos de ejemplo

Estas imágenes vienen con el instalador (la casilla «Agregar 12 fondos de
ejemplo») y el programa las pone en la pestaña **Fondos de ejemplo**.

No son fotos de nadie: las dibujó el programita de la carpeta `generador`, con
degradés, ruido y figuras. Son del proyecto EcclesiaCast y se ofrecen en
**dominio público** ([CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/deed.es)):
cualquier iglesia las puede usar, cambiar y repartir, sin pedir permiso y sin
nombrar a nadie.

| Archivo | Qué es |
|---|---|
| `Amanecer.jpg` | Cielo de amanecer, del azul al naranja, con nubes finas |
| `Aurora.jpg` | Aurora verde sobre un cielo con estrellas y cerros oscuros |
| `Cálido.jpg` | Marrón cálido y liso, con una luz suave arriba a la izquierda |
| `Luces azules.jpg` | Luces azules y violetas fuera de foco |
| `Luces doradas.jpg` | Luces doradas fuera de foco |
| `Mosaico azul.jpg` | Triángulos en tonos de azul |
| `Niebla.jpg` | Niebla gris azulada, con luz en el medio |
| `Noche estrellada.jpg` | Cielo de noche con estrellas y la Vía Láctea tenue |
| `Ondas violeta.jpg` | Líneas de luz rosa y violeta que ondulan abajo |
| `Profundo.jpg` | Bajo el agua: rayos de luz y reflejos sobre azul profundo |
| `Rayos de luz.jpg` | Rayos de luz que bajan desde arriba sobre azul oscuro |
| `Tres cruces.jpg` | Tres cruces en un cerro, recortadas contra el atardecer |

## Volver a dibujarlos

Con el SDK de .NET 8 y ffmpeg instalados:

```powershell
powershell -File installer\fondos\generar.ps1
```

Para cambiar uno, se toca su receta en `generador\Program.cs`. Si cambia la
cantidad, hay que cambiar también el «12» de la casilla en
`installer\EcclesiaCast.iss` (una prueba lo controla).
