using System.IO;
using System.Windows;
using EcclesiaCast.App.Views;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Feedback;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;

namespace EcclesiaCast.ScreenBench.Wpf;

/// <summary>
/// The simple dialogs (batch 15), each made with the real window of App and
/// the same sample data as in DialogScreenTests on the Avalonia side. A window
/// is never shown: its content is taken out and drawn on its own.
/// </summary>
internal static class DialogScreens
{
    public static IEnumerable<(string Name, Func<FrameworkElement> Make, double Width)> All()
    {
        yield return ("elegir", () => Take(new ChoiceWindow(
            "Ya existe una canción",
            "En la biblioteca ya hay una canción que se llama «Cuán grande es Él». ¿Qué hacemos con la que estás importando?",
            ["Guardar las dos", "Reemplazar la que estaba", "Saltear esta"],
            "Hacer lo mismo con las que quedan")), 440);

        yield return ("texto-pedir", () => Take(new TextPromptWindow(
            "Nueva pestaña", "Nombre de la pestaña", "Campamento")), 420);

        yield return ("texto-pedir-dos", () =>
        {
            var window = new TextPromptWindow("Renombrar la Biblia", "Nombre", "Reina Valera 1909");
            window.AddSecondField("Abreviatura", "RV1909");
            return Take(window);
        }, 420);

        yield return ("aviso-pregunta", () => Take(new NoticeWindow(
            "La canción va a la papelera y se puede recuperar durante 30 días.",
            "¿Eliminar «Cuán grande es Él»?", NoticeKind.Question,
            ["Cancelar", "Eliminar"], 1, 0, destructive: false, hasOwner: true)), 460);

        yield return ("aviso-destructivo", () => Take(new NoticeWindow(
            "Se borra para siempre. No hay forma de recuperarla.",
            "¿Vaciar la papelera?", NoticeKind.Warning,
            ["Cancelar", "Vaciar"], 1, 0, destructive: true, hasOwner: true)), 460);

        yield return ("aviso-con-icono", () => Take(new NoticeWindow(
            $"Las imágenes marcadas con {Notice.OnTopIconMark} van encima de todo, también de la letra.",
            "Va encima de todo", NoticeKind.Info,
            ["Entendido"], 0, 0, destructive: false, hasOwner: true)), 460);

        yield return ("aviso-error-sin-titulo", () => Take(new NoticeWindow(
            "No se pudo abrir el archivo. Puede que esté dañado o que lo esté usando otro programa.",
            null, NoticeKind.Error, ["Cancelar", "No guardar", "Guardar"], 2, 0,
            destructive: false, hasOwner: true)), 460);

        yield return ("carga", () => Take(new SplashWindow(), height: 300), 480);

        yield return ("acerca-de", () => Take(new AboutWindow("1.5.0")), 460);
        yield return ("acerca-de-primera-vez", () => Take(new AboutWindow("1.5.0", firstRun: true)), 460);

        yield return ("transiciones", () => Take(new TransitionsWindow(new MemorySettings())), 500);

        yield return ("texto-rapido", () => Take(new QuickTextWindow(
            "Cuán grande es Él,\nmi corazón entona la canción"), height: 290), 520);

        yield return ("cuenta-regresiva", () => Take(new CountdownWindow(
            new CountdownViewModel(CountdownSettings.Default, isRunning: false))), 520);
        yield return ("cuenta-regresiva-en-marcha", () => Take(new CountdownWindow(new CountdownViewModel(
            new CountdownSettings(10, "19:00", "La reunión empieza en", "¡Bienvenidos!"), isRunning: true))), 520);

        yield return ("comentarios", () => Take(Feedback(FeedbackReason.Menu)), 520);
        yield return ("comentarios-al-cerrar", () => Take(Feedback(FeedbackReason.Closing)), 520);
        yield return ("comentarios-tras-un-cierre", () => Take(Feedback(FeedbackReason.AfterCrash)), 520);

        yield return ("copia-liviana", () => Take(new LightCopyWindow(LightCopyViewModel.For(
            Path.Combine(Path.GetTempPath(), "fondo.mp4"), "Fondo de olas 4K"))), 480);

        yield return ("descargar-youtube", () => Take(new YouTubeDownloadWindow(YouTubeDownloadViewModel.For(
            "abcdefghijk", "Alabanza en vivo", () => Task.FromResult<string?>(null)))), 480);
    }

    private static FeedbackWindow Feedback(FeedbackReason reason)
    {
        var dataFolder = Path.Combine(Path.GetTempPath(), "ecclesiacast-banco-sin-registro");
        return new FeedbackWindow(new FeedbackSession(new MemorySettings(), dataFolder).CreateForm(reason));
    }

    /// <summary>
    /// What the window shows, free to be drawn alone. The height is for the
    /// windows with a fixed one (without the system's title bar).
    /// </summary>
    internal static FrameworkElement Take(Window window, double height = double.NaN)
    {
        var content = (FrameworkElement)window.Content;
        var data = window.DataContext;
        var names = NameScope.GetNameScope(window);
        window.Content = null;
        // The looks the window declares for itself, and the names its own
        // bindings point at, stay with what is drawn.
        if (window.Resources.Count > 0 || window.Resources.MergedDictionaries.Count > 0)
            content.Resources.MergedDictionaries.Add(window.Resources);
        if (names is not null && NameScope.GetNameScope(content) is null)
            NameScope.SetNameScope(content, names);
        if (data is not null)
            content.DataContext = data;
        if (!double.IsNaN(height))
            content.Height = height;
        return content;
    }

    private sealed class MemorySettings : ISettingsStore
    {
        private readonly Dictionary<string, string> _values = [];

        public string? Get(string key) => _values.GetValueOrDefault(key);

        public void Set(string key, string value) => _values[key] = value;
    }
}
