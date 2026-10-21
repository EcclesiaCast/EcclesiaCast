// Dibuja los fondos de ejemplo que vienen con el instalador. Todo sale de
// fórmulas (degradés, ruido, círculos), sin fotos de nadie, así que los fondos
// son del proyecto y se reparten con él.
//
// Uso:  dotnet run --project installer\fondos\generador -- <carpeta>
// Deja un .rgb crudo por fondo; installer\fondos\generar.ps1 los pasa a JPG.

using System.Diagnostics;

var outDir = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outDir);

var designs = new (string Name, Func<Img> Draw)[]
{
    ("Amanecer", Designs.Sunrise),
    ("Noche estrellada", Designs.StarryNight),
    ("Luces doradas", () => Designs.Bokeh(Col.Hex("2A1708"), Col.Hex("0B0603"),
        [Col.Hex("FFC870"), Col.Hex("FFB050"), Col.Hex("FFE2A8"), Col.Hex("F59A3C")], 31)),
    ("Luces azules", () => Designs.Bokeh(Col.Hex("0A1A33"), Col.Hex("02050C"),
        [Col.Hex("5AB8FF"), Col.Hex("3D7BFF"), Col.Hex("8FE3FF"), Col.Hex("9A7BFF")], 41)),
    ("Rayos de luz", Designs.LightRays),
    ("Ondas violeta", Designs.VioletWaves),
    ("Aurora", Designs.Aurora),
    ("Profundo", Designs.Deep),
    ("Cálido", Designs.Warm),
    ("Niebla", Designs.Mist),
    ("Mosaico azul", Designs.LowPoly),
    ("Tres cruces", Designs.ThreeCrosses),
};

for (int n = 0; n < designs.Length; n++)
{
    var (name, draw) = designs[n];
    var watch = Stopwatch.StartNew();
    var img = draw();
    img.Dither(1000 + n);
    File.WriteAllBytes(Path.Combine(outDir, name + ".rgb"), img.ToBytes());
    Console.WriteLine($"{name}  ({watch.ElapsedMilliseconds} ms)");
}

static class Designs
{
    const int W = 1920, H = 1080;
    const float Aspect = (float)W / H;

    public static Img Sunrise()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            var sky = M.Grad(v, (0f, Col.Hex("0A1433")), (0.42f, Col.Hex("2B2F6B")), (0.68f, Col.Hex("A04E6E")),
                (0.86f, Col.Hex("EE8549")), (1f, Col.Hex("FFD07A")));

            // The sun, just under the bottom edge.
            float dx = (u - 0.5f) * Aspect, dy = v - 1.06f;
            float d2 = dx * dx + dy * dy;
            sky += Col.Hex("FFB866") * (MathF.Exp(-d2 / 0.09f) * 0.5f + MathF.Exp(-d2 / 0.012f) * 0.3f);

