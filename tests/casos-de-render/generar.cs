#:project ../../src/EcclesiaCast.Presentation/EcclesiaCast.Presentation.csproj
#:property PublishAot=false

// Writes the render cases (one .json each) into this folder. Run from the
// root of the repository:
//
//     dotnet run tests/casos-de-render/generar.cs
//
// The cases are frozen data: once the references of the WPF bench are saved
// (referencia-wpf/), changing a case here means saving them again.

using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Presentation.Rendering;

var folder = Path.Combine(Directory.GetCurrentDirectory(), "tests", "casos-de-render");
if (!Directory.Exists(folder))
{
    Console.Error.WriteLine("Run it from the root of the repository.");
    return 1;
}

foreach (var old in Directory.GetFiles(folder, "*.json"))
    File.Delete(old);

// ── The three themes ─────────────────────────────────────────────
// The first two are what ThemeSeeder creates on the first run of the program;
// the third is a narrow, heavy typeface that stresses the line cuts.

SlideTheme Song() => new() { Name = "Canciones", Kind = ThemeKind.Song, TransparentBackground = true };

SlideTheme Bible() => new()
{
    Name = "Biblia",
    Kind = ThemeKind.Bible,
    Bold = false,
    MaxFontSize = 120,
    ShowVerseNumbers = false,
    TransparentBackground = true,
};

SlideTheme Impact() => new()
{
    Name = "Impacto",
    Kind = ThemeKind.Song,
    FontFamily = "Impact",
    Bold = false,
    MaxFontSize = 110,
    MinFontSize = 40,
    TransparentBackground = true,
};

var count = 0;

void Write(string id, string description, Action<RenderCase> shape, Action<SlideTheme>? tweak = null)
{
    var c = new RenderCase { Id = id, Description = description };
    shape(c);
    foreach (var (key, make) in new (string, Func<SlideTheme>)[] { ("cancion", Song), ("biblia", Bible), ("impact", Impact) })
    {
        var theme = make();
        tweak?.Invoke(theme);
        c.Themes.Add(new RenderTheme { Key = key, Theme = theme });
    }

    File.WriteAllText(Path.Combine(folder, id + ".json"), c.ToJson() + "\n", new System.Text.UTF8Encoding(false));
    count++;
}

void Slide(string id, string description, string main, string? caption = null, string? second = null,
    SlideOverride? over = null, Action<SlideTheme>? tweak = null, Action<RenderCase>? more = null)
{
    // Shadows, outlines and bands do not show over black: those cases get a
    // mid-grey background so the pictures can be judged by eye.
    var inner = tweak;
    if (id.StartsWith("efecto-") || id.StartsWith("franja-"))
        tweak = t =>
        {
            t.TransparentBackground = false;
            t.BackgroundColor = "#7A8499";
            inner?.Invoke(t);
        };

    Write(id, description, c =>
    {
        c.Slide = new RenderSlide { MainText = main, Caption = caption, SecondaryText = second, Override = over };
        more?.Invoke(c);
    }, tweak);
}

// ── Texts ────────────────────────────────────────────────────────

const string Two = "Santo, santo, santo, Señor omnipotente\nSiempre el labio mío loores te dará";
const string Four =
    "Cuán grande es Él, cuán grande es Él\nCanta mi alma a Ti, Señor\nCuán grande es Él, cuán grande es Él\nTu nombre alabaré";
const string Six =
    "Sublime gracia del Señor\nQue a un infeliz salvó\nYo ciego fui, mas veo hoy\nPerdido y Él me halló\nFue la gracia que me enseñó a temer\nY mis dudas ahuyentó";
const string Long4 =
    "Sublime es tu gracia que me alcanzó en el camino cuando yo andaba perdido\n"
    + "Tu mano me levantó y me puso sobre la roca firme donde ya no caeré\n"
    + "Hoy canto con todo mi corazón porque tu misericordia nunca se acaba\n"
    + "Por siempre y para siempre te alabaré, Señor, mi fortaleza y mi canción";
