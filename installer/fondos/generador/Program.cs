// Dibuja los fondos en video que vienen con el instalador. Todo sale de
// fórmulas (degradés, ruido, círculos), sin fotos ni filmaciones de nadie, así
// que los fondos son del proyecto y se reparten con él.
//
// Uso:  dotnet run -c Release --project installer\fondos\generador -- <carpeta> [segundos] [nombre]
// Arma cada video con ffmpeg (tiene que estar en el PATH). Con un nombre, sólo
// ése. Cada dibujo recibe en qué punto del bucle está (de 0 a 1), y todo lo que
// se mueve da vueltas enteras en ese tiempo: el último cuadro empalma con el
// primero y el bucle no se nota.

using System.Diagnostics;

var outDir = args.Length > 0 ? args[0] : ".";
var seconds = args.Length > 1 ? int.Parse(args[1]) : 20;
var only = args.Length > 2 ? args[2] : null;
Directory.CreateDirectory(outDir);

var videos = new (string Name, Func<float, Img> Frame)[]
{
    ("Amanecer", Designs.Sunrise),
    ("Aurora", Designs.Aurora),
    ("Luces azules", t => Designs.Bokeh(Col.Hex("0A1A33"), Col.Hex("02050C"),
        [Col.Hex("5AB8FF"), Col.Hex("3D7BFF"), Col.Hex("8FE3FF"), Col.Hex("9A7BFF")], 41, t)),
    ("Luces doradas", t => Designs.Bokeh(Col.Hex("2A1708"), Col.Hex("0B0603"),
        [Col.Hex("FFC870"), Col.Hex("FFB050"), Col.Hex("FFE2A8"), Col.Hex("F59A3C")], 31, t)),
    ("Niebla", Designs.Mist),
    ("Noche estrellada", Designs.StarryNight),
    ("Ondas violeta", Designs.VioletWaves),
    ("Profundo", Designs.Deep),
    ("Rayos de luz", Designs.LightRays),
    ("Tres cruces", Designs.ThreeCrosses),
};

// Fondos extra: no vienen con el instalador. Se arman con «--paquete» como
// tercer argumento y se reparten como un .zip aparte (Archivo > Agregar paquete
// de fondos). Mismas recetas, otros colores.
var extras = new (string Name, Func<float, Img> Frame)[]
{
    ("Luces rojas", t => Designs.Bokeh(Col.Hex("33090F"), Col.Hex("0D0204"),
        [Col.Hex("FF5A5A"), Col.Hex("E02D4A"), Col.Hex("FF9A8A"), Col.Hex("B81E3C")], 52, t)),
    ("Luces rosas", t => Designs.Bokeh(Col.Hex("311028"), Col.Hex("0C040A"),
        [Col.Hex("FF8CC8"), Col.Hex("F25AA6"), Col.Hex("FFC2E0"), Col.Hex("C65AF2")], 63, t)),
    ("Luces violeta", t => Designs.Bokeh(Col.Hex("1D0F3A"), Col.Hex("07030F"),
        [Col.Hex("B38CFF"), Col.Hex("8456F2"), Col.Hex("D9BFFF"), Col.Hex("6A6AFF")], 74, t)),
    ("Luces verdes", t => Designs.Bokeh(Col.Hex("0A2A1C"), Col.Hex("020C07"),
        [Col.Hex("6BFFB0"), Col.Hex("2DD68A"), Col.Hex("B6FFD9"), Col.Hex("49C7C0")], 85, t)),
    ("Luces turquesa", t => Designs.Bokeh(Col.Hex("08292E"), Col.Hex("020B0D"),
        [Col.Hex("5AF0E6"), Col.Hex("2DB8D6"), Col.Hex("A8FFF5"), Col.Hex("5AA8FF")], 96, t)),
    ("Luces blancas", t => Designs.Bokeh(Col.Hex("1B2434"), Col.Hex("05070B"),
        [Col.Hex("FFFFFF"), Col.Hex("DCE8FF"), Col.Hex("FFF3D9"), Col.Hex("BFD4FF")], 107, t)),
    ("Niebla violeta", t => Designs.Mist(t, "0E0A1A", "34264F", "A590C8")),
    ("Niebla verde", t => Designs.Mist(t, "07120E", "1F3F34", "7FB8A0")),
    ("Niebla dorada", t => Designs.Mist(t, "140D07", "4A361F", "C8A878")),
};