            // Long thin clouds, lit from below.
            float n = Noise.Fbm(u * 3.2f, v * 16f, 6, 11);
            float band = M.Smooth(0.3f, 0.5f, v) * (1 - M.Smooth(0.8f, 0.92f, v));
            float cloud = M.Smooth(0.52f, 0.78f, n) * band;
            var cloudCol = M.Grad(v, (0.3f, Col.Hex("5E4A86")), (0.85f, Col.Hex("FF9C78")));
            return Col.Lerp(sky, cloudCol, cloud * 0.6f);
        });
        img.Vignette(0.35f);
        return img;
    }

    public static Img StarryNight()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            var sky = M.Grad(v, (0f, Col.Hex("02040D")), (0.6f, Col.Hex("07132D")), (1f, Col.Hex("0F2448")));

            // A faint band of the galaxy, across the sky.
            float across = v - (0.15f + 0.55f * u);
            float bandMask = MathF.Exp(-across * across / 0.03f);
            float neb = Noise.Fbm(u * 5 + 3, v * 3, 6, 21);
            var nebCol = Col.Lerp(Col.Hex("3B2170"), Col.Hex("1E5592"), Noise.Fbm(u * 2, v * 2, 4, 22));
            return Col.Lerp(sky, nebCol, M.Smooth(0.42f, 0.8f, neb) * bandMask * 0.45f);
        });
        Stars(img, 1900, 5, 1f);
        img.Vignette(0.3f);
        return img;
    }

    static void Stars(Img img, int count, int seed, float strength)
    {
        var rng = new Random(seed);
        for (int i = 0; i < count; i++)
        {
            float x = rng.NextSingle() * W, y = rng.NextSingle() * H;
            float b = MathF.Pow(rng.NextSingle(), 7) * 0.9f + 0.06f;
            float size = 0.45f + b * 1.3f;
            var tint = Col.Lerp(Col.Hex("BFD4FF"), Col.Hex("FFE6C4"), rng.NextSingle());
            img.Splat(x, y, size, tint * (b * 1.6f * strength));
            if (b > 0.6f)
                img.Splat(x, y, 7f, tint * (0.05f * strength));
        }
    }

    public static Img Bokeh(Col center, Col edge, Col[] palette, int seed)
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            float dx = (u - 0.5f) * Aspect, dy = v - 0.55f;
            return Col.Lerp(center, edge, M.Clamp01(MathF.Sqrt(dx * dx + dy * dy) / 1.05f));
        });

        var rng = new Random(seed);
        // Far, small and blurred; then nearer and bigger; then a few huge and faint.
        (int Count, float RMin, float RMax, float AMin, float AMax, int Blur)[] layers =
        [
            (90, 8, 34, 0.25f, 0.6f, 5),
            (45, 36, 100, 0.12f, 0.32f, 3),
            (12, 120, 230, 0.05f, 0.12f, 1),
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
                lights.Disk(u * W, v * H, r, palette[rng.Next(palette.Length)] * a);
            }
            lights.Blur(layer.Blur);
            img.AddImage(lights);
        }
        img.Vignette(0.45f);
        return img;
    }

    public static Img LightRays()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            var col = M.Grad(v, (0f, Col.Hex("16264A")), (0.6f, Col.Hex("0A1226")), (1f, Col.Hex("03050C")));
            float dx = (u - 0.5f) * Aspect, dy = v + 0.25f;
            float angle = MathF.Atan2(dx, dy);
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            float rays = 0.6f * Noise.Value(angle * 16, 0.5f, 51) + 0.4f * Noise.Value(angle * 37, 3.5f, 52);
            rays = M.Smooth(0.45f, 0.9f, rays);
            float cone = MathF.Exp(-angle * angle / 0.35f);
            float fall = MathF.Exp(-dist * 1.2f);
            float haze = MathF.Exp(-((u - 0.5f) * (u - 0.5f) * 4 + v * v * 7));
            return col + Col.Hex("FFE3B4") * (rays * cone * fall * 0.55f) + Col.Hex("FFD9A0") * (haze * 0.16f);
        });
        img.Blur(2);
        img.Vignette(0.3f);
        return img;
    }

    public static Img VioletWaves()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            var col = Col.Lerp(Col.Hex("12041D"), Col.Hex("3A0B46"), M.Clamp01(u * 0.55f + v * 0.45f));
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                float freq = 0.6f + i * 0.12f, phase = i * 0.8f;
                float center = 0.66f + (i - 3.5f) * 0.022f - 0.12f * (u - 0.5f);
                float y = center + (0.07f + 0.015f * (i % 3)) * MathF.Sin(u * freq * MathF.Tau + phase)
                          + 0.025f * MathF.Sin(u * freq * 5.1f + phase * 2);
                float thick = 0.0025f + 0.0015f * (i % 3);
                float d = (v - y) / thick;
                float line = MathF.Exp(-d * d);
                float glow = MathF.Exp(-MathF.Abs(v - y) / 0.05f);
                col += Col.Lerp(Col.Hex("FF4FA8"), Col.Hex("8C6BFF"), t) * (line * 0.45f + glow * 0.07f);
            }
            return col;
        });
        img.Vignette(0.4f);
        return img;
    }

    public static Img Aurora()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            var sky = M.Grad(v, (0f, Col.Hex("01040B")), (0.7f, Col.Hex("030E18")), (1f, Col.Hex("04141C")));
            // The curtain folds: its foot wanders up and down, and the folds
            // themselves bend the streaks.
            float yc = 0.5f + 0.3f * (Noise.Fbm(u * 2.5f, 0.3f, 3, 61) - 0.5f) + 0.06f * MathF.Sin(u * 9 + 1);
            float bend = (Noise.Value(u * 4, v * 2, 66) - 0.5f) * 0.08f;
            float streak = Noise.Fbm((u + bend) * 70, v * 1.4f, 3, 62);
            float profile = v < yc ? MathF.Exp(-(yc - v) / 0.24f) : MathF.Exp(-MathF.Pow((v - yc) / 0.09f, 2));
            float strength = (0.3f + 0.7f * streak) * profile * M.Smooth(0.15f, 0.7f, Noise.Fbm(u * 3, 1, 3, 63));
            float height = M.Clamp01((yc - v) / 0.3f);
            var light = Col.Lerp(Col.Hex("2BEA9A"), Col.Hex("7A4BD8"), height);
            return sky + light * (strength * 0.7f);
        });
        Stars(img, 700, 64, 0.6f);
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

    public static Img Deep()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            var col = M.Grad(v, (0f, Col.Hex("0B4A63")), (0.5f, Col.Hex("05273A")), (1f, Col.Hex("01070D")));
            // Two ripples of light crossing make the net of the sea floor's caustics.
            float warp = Noise.Fbm(u * 2, v * 2, 3, 71) * 1.2f;
            float c1 = 1 - MathF.Abs(Noise.Value(u * 7 + warp, v * 5, 72) * 2 - 1);
            float c2 = 1 - MathF.Abs(Noise.Value(u * 6, v * 7 + warp, 75) * 2 - 1);
            float c = MathF.Pow(c1, 6) * 0.5f + MathF.Pow(c2, 6) * 0.5f;
            float top = (1 - v) * (1 - v);
            float dx = (u - 0.3f) * Aspect, dy = v + 0.3f;
            float angle = MathF.Atan2(dx, dy);
            float shafts = M.Smooth(0.5f, 0.95f, Noise.Value(angle * 22, 0.5f, 73)) * MathF.Exp(-MathF.Sqrt(dx * dx + dy * dy) * 1.3f);
            return col + Col.Hex("8FE8FF") * (c * top * 0.18f + shafts * 0.25f);
        });
        img.Blur(4);
        var rng = new Random(74);
        for (int i = 0; i < 160; i++)
            img.Splat(rng.NextSingle() * W, rng.NextSingle() * H, 0.8f + rng.NextSingle() * 1.5f, Col.Hex("A8F0FF") * (0.05f + rng.NextSingle() * 0.15f));
        img.Vignette(0.4f);
        return img;
    }

    public static Img Warm()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            float dx = (u - 0.5f) * Aspect, dy = v - 0.45f;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            var col = M.Grad(d, (0f, Col.Hex("6B4426")), (0.6f, Col.Hex("3A2213")), (1.1f, Col.Hex("140A04")));
            float warp = Noise.Fbm(u * 2, v * 2, 4, 81);
            col *= 0.88f + 0.24f * Noise.Fbm(u * 3 + warp, v * 3 + warp, 6, 82);
            float leak = MathF.Exp(-((u - 0.12f) * (u - 0.12f) * Aspect * Aspect + (v - 0.08f) * (v - 0.08f)) / 0.09f);
            return col + Col.Hex("FFB060") * (leak * 0.22f);
        });
        img.Vignette(0.5f);
        return img;
    }

    public static Img Mist()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            float px = u * 1.6f * Aspect, py = v * 1.6f;
            float warp = Noise.Fbm(px * 0.7f, py * 0.7f, 3, 91) * 0.8f;
            float f = Noise.Fbm(px + warp, py * 1.6f + warp, 6, 92);
            float dx = (u - 0.5f) * Aspect, dy = v - 0.45f;
            float glow = MathF.Exp(-(dx * dx + dy * dy) / 0.35f);

            var col = Col.Lerp(Col.Hex("080D16"), Col.Hex("23384A"), glow);
            return Col.Lerp(col, Col.Hex("7F9BB0"), M.Smooth(0.4f, 0.8f, f) * (0.2f + 0.25f * glow));
        });
        img.Blur(3);
        img.Vignette(0.45f);
        return img;
    }

    public static Img LowPoly()
    {
        var img = new Img(W, H);
        const int nx = 16, ny = 9;
        var rng = new Random(101);
        var pts = new (float X, float Y)[nx + 1, ny + 1];
        float cw = (float)W / nx, ch = (float)H / ny;
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= ny; j++)
            {
                float jx = i is 0 or nx ? 0 : (rng.NextSingle() - 0.5f) * 0.7f * cw;
                float jy = j is 0 or ny ? 0 : (rng.NextSingle() - 0.5f) * 0.7f * ch;
                pts[i, j] = (i * cw + jx, j * ch + jy);
            }

        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
            {
                var a = pts[i, j]; var b = pts[i + 1, j]; var c = pts[i + 1, j + 1]; var d = pts[i, j + 1];
                // Alternate the diagonal so the pattern has no grain.
                var tris = (i + j) % 2 == 0 ? new[] { (a, b, c), (a, c, d) } : new[] { (a, b, d), (b, c, d) };
                foreach (var (p, q, r) in tris)
                {
                    float cx = (p.X + q.X + r.X) / 3 / W, cy = (p.Y + q.Y + r.Y) / 3 / H;
                    var col = M.Grad(cx * 0.6f + cy * 0.4f, (0f, Col.Hex("0A1A38")), (0.55f, Col.Hex("174E70")), (1f, Col.Hex("0B2440")));
                    img.Triangle(p, q, r, col * (0.86f + 0.26f * rng.NextSingle()));
                }
            }
        img.Blur(1);
        img.Vignette(0.4f);
        return img;
    }

    public static Img ThreeCrosses()
    {
        var img = new Img(W, H);
        img.Fill((u, v) =>
        {
            var sky = M.Grad(v, (0f, Col.Hex("1A0B2E")), (0.35f, Col.Hex("55204C")), (0.6f, Col.Hex("BC4639")),
                (0.76f, Col.Hex("F08A39")), (0.84f, Col.Hex("FFC766")));
            float dx = (u - 0.64f) * Aspect, dy = v - 0.8f;
            float d2 = dx * dx + dy * dy;
            sky += Col.Hex("FFD27A") * (MathF.Exp(-d2 / 0.05f) * 0.45f + MathF.Exp(-d2 / 0.004f) * 0.35f);
            float streaks = M.Smooth(0.55f, 0.8f, Noise.Fbm(u * 2.2f, v * 30, 5, 111)) * M.Smooth(0.2f, 0.4f, v) * (1 - M.Smooth(0.68f, 0.78f, v));
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
                float foot = Hill(cu) * H + 6, top = foot - height * H;
                float cx = cu * W;
                float arm = height * H * 0.36f, armY = top + height * H * 0.27f;
                cover = MathF.Max(cover, M.Box(x, y, cx - post / 2, top, cx + post / 2, foot));
                cover = MathF.Max(cover, M.Box(x, y, cx - arm, armY - post / 2, cx + arm, armY + post / 2));
            }
            return Col.Lerp(before, ink, cover);
        });
        img.Vignette(0.3f);
        return img;
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

    public void Triangle((float X, float Y) p, (float X, float Y) q, (float X, float Y) s, Col c)
    {
        int x0 = (int)MathF.Floor(MathF.Min(p.X, MathF.Min(q.X, s.X))), x1 = (int)MathF.Ceiling(MathF.Max(p.X, MathF.Max(q.X, s.X)));
        int y0 = (int)MathF.Floor(MathF.Min(p.Y, MathF.Min(q.Y, s.Y))), y1 = (int)MathF.Ceiling(MathF.Max(p.Y, MathF.Max(q.Y, s.Y)));
        float area = (q.X - p.X) * (s.Y - p.Y) - (q.Y - p.Y) * (s.X - p.X);
        for (int y = Math.Max(0, y0); y <= Math.Min(H - 1, y1); y++)
            for (int x = Math.Max(0, x0); x <= Math.Min(W - 1, x1); x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float w0 = ((q.X - px) * (s.Y - py) - (q.Y - py) * (s.X - px)) / area;
                float w1 = ((s.X - px) * (p.Y - py) - (s.Y - py) * (p.X - px)) / area;
                float w2 = 1 - w0 - w1;
                // A hair of overlap, so no seam shows between neighbours.
                if (w0 < -0.002f || w1 < -0.002f || w2 < -0.002f) continue;
                int i = y * W + x;
                r[i] = c.R; g[i] = c.G; b[i] = c.B;
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