const string LongLine =
    "Digno es el Cordero que fue inmolado de recibir el poder, las riquezas, la sabiduría, la fortaleza, la honra, la gloria y la alabanza";
const string NoSpaces =
    "Aleluyaaleluyaaleluyaaleluyaaleluyaaleluyaaleluyaaleluyaaleluyaaleluyaaleluyaaleluyaaleluyaaleluya";
const string John316 =
    "Porque de tal manera amó Dios al mundo, que ha dado a su Hijo unigénito, para que todo aquel que en él cree, no se pierda, mas tenga vida eterna.";
const string John316Other =
    "Porque tanto amó Dios al mundo que dio a su Hijo único, para que todo el que cree en él no muera, sino que tenga vida eterna.";
const string VeryLongVerse =
    "Y por la mañana, cuando todo el pueblo se había reunido en la plaza, los escribanos del rey fueron llamados y se escribió, conforme a todo lo que se mandaba, a los jefes, a los gobernadores y a los príncipes de las provincias que había desde la India hasta Etiopía, ciento veintisiete provincias, a cada provincia según su escritura, a cada pueblo según su lengua, y también a los judíos conforme a su escritura y a su lengua, para que todos supieran lo que el rey había dispuesto y lo cumplieran sin tardanza.";

// ── Songs ────────────────────────────────────────────────────────

Slide("cancion-1-renglon-corto", "Un renglón de una palabra.", "Aleluya", "Aleluya · Coros");
Slide("cancion-1-renglon-largo", "Un solo renglón que no entra en el ancho y se corta solo.", LongLine, "Digno es el Cordero");
Slide("cancion-2-renglones", "Dos renglones medianos.", Two, "Santo, santo, santo");
Slide("cancion-4-renglones", "Cuatro renglones medianos.", Four, "Cuán grande es Él");
Slide("cancion-6-renglones", "Seis renglones: el tamaño tiene que bajar para que entren.", Six, "Sublime gracia");
Slide("cancion-renglones-cortos", "Cuatro renglones de una o dos palabras.", "Gloria\nAleluya\nAmén\nCristo vive", "Coros");
Slide("cancion-renglones-largos", "Cuatro renglones largos, cada uno se parte en dos.", Long4, "Sublime es tu gracia");
Slide("cancion-renglon-larguisimo-sin-espacios",
    "Una palabra de cien letras: no hay dónde cortar, parte por la letra.", NoSpaces, "Aleluya");
Slide("cancion-todo-mayusculas-tema", "El tema pasa todo a mayúsculas.", Four, "Cuán grande es Él",
    tweak: t => t.TextCase = TextCase.Upper);
Slide("cancion-escrita-en-mayusculas", "El texto ya viene escrito en mayúsculas.", Four.ToUpperInvariant(), "Cuán grande es Él");
Slide("cancion-acentos-y-enies", "Acentos, eñes, diéresis.",
    "Mañana será un día de gozo, ¡Señor!\nÑandú, cigüeña, camión y corazón\nÁngeles, árbol, éxodo, íntimo, óleo, único",
    "Acentos");
Slide("cancion-signos-de-apertura", "Signos de apertura al principio de renglón.",
    "¿Quién es como tú, oh Jehová?\n¡Aleluya, aleluya!\n¡¿Cómo no alabarte?!\n¿Dónde iré de tu Espíritu?", "Signos");
Slide("cancion-titulo-tema", "El tema pone cada palabra con mayúscula inicial.", Four, "Cuán grande es Él",
    tweak: t => t.TextCase = TextCase.Title);
Slide("cancion-minusculas", "El tema lo pasa todo a minúsculas.", Four, "Cuán grande es Él",
    tweak: t => t.TextCase = TextCase.Lower);

// ── Bible ────────────────────────────────────────────────────────