if (only == "--paquete")
{
    foreach (var (name, frame) in extras)
        Video.Render(Path.Combine(outDir, name + ".mp4"), seconds, frame);
    return;
}

foreach (var (name, frame) in videos)
{
    if (only is null || string.Equals(only, name, StringComparison.OrdinalIgnoreCase))
        Video.Render(Path.Combine(outDir, name + ".mp4"), seconds, frame);
}

static class Designs
{
    const int W = 1920, H = 1080;
    const float Aspect = (float)W / H;

    /// <summary>
    /// A point that goes once round a circle of radius <paramref name="r"/> per
    /// loop. Added to where a noise is read, the noise changes along the loop
    /// and comes back to where it started.
    /// </summary>
    static (float X, float Y) Orbit(float t, float r) => (r * MathF.Cos(MathF.Tau * t), r * MathF.Sin(MathF.Tau * t));

    /// <summary>A gentle swell, once (or <paramref name="times"/>) per loop: 1 at the start.</summary>
    static float Breath(float t, float depth, int times = 1, float phase = 0) =>
        1 + depth * (MathF.Sin(MathF.Tau * times * t + phase) - MathF.Sin(phase));

    public static Img Sunrise(float t)
    {
        var img = new Img(W, H);
        var drift = Orbit(t, 0.35f);
        float sun = Breath(t, 0.06f);
        img.Fill((u, v) =>
        {
            var sky = M.Grad(v, (0f, Col.Hex("0A1433")), (0.42f, Col.Hex("2B2F6B")), (0.68f, Col.Hex("A04E6E")),
                (0.86f, Col.Hex("EE8549")), (1f, Col.Hex("FFD07A")));

            // The sun, just under the bottom edge.
            float dx = (u - 0.5f) * Aspect, dy = v - 1.06f;
            float d2 = dx * dx + dy * dy;
            sky += Col.Hex("FFB866") * ((MathF.Exp(-d2 / 0.09f) * 0.5f + MathF.Exp(-d2 / 0.012f) * 0.3f) * sun);

            // Long thin clouds, lit from below.
            float n = Noise.Fbm(u * 3.2f + drift.X, v * 16f + drift.Y * 0.25f, 6, 11);
            float band = M.Smooth(0.3f, 0.5f, v) * (1 - M.Smooth(0.8f, 0.92f, v));
            float cloud = M.Smooth(0.52f, 0.78f, n) * band;
            var cloudCol = M.Grad(v, (0.3f, Col.Hex("5E4A86")), (0.85f, Col.Hex("FF9C78")));
            return Col.Lerp(sky, cloudCol, cloud * 0.6f);
        });
        img.Vignette(0.35f);
        return img;
    }

    public static Img StarryNight(float t)
    {
        var img = new Img(W, H);
        var drift = Orbit(t, 0.25f);
        img.Fill((u, v) =>
        {
            var sky = M.Grad(v, (0f, Col.Hex("02040D")), (0.6f, Col.Hex("07132D")), (1f, Col.Hex("0F2448")));

            // A faint band of the galaxy, across the sky.
            float across = v - (0.15f + 0.55f * u);
            float bandMask = MathF.Exp(-across * across / 0.03f);
            float neb = Noise.Fbm(u * 5 + 3 + drift.X, v * 3 + drift.Y, 6, 21);
            var nebCol = Col.Lerp(Col.Hex("3B2170"), Col.Hex("1E5592"), Noise.Fbm(u * 2, v * 2, 4, 22));
            return Col.Lerp(sky, nebCol, M.Smooth(0.42f, 0.8f, neb) * bandMask * 0.45f);
        });
        Stars(img, 1900, 5, 1f, t);
        img.Vignette(0.3f);
        return img;
    }

