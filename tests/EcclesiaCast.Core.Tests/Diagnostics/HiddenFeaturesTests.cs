using EcclesiaCast.Core.Diagnostics;

namespace EcclesiaCast.Core.Tests.Diagnostics;

public sealed class HiddenFeaturesTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ecclesiacast-hidden-" + Guid.NewGuid().ToString("N"));

    public HiddenFeaturesTests() => Directory.CreateDirectory(_folder);

    public void Dispose()
    {
        try { Directory.Delete(_folder, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void Con_el_archivo_la_funcion_esta_habilitada()
    {
        File.WriteAllText(Path.Combine(_folder, "seguir-la-cancion.txt"), string.Empty);
        Assert.True(HiddenFeatures.IsEnabled(_folder, HiddenFeatures.FollowTheSong));
    }

    [Fact]
    public void El_contenido_del_archivo_no_importa()
    {
        File.WriteAllText(Path.Combine(_folder, "seguir-la-cancion.txt"), "lo que sea");
        Assert.True(HiddenFeatures.IsEnabled(_folder, HiddenFeatures.FollowTheSong));
    }

    [Fact]
    public void Sin_el_archivo_no_esta_habilitada()
    {
        Assert.False(HiddenFeatures.IsEnabled(_folder, HiddenFeatures.FollowTheSong));
    }

    [Fact]
    public void Otro_archivo_no_habilita_la_funcion()
    {
        File.WriteAllText(Path.Combine(_folder, "otra-cosa.txt"), string.Empty);
        Assert.False(HiddenFeatures.IsEnabled(_folder, HiddenFeatures.FollowTheSong));
    }

    [Fact]
    public void Una_carpeta_con_ese_nombre_no_cuenta()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "seguir-la-cancion.txt"));
        Assert.False(HiddenFeatures.IsEnabled(_folder, HiddenFeatures.FollowTheSong));
    }

    [Fact]
    public void Si_la_carpeta_no_existe_no_esta_habilitada()
    {
        Assert.False(HiddenFeatures.IsEnabled(Path.Combine(_folder, "no-existe"), HiddenFeatures.FollowTheSong));
    }

    [Fact]
    public void Carpeta_o_nombre_vacios_no_habilitan_nada()
    {
        Assert.False(HiddenFeatures.IsEnabled(string.Empty, HiddenFeatures.FollowTheSong));
        Assert.False(HiddenFeatures.IsEnabled(_folder, string.Empty));
    }

    [Fact]
    public void Las_mayusculas_no_importan_en_Windows()
    {
        // The program only runs on Windows, where file names are not case sensitive.
        File.WriteAllText(Path.Combine(_folder, "Seguir-La-Cancion.TXT"), string.Empty);
        Assert.True(HiddenFeatures.IsEnabled(_folder, HiddenFeatures.FollowTheSong));
        Assert.True(HiddenFeatures.IsEnabled(_folder, "SEGUIR-LA-CANCION"));
    }
}