Slide("biblia-versiculo-corto", "Un versículo de dos palabras.", "Jesús lloró.", "Juan 11:35 · PRUEBA");
Slide("biblia-versiculo-medio", "Un versículo de largo común.", John316, "Juan 3:16 · PRUEBA");
Slide("biblia-versiculo-muy-largo", "Un versículo larguísimo: el tamaño baja hasta que entra.", VeryLongVerse, "Ester 8:9 · PRUEBA");
Slide("biblia-dos-versiones", "El mismo versículo en dos versiones, una debajo de la otra.", John316, "Juan 3:16 · PRUEBA / OTRA",
    John316Other);
Slide("biblia-dos-versiones-largas", "Dos versiones de un versículo largo.", VeryLongVerse, "Ester 8:9 · PRUEBA / OTRA",
    VeryLongVerse.Replace("escribanos", "secretarios").Replace("plaza", "plaza pública"));
Slide("biblia-con-cita", "Versículo con la cita abajo a la derecha.", John316, "Juan 3:16 · PRUEBA");
Slide("biblia-sin-cita", "Versículo sin cita.", John316);
Slide("biblia-cita-oculta-por-el-tema", "La cita existe pero el tema no la muestra.", John316, "Juan 3:16 · PRUEBA",
    tweak: t => t.ShowCaption = false);
Slide("biblia-dos-versiones-mismo-estilo", "La segunda versión con el mismo estilo que la primera.", John316,
    "Juan 3:16 · PRUEBA / OTRA", John316Other, tweak: t => t.SecondaryMatchesPrimary = true);
Slide("biblia-numeros-de-versiculo", "Varios versículos seguidos, con su número.",
    "16 Porque de tal manera amó Dios al mundo, que ha dado a su Hijo unigénito. 17 Porque no envió Dios a su Hijo al mundo para condenar al mundo, sino para que el mundo sea salvo por él.",
    "Juan 3:16-17 · PRUEBA");

// ── Fit to width ─────────────────────────────────────────────────

Slide("ajuste-ancho-prendido-largos", "Ajuste al ancho prendido: ningún renglón se parte, el tamaño baja.", Long4, "Ajuste",
    tweak: t => t.FitToWidth = true);
Slide("ajuste-ancho-apagado-largos", "El mismo texto con el ajuste apagado.", Long4, "Ajuste", tweak: t => t.FitToWidth = false);
Slide("ajuste-ancho-prendido-larguisimo", "Un renglón larguísimo sin espacios con el ajuste prendido.", NoSpaces, "Ajuste",
    tweak: t => t.FitToWidth = true);
Slide("ajuste-ancho-prendido-normales", "Cuatro renglones normales con el ajuste prendido.", Four, "Ajuste",
    tweak: t => t.FitToWidth = true);

// ── Line spacing ─────────────────────────────────────────────────

Slide("interlineado-chico", "Interlineado apretado (0,85) en cuatro renglones.", Four, "Interlineado", over: new SlideOverride(LineSpacing: 0.85));
Slide("interlineado-grande", "Interlineado amplio (1,6) en cuatro renglones.", Four, "Interlineado", over: new SlideOverride(LineSpacing: 1.6));
Slide("interlineado-grande-seis-renglones", "Interlineado amplio en seis renglones: baja el tamaño.", Six, "Interlineado",
    over: new SlideOverride(LineSpacing: 1.6));

// ── Effects ──────────────────────────────────────────────────────

Slide("efecto-solo-contorno", "Contorno negro, sin sombra.", Two, "Efectos",
    tweak: t => { t.Shadow = false; t.OutlineWidth = 8; t.OutlineColor = "#000000"; });
Slide("efecto-solo-sombra", "Sombra, sin contorno.", Two, "Efectos", tweak: t => { t.Shadow = true; t.OutlineWidth = 0; });
Slide("efecto-contorno-y-sombra", "Contorno y sombra juntos.", Two, "Efectos",
    tweak: t => { t.Shadow = true; t.OutlineWidth = 8; t.OutlineColor = "#000000"; });
Slide("efecto-ninguno", "Ni contorno ni sombra.", Two, "Efectos", tweak: t => { t.Shadow = false; t.OutlineWidth = 0; });
Slide("efecto-contorno-grueso-de-color", "Contorno grueso azul con letra amarilla y sombra.", Two, "Efectos",
    tweak: t => { t.Shadow = true; t.OutlineWidth = 16; t.OutlineColor = "#1F3FA0"; t.TextColor = "#FFE066"; });