    /// <summary>Stars that twinkle, each at its own pace (a whole number of times per loop).</summary>
    static void Stars(Img img, int count, int seed, float strength, float t)
    {
        var rng = new Random(seed);
        var twinkle = new Random(seed + 500);
        for (int i = 0; i < count; i++)
        {
            float x = rng.NextSingle() * W, y = rng.NextSingle() * H;
            float b = MathF.Pow(rng.NextSingle(), 7) * 0.9f + 0.06f;
            float size = 0.45f + b * 1.3f;
            var tint = Col.Lerp(Col.Hex("BFD4FF"), Col.Hex("FFE6C4"), rng.NextSingle());
            b *= Breath(t, 0.3f, 1 + twinkle.Next(3), twinkle.NextSingle() * MathF.Tau);
            img.Splat(x, y, size, tint * (b * 1.6f * strength));
            if (b > 0.6f)
                img.Splat(x, y, 7f, tint * (0.05f * strength));
        }
    }

    public static Img Bokeh(Col center, Col edge, Col[] palette, int seed, float t)
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            float dx = (u - 0.5f) * Aspect, dy = v - 0.55f;
            return Col.Lerp(center, edge, M.Clamp01(MathF.Sqrt(dx * dx + dy * dy) / 1.05f));
        });

        var rng = new Random(seed);
        var drift = new Random(seed + 500);
        // Far, small and blurred; then nearer and bigger; then a few huge and faint.
        // The nearer, the more it drifts.
        (int Count, float RMin, float RMax, float AMin, float AMax, int Blur, float Drift)[] layers =
        [
            (90, 8, 34, 0.25f, 0.6f, 5, 30),
            (45, 36, 100, 0.12f, 0.32f, 3, 55),
            (12, 120, 230, 0.05f, 0.12f, 1, 90),
        ];
        foreach (var layer in layers)
        {
            var lights = new Img(W, H);
            for (int i = 0; i < layer.Count; i++)
            {
                float u = rng.NextSingle();
                // Along a diagonal, so the lights make a shape instead of noise.
                float v = 0.25f + 0.45f * u + M.Gauss(rng) * 0.2f;
                float r = layer.RMin + rng.NextSingle() * (layer.RMax - layer.RMin);
                float a = layer.AMin + rng.NextSingle() * (layer.AMax - layer.AMin);

                // Each light goes round a small oval once (or twice) per loop, and breathes.
                float amp = layer.Drift * (0.5f + drift.NextSingle()), phase = drift.NextSingle() * MathF.Tau;
                float turns = 1 + drift.Next(2), breath = drift.NextSingle() * MathF.Tau;
                float angle = MathF.Tau * turns * t + phase;
                float x = u * W + amp * (MathF.Sin(angle) - MathF.Sin(phase));
                float y = v * H + amp * 0.6f * (MathF.Cos(angle) - MathF.Cos(phase));
                lights.Disk(x, y, r, palette[rng.Next(palette.Length)] * (a * Breath(t, 0.3f, 2, breath)));
            }
            lights.Blur(layer.Blur);
            img.AddImage(lights);
        }
        img.Vignette(0.45f);
        return img;
    }

    public static Img LightRays(float t)
    {
        var img = new Img(W, H);
        var sway = Orbit(t, 0.6f);
        var shimmer = Orbit(2 * t, 0.5f);
        float haze = Breath(t, 0.12f);
        img.Fill((u, v) =>
        {
            var col = M.Grad(v, (0f, Col.Hex("16264A")), (0.6f, Col.Hex("0A1226")), (1f, Col.Hex("03050C")));
            float dx = (u - 0.5f) * Aspect, dy = v + 0.25f;
            float angle = MathF.Atan2(dx, dy);
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            float rays = 0.6f * Noise.Value(angle * 16 + sway.X, 0.5f + sway.Y, 51)
                         + 0.4f * Noise.Value(angle * 37 + shimmer.X, 3.5f + shimmer.Y, 52);
            rays = M.Smooth(0.45f, 0.9f, rays);
            float cone = MathF.Exp(-angle * angle / 0.35f);
            float fall = MathF.Exp(-dist * 1.2f);
            float glow = MathF.Exp(-((u - 0.5f) * (u - 0.5f) * 4 + v * v * 7)) * haze;
            return col + Col.Hex("FFE3B4") * (rays * cone * fall * 0.55f) + Col.Hex("FFD9A0") * (glow * 0.16f);
        });
        img.Blur(2);
        img.Vignette(0.3f);
        return img;
    }

    public static Img VioletWaves(float t)
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            var col = Col.Lerp(Col.Hex("12041D"), Col.Hex("3A0B46"), M.Clamp01(u * 0.55f + v * 0.45f));
            for (int i = 0; i < 8; i++)
            {
                // Each wave travels a whole number of times per loop, so it closes.
                float freq = 0.6f + i * 0.12f, phase = i * 0.8f + MathF.Tau * (1 + i % 2) * t;
                float center = 0.66f + (i - 3.5f) * 0.022f - 0.12f * (u - 0.5f)
                               + 0.012f * (MathF.Sin(MathF.Tau * t + i) - MathF.Sin(i));
                float y = center + (0.07f + 0.015f * (i % 3)) * MathF.Sin(u * freq * MathF.Tau + phase)
                          + 0.025f * MathF.Sin(u * freq * 5.1f + phase * 2);
                float thick = 0.0025f + 0.0015f * (i % 3);
                float d = (v - y) / thick;
                float line = MathF.Exp(-d * d);
                float glow = MathF.Exp(-MathF.Abs(v - y) / 0.05f);
                col += Col.Lerp(Col.Hex("FF4FA8"), Col.Hex("8C6BFF"), i / 7f) * (line * 0.45f + glow * 0.07f);
            }
            return col;
        });
        img.Vignette(0.4f);
        return img;
    }

    public static Img Aurora(float t)
    {
        var img = new Img(W, H);
        var foot = Orbit(t, 0.3f);
        var fold = Orbit(t, 0.6f);
        var sway = Orbit(t, 2f);
        var glow = Orbit(t, 0.5f);
        img.Fill((u, v) =>
        {
            var sky = M.Grad(v, (0f, Col.Hex("01040B")), (0.7f, Col.Hex("030E18")), (1f, Col.Hex("04141C")));
            // The curtain folds: its foot wanders up and down, and the folds
            // themselves bend the streaks.
            float yc = 0.5f + 0.3f * (Noise.Fbm(u * 2.5f + foot.X, 0.3f + foot.Y, 3, 61) - 0.5f) + 0.06f * MathF.Sin(u * 9 + 1);
            float bend = (Noise.Value(u * 4 + fold.X, v * 2 + fold.Y, 66) - 0.5f) * 0.08f;
            float streak = Noise.Fbm((u + bend) * 70 + sway.X, v * 1.4f + sway.Y * 0.25f, 3, 62);
            float profile = v < yc ? MathF.Exp(-(yc - v) / 0.24f) : MathF.Exp(-MathF.Pow((v - yc) / 0.09f, 2));
            float strength = (0.3f + 0.7f * streak) * profile * M.Smooth(0.15f, 0.7f, Noise.Fbm(u * 3 + glow.X, 1 + glow.Y, 3, 63));
            float height = M.Clamp01((yc - v) / 0.3f);
            var light = Col.Lerp(Col.Hex("2BEA9A"), Col.Hex("7A4BD8"), height);
            return sky + light * (strength * 0.7f);
        });
        Stars(img, 700, 64, 0.6f, t);
        img.Blur(1);

        // Distant hills, black against the sky.
        img.Fill((u, v, before) =>
        {
            float ridge = 0.87f + 0.05f * (Noise.Fbm(u * 4, 0.5f, 5, 65) - 0.5f) * 2;
            float cover = M.Smooth(ridge - 0.6f / H, ridge + 0.6f / H, v);
            return Col.Lerp(before, Col.Hex("01050A"), cover);
        });
        img.Vignette(0.3f);
        return img;
    }

    public static Img Deep(float t)
    {
        var img = new Img(W, H);
        var warpDrift = Orbit(t, 0.4f);
        var ripple1 = Orbit(t, 1f);
        var ripple2 = Orbit(-t, 1f);
        var shaftDrift = Orbit(t, 0.6f);
        img.Fill((u, v) =>
        {
            var col = M.Grad(v, (0f, Col.Hex("0B4A63")), (0.5f, Col.Hex("05273A")), (1f, Col.Hex("01070D")));
            // Two ripples of light crossing make the net of the sea floor's caustics.
            float warp = Noise.Fbm(u * 2 + warpDrift.X, v * 2 + warpDrift.Y, 3, 71) * 1.2f;
            float c1 = 1 - MathF.Abs(Noise.Value(u * 7 + warp + ripple1.X, v * 5 + ripple1.Y, 72) * 2 - 1);
            float c2 = 1 - MathF.Abs(Noise.Value(u * 6 + ripple2.X, v * 7 + warp + ripple2.Y, 75) * 2 - 1);
            float c = MathF.Pow(c1, 6) * 0.5f + MathF.Pow(c2, 6) * 0.5f;
            float top = (1 - v) * (1 - v);
            float dx = (u - 0.3f) * Aspect, dy = v + 0.3f;
            float angle = MathF.Atan2(dx, dy);
            float shafts = M.Smooth(0.5f, 0.95f, Noise.Value(angle * 22 + shaftDrift.X, 0.5f + shaftDrift.Y, 73))
                           * MathF.Exp(-MathF.Sqrt(dx * dx + dy * dy) * 1.3f);
            return col + Col.Hex("8FE8FF") * (c * top * 0.18f + shafts * 0.25f);
        });
        img.Blur(4);

        // Specks floating up: each rises one screen per loop and comes in again from below.
        var rng = new Random(74);
        for (int i = 0; i < 160; i++)
        {
            float x0 = rng.NextSingle() * W, y0 = rng.NextSingle() * H;
            float size = 0.8f + rng.NextSingle() * 1.5f, a = 0.05f + rng.NextSingle() * 0.15f;
            float y = ((y0 - t * H) % H + H) % H;
            float x = x0 + 12 * MathF.Sin(MathF.Tau * t + i);
            img.Splat(x, y, size, Col.Hex("A8F0FF") * a);
        }
        img.Vignette(0.4f);
        return img;
    }

    public static Img Mist(float t) => Mist(t, "080D16", "23384A", "7F9BB0");

    public static Img Mist(float t, string dark, string mid, string light)
    {
        var img = new Img(W, H);
        var slow = Orbit(t, 0.3f);
        var drift = Orbit(t, 0.8f);
        img.Fill((u, v) =>
        {
            float px = u * 1.6f * Aspect, py = v * 1.6f;
            float warp = Noise.Fbm(px * 0.7f + slow.X, py * 0.7f + slow.Y, 3, 91) * 0.8f;
            float f = Noise.Fbm(px + warp + drift.X, py * 1.6f + warp + drift.Y, 6, 92);
            float dx = (u - 0.5f) * Aspect, dy = v - 0.45f;
            float glow = MathF.Exp(-(dx * dx + dy * dy) / 0.35f);

            var col = Col.Lerp(Col.Hex(dark), Col.Hex(mid), glow);
            return Col.Lerp(col, Col.Hex(light), M.Smooth(0.4f, 0.8f, f) * (0.2f + 0.25f * glow));
        });
        img.Blur(3);
        img.Vignette(0.45f);
        return img;
    }

    public static Img ThreeCrosses(float t)
    {
        var img = new Img(W, H);
        var drift = Orbit(t, 0.5f);
        float sun = Breath(t, 0.06f);
        img.Fill((u, v) =>
        {
            var sky = M.Grad(v, (0f, Col.Hex("1A0B2E")), (0.35f, Col.Hex("55204C")), (0.6f, Col.Hex("BC4639")),
                (0.76f, Col.Hex("F08A39")), (0.84f, Col.Hex("FFC766")));
            float dx = (u - 0.64f) * Aspect, dy = v - 0.8f;
            float d2 = dx * dx + dy * dy;
            sky += Col.Hex("FFD27A") * ((MathF.Exp(-d2 / 0.05f) * 0.45f + MathF.Exp(-d2 / 0.004f) * 0.35f) * sun);
            float streaks = M.Smooth(0.55f, 0.8f, Noise.Fbm(u * 2.2f + drift.X, v * 30 + drift.Y * 0.3f, 5, 111))
                            * M.Smooth(0.2f, 0.4f, v) * (1 - M.Smooth(0.68f, 0.78f, v));
            return Col.Lerp(sky, Col.Lerp(Col.Hex("3A1A44"), Col.Hex("FF8A5C"), v), streaks * 0.55f);
        });

        static float Hill(float u) => 0.85f - 0.075f * MathF.Exp(-MathF.Pow((u - 0.64f) / 0.17f, 2)) + 0.012f * (Noise.Fbm(u * 18, 0.5f, 3, 112) - 0.5f);

        // The cross stands on the hill: its foot a little into the ground.
        (float U, float Height, float Post)[] crosses = [(0.64f, 0.19f, 12f), (0.575f, 0.13f, 8f), (0.705f, 0.13f, 8f)];
        var ink = Col.Hex("0B0610");
        img.Fill((u, v, before) =>
        {
            float x = u * W, y = v * H;
            float ground = Hill(u) * H;
            float cover = M.Smooth(ground - 0.8f, ground + 0.8f, y);
            foreach (var (cu, height, post) in crosses)
            {
                float footY = Hill(cu) * H + 6, top = footY - height * H;
                float cx = cu * W;
                float arm = height * H * 0.36f, armY = top + height * H * 0.27f;
                cover = MathF.Max(cover, M.Box(x, y, cx - post / 2, top, cx + post / 2, footY));
                cover = MathF.Max(cover, M.Box(x, y, cx - arm, armY - post / 2, cx + arm, armY + post / 2));
            }
            return Col.Lerp(before, ink, cover);
        });
        img.Vignette(0.3f);
        return img;
    }
}

static class Video
{
    const int Fps = 30;

    /// <summary>Draws every frame of the loop and hands them to ffmpeg, which makes the MP4.</summary>
    public static void Render(string path, int seconds, Func<float, Img> frame)
    {
        var watch = Stopwatch.StartNew();
        var frames = seconds * Fps;
        var ffmpeg = Process.Start(new ProcessStartInfo("ffmpeg",
            $"-loglevel error -y -f rawvideo -pix_fmt rgb24 -s 1920x1080 -r {Fps} -i - " +
            "-c:v libx264 -preset slow -crf 22 -x264-params aq-mode=3 -pix_fmt yuv420p " +
            $"-movflags +faststart -an \"{path}\"")
        {
            RedirectStandardInput = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("No se encontró ffmpeg.");

        using (var input = ffmpeg.StandardInput.BaseStream)
        {
            for (int f = 0; f < frames; f++)
            {
                var img = frame((float)f / frames);
                // The same dither on every frame: it stops the banding without
                // making the still parts of the picture change.
                img.Dither(7);
                input.Write(img.ToBytes());
            }
        }

        ffmpeg.WaitForExit();
        if (ffmpeg.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg no pudo armar {path}");
        Console.WriteLine($"{Path.GetFileName(path)}  ({watch.Elapsed.TotalSeconds:0} s, {new FileInfo(path).Length / 1024 / 1024.0:0.0} MB)");
    }
}

readonly record struct Col(float R, float G, float B)
{
    public static Col Hex(string hex) => new(
        Convert.ToInt32(hex[..2], 16) / 255f, Convert.ToInt32(hex[2..4], 16) / 255f, Convert.ToInt32(hex[4..6], 16) / 255f);

    public static Col Lerp(Col a, Col b, float t) => new(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
    public static Col operator +(Col a, Col b) => new(a.R + b.R, a.G + b.G, a.B + b.B);
    public static Col operator *(Col a, float k) => new(a.R * k, a.G * k, a.B * k);
}

static class M
{
    public static float Clamp01(float x) => x < 0 ? 0 : x > 1 ? 1 : x;

    public static float Smooth(float a, float b, float x)
    {
        float t = Clamp01((x - a) / (b - a));
        return t * t * (3 - 2 * t);
    }

    public static Col Grad(float t, params (float At, Col Col)[] stops)
    {
        if (t <= stops[0].At) return stops[0].Col;
        for (int i = 1; i < stops.Length; i++)
            if (t <= stops[i].At)
                return Col.Lerp(stops[i - 1].Col, stops[i].Col, Smooth(stops[i - 1].At, stops[i].At, t));
        return stops[^1].Col;
    }

    /// <summary>How much of the pixel a rectangle covers, with a soft one-pixel edge.</summary>
    public static float Box(float x, float y, float x0, float y0, float x1, float y1) =>
        Clamp01(MathF.Min(MathF.Min(x - x0, x1 - x), MathF.Min(y - y0, y1 - y)) + 0.5f);

    public static float Gauss(Random rng) =>
        MathF.Sqrt(-2 * MathF.Log(1 - rng.NextSingle())) * MathF.Cos(MathF.Tau * rng.NextSingle());
}

static class Noise
{
    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177;
            return (h ^ (h >> 16)) / (float)uint.MaxValue;
        }
    }

    /// <summary>Smooth value noise, 0 to 1.</summary>
    public static float Value(float x, float y, int seed)
    {
        int x0 = (int)MathF.Floor(x), y0 = (int)MathF.Floor(y);
        float fx = x - x0, fy = y - y0;
        float sx = fx * fx * fx * (fx * (fx * 6 - 15) + 10), sy = fy * fy * fy * (fy * (fy * 6 - 15) + 10);
        float a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed), c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
        return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
    }

    public static float Fbm(float x, float y, int octaves, int seed)
    {
        float sum = 0, amp = 0.5f, total = 0;
        for (int i = 0; i < octaves; i++)
        {
            sum += Value(x, y, seed + i * 7) * amp;
            total += amp;
            x = x * 2.03f + 1.7f;
            y = y * 2.03f + 9.2f;
            amp *= 0.5f;
        }
        return sum / total;
    }
}

sealed class Img(int w, int h)
{
    public readonly int W = w, H = h;
    readonly float[] r = new float[w * h], g = new float[w * h], b = new float[w * h];