Slide("efecto-contorno-biblia-dos-versiones", "Contorno y sombra en dos versiones de la Biblia.", John316, "Juan 3:16 · PRUEBA / OTRA",
    John316Other, tweak: t => { t.Shadow = true; t.OutlineWidth = 8; });

// ── Bands ────────────────────────────────────────────────────────

Slide("franja-texto-negra", "Franja negra tan ancha como cada renglón.", Four, "Franjas",
    tweak: t => { t.LineBand = LineBand.Text; t.LineBandColor = "#000000"; t.LineBandOpacity = 1; });
Slide("franja-ancho-completo-negra", "Franja negra de borde a borde.", Four, "Franjas",
    tweak: t => { t.LineBand = LineBand.FullWidth; t.LineBandColor = "#000000"; t.LineBandOpacity = 1; });
Slide("franja-texto-blanca-letra-negra", "Franja blanca con letra negra.", Four, "Franjas",
    tweak: t => { t.LineBand = LineBand.Text; t.LineBandColor = "#FFFFFF"; t.LineBandOpacity = 0.9; t.TextColor = "#000000"; t.Shadow = false; });
Slide("franja-ancho-completo-translucida", "Franja de borde a borde a medio tono.", Four, "Franjas",
    tweak: t => { t.LineBand = LineBand.FullWidth; t.LineBandColor = "#10285A"; t.LineBandOpacity = 0.5; });
Slide("franja-biblia-dos-versiones", "Franjas detrás de las dos versiones.", John316, "Juan 3:16 · PRUEBA / OTRA", John316Other,
    tweak: t => { t.LineBand = LineBand.Text; t.LineBandColor = "#000000"; t.LineBandOpacity = 0.8; });

// ── Alignment and margins ────────────────────────────────────────

Slide("alineacion-izquierda", "Texto alineado a la izquierda.", Four, "Alineación",
    tweak: t => t.AlignH = HAlign.Left);
Slide("alineacion-centro", "Texto centrado.", Four, "Alineación", tweak: t => t.AlignH = HAlign.Center);
Slide("alineacion-derecha", "Texto alineado a la derecha.", Four, "Alineación", tweak: t => t.AlignH = HAlign.Right);
Slide("alineacion-arriba-izquierda", "Arriba y a la izquierda.", Two, "Alineación",
    tweak: t => { t.AlignH = HAlign.Left; t.AlignV = VAlign.Top; });
Slide("alineacion-abajo-derecha", "Abajo y a la derecha, con la cita arriba a la izquierda.", Two, "Alineación",
    tweak: t => { t.AlignH = HAlign.Right; t.AlignV = VAlign.Bottom; t.CaptionPosition = CaptionPosition.TopLeft; });
Slide("margenes-grandes", "Márgenes de 300 y 200: el texto tiene poco ancho.", Long4, "Márgenes",
    tweak: t => { t.MarginHorizontal = 300; t.MarginVertical = 200; });
Slide("margenes-chicos", "Márgenes de 40 y 30: el texto casi toca el borde.", Long4, "Márgenes",
    tweak: t => { t.MarginHorizontal = 40; t.MarginVertical = 30; });
Slide("caja-de-texto-del-tema", "El tema define una caja de texto en vez de márgenes.", Long4, "Caja",
    tweak: t => { t.BoxX = 300; t.BoxY = 200; t.BoxWidth = 1320; t.BoxHeight = 560; });
Slide("caja-propia-de-la-diapositiva", "La diapositiva trae su propia caja de texto.", Long4, "Caja",
    over: new SlideOverride(BoxX: 960, BoxY: 100, BoxWidth: 900, BoxHeight: 700, AlignH: HAlign.Left));

// ── Announcement, countdown, quick text, logo, extra boxes ───────