    public void Fill(Func<float, float, Col> paint) => Fill((u, v, _) => paint(u, v));

    public void Fill(Func<float, float, Col, Col> paint) => Parallel.For(0, H, y =>
    {
        for (int x = 0; x < W; x++)
        {
            int i = y * W + x;
            var c = paint((x + 0.5f) / W, (y + 0.5f) / H, new Col(r[i], g[i], b[i]));
            r[i] = c.R; g[i] = c.G; b[i] = c.B;
        }
    });

    void Add(int x, int y, Col c)
    {
        if (x < 0 || y < 0 || x >= W || y >= H) return;
        int i = y * W + x;
        r[i] += c.R; g[i] += c.G; b[i] += c.B;
    }

    public void AddImage(Img other)
    {
        for (int i = 0; i < r.Length; i++) { r[i] += other.r[i]; g[i] += other.g[i]; b[i] += other.b[i]; }
    }

    /// <summary>A soft round dot, added: a star or a speck of light.</summary>
    public void Splat(float cx, float cy, float size, Col c)
    {
        int reach = (int)MathF.Ceiling(size * 3);
        for (int y = (int)cy - reach; y <= (int)cy + reach; y++)
            for (int x = (int)cx - reach; x <= (int)cx + reach; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                Add(x, y, c * MathF.Exp(-(dx * dx + dy * dy) / (2 * size * size)));
            }
    }

    /// <summary>An out-of-focus light: a disk with a brighter rim, added.</summary>
    public void Disk(float cx, float cy, float radius, Col c)
    {
        int reach = (int)MathF.Ceiling(radius + 2);
        for (int y = (int)cy - reach; y <= (int)cy + reach; y++)
            for (int x = (int)cx - reach; x <= (int)cx + reach; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float d = MathF.Sqrt(dx * dx + dy * dy);
                float inside = 1 - M.Smooth(radius - 2.5f, radius, d);
                float rim = 0.7f + 0.3f * M.Smooth(radius * 0.55f, radius, d);
                if (inside > 0) Add(x, y, c * (inside * rim));
            }
    }

    /// <summary>Three box blurs in a row: close enough to a gaussian.</summary>
    public void Blur(int radius)
    {
        if (radius <= 0) return;
        foreach (var channel in new[] { r, g, b })
            for (int pass = 0; pass < 3; pass++)
            {
                BoxRows(channel, radius);
                BoxColumns(channel, radius);
            }
    }

    void BoxRows(float[] ch, int rad) => Parallel.For(0, H, y =>
    {
        var line = new float[W];
        Array.Copy(ch, y * W, line, 0, W);
        float sum = 0;
        for (int k = -rad; k <= rad; k++) sum += line[Math.Clamp(k, 0, W - 1)];
        for (int x = 0; x < W; x++)
        {
            ch[y * W + x] = sum / (2 * rad + 1);
            sum += line[Math.Min(x + rad + 1, W - 1)] - line[Math.Max(x - rad, 0)];
        }
    });