Write("anuncio-corto", "Aviso al pie de una línea, sobre una letra.", c =>
{
    c.Slide = new RenderSlide { MainText = Two, Caption = "Aviso" };
    c.Overlay = new RenderOverlay { Text = "Hoy después del culto: almuerzo en el salón" };
});
Write("anuncio-largo-dos-renglones", "Aviso al pie largo: se parte en dos renglones.", c =>
{
    c.Slide = new RenderSlide { MainText = Two, Caption = "Aviso" };
    c.Overlay = new RenderOverlay
    {
        Text = "Esta semana no hay reunión de jóvenes. El próximo viernes nos encontramos a las 19:30 en el templo para la vigilia de oración y ayuno",
    };
});
Write("anuncio-grande", "Aviso al pie con letra de 90.", c =>
{
    c.Slide = new RenderSlide { MainText = Two, Caption = "Aviso" };
    c.Overlay = new RenderOverlay { Text = "Bautismos el domingo próximo", FontSize = 90 };
});
Write("cuenta-regresiva", "Cuenta regresiva con su título.", c =>
{
    c.Countdown = new RenderCountdown { Minutes = 5.5, Heading = "Empezamos en" };
});
Write("cuenta-regresiva-sin-titulo", "Cuenta regresiva sin título.", c =>
{
    c.Countdown = new RenderCountdown { Minutes = 12.5, Heading = "" };
});
Write("cuenta-regresiva-terminada", "La cuenta llegó a cero y muestra el mensaje.", c =>
{
    c.Countdown = new RenderCountdown { Minutes = -1, Heading = "Empezamos en", FinishedMessage = "¡Bienvenidos a la casa de Dios!" };
});
Slide("texto-rapido-corto", "Texto rápido: una frase escrita en el momento, sin cita.", "Bienvenidos a todos");
Slide("texto-rapido-largo", "Texto rápido largo, escrito de corrido.",
    "Les pedimos que apaguen sus teléfonos y que los padres de los niños de la escuela dominical pasen a recogerlos al terminar el culto de hoy");
Write("logo-texto-corto", "Logo hecho de texto, una línea.", c =>
{
    c.State = OutputState.Logo;
    c.Logo = new RenderLogo { Text = "Iglesia Nueva Vida" };
});
Write("logo-texto-largo", "Logo hecho de texto que se parte en renglones.", c =>
{
    c.State = OutputState.Logo;
    c.Logo = new RenderLogo { Text = "Iglesia Evangélica Bautista Nueva Vida de Villa Mercedes", FontSize = 110 };
});
Slide("cuadros-de-texto-extra", "La diapositiva trae dos cuadros de texto sueltos.", Two, "Extra",
    over: new SlideOverride(Boxes:
    [
        new SlideTextBox("Serie: El Padre nuestro", 60, 60, 700, 120, FontSize: 44, AlignH: HAlign.Left),
        new SlideTextBox("Domingo 18 de noviembre", 1160, 880, 700, 120, FontSize: 40, Bold: true, AlignH: HAlign.Right),
    ]));

// ── Stage ────────────────────────────────────────────────────────

void Stage(string id, string description, StageBox box) =>
    Write(id, description, c => c.StageBox = box);

Stage("escenario-letra", "Cuadro del escenario con dos renglones.",
    new StageBox { Kind = StageBoxKind.FixedText, X = 100, Y = 100, Width = 1000, Height = 360, FontSize = 70, Align = StageAlign.Left, Text = Two });
Stage("escenario-texto-largo", "Cuadro del escenario con un texto largo: se parte y se achica para entrar.",
    new StageBox { Kind = StageBoxKind.FixedText, X = 100, Y = 100, Width = 900, Height = 300, FontSize = 80, Align = StageAlign.Center, Text = LongLine });
Stage("escenario-cuenta-regresiva", "Cuadro de cuenta regresiva del escenario (muestra 5:00 de ejemplo).",
    new StageBox { Kind = StageBoxKind.Countdown, X = 560, Y = 300, Width = 800, Height = 400, FontSize = 220, Align = StageAlign.Center });

Console.WriteLine($"{count} cases written in {folder}");
return 0;