    void BoxColumns(float[] ch, int rad) => Parallel.For(0, W, x =>
    {
        var line = new float[H];
        for (int y = 0; y < H; y++) line[y] = ch[y * W + x];
        float sum = 0;
        for (int k = -rad; k <= rad; k++) sum += line[Math.Clamp(k, 0, H - 1)];
        for (int y = 0; y < H; y++)
        {
            ch[y * W + x] = sum / (2 * rad + 1);
            sum += line[Math.Min(y + rad + 1, H - 1)] - line[Math.Max(y - rad, 0)];
        }
    });

    /// <summary>Darker corners, so the eye stays on the middle, where the words go.</summary>
    public void Vignette(float strength) => Fill((u, v, c) =>
    {
        float dx = (u - 0.5f) * 1.6f, dy = (v - 0.5f) * 1.6f;
        return c * (1 - strength * M.Smooth(0.35f, 1.15f, MathF.Sqrt(dx * dx + dy * dy)));
    });

    /// <summary>A touch of noise under one step of 255, so the gradients don't band.</summary>
    public void Dither(int seed)
    {
        var rng = new Random(seed);
        for (int i = 0; i < r.Length; i++)
        {
            float n = (rng.NextSingle() + rng.NextSingle() - 1) / 255f;
            r[i] += n; g[i] += n; b[i] += n;
        }
    }

    public byte[] ToBytes()
    {
        var bytes = new byte[r.Length * 3];
        for (int i = 0; i < r.Length; i++)
        {
            bytes[i * 3] = (byte)MathF.Round(M.Clamp01(r[i]) * 255);
            bytes[i * 3 + 1] = (byte)MathF.Round(M.Clamp01(g[i]) * 255);
            bytes[i * 3 + 2] = (byte)MathF.Round(M.Clamp01(b[i]) * 255);
        }
        return bytes;
    }
}
