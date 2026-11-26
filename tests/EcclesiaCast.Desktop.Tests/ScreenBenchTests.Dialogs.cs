using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using EcclesiaCast.Desktop.Views;
using EcclesiaCast.Presentation.Platform;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// The simple dialogs (batch 15), with the same sample data as
/// DialogScreens in the WPF bench. Here the real window is shown in memory,
/// which is on no screen.
/// </summary>
public sealed partial class ScreenBenchTests
{
    [AvaloniaFact]
    public void The_choice_window_is_drawn()
    {
        var picture = DrawWindow(new ChoiceWindow(
            "Ya existe una canción",
            "En la biblioteca ya hay una canción que se llama «Cuán grande es Él». ¿Qué hacemos con la que estás importando?",
            ["Guardar las dos", "Reemplazar la que estaba", "Saltear esta"],
            "Hacer lo mismo con las que quedan"), "elegir");

        Assert.Equal(440, picture.Width);
    }

    [AvaloniaFact]
    public void The_text_prompt_is_drawn() =>
        DrawWindow(new TextPromptWindow("Nueva pestaña", "Nombre de la pestaña", "Campamento"), "texto-pedir");

    [AvaloniaFact]
    public void The_prompt_for_two_texts_is_drawn()
    {
        var window = new TextPromptWindow("Renombrar la Biblia", "Nombre", "Reina Valera 1909");
        window.AddSecondField("Abreviatura", "RV1909");

        DrawWindow(window, "texto-pedir-dos");
    }

    [AvaloniaFact]
    public void A_question_is_drawn() =>
        DrawWindow(new NoticeWindow(
            "La canción va a la papelera y se puede recuperar durante 30 días.",
            "¿Eliminar «Cuán grande es Él»?", NoticeKind.Question, ["Cancelar", "Eliminar"], 1, 0), "aviso-pregunta");

    [AvaloniaFact]
    public void A_destructive_question_is_drawn() =>
        DrawWindow(new NoticeWindow(
            "Se borra para siempre. No hay forma de recuperarla.",
            "¿Vaciar la papelera?", NoticeKind.Warning, ["Cancelar", "Vaciar"], 1, 0, destructive: true),
            "aviso-destructivo");

    [AvaloniaFact]
    public void A_notice_with_the_on_top_icon_is_drawn() =>
        DrawWindow(new NoticeWindow(
            $"Las imágenes marcadas con {Notice.OnTopIconMark} van encima de todo, también de la letra.",
            "Va encima de todo", NoticeKind.Info, ["Entendido"], 0, 0), "aviso-con-icono");

    [AvaloniaFact]
    public void An_error_without_a_title_is_drawn() =>
        DrawWindow(new NoticeWindow(
            "No se pudo abrir el archivo. Puede que esté dañado o que lo esté usando otro programa.",
            null, NoticeKind.Error, ["Cancelar", "No guardar", "Guardar"], 2, 0), "aviso-error-sin-titulo");

    /// <summary>
    /// A whole dialog in memory, at its own width. <paramref name="height"/>
    /// is for the windows with a fixed one, without the system's title bar.
    /// </summary>
    private static Avalonia.PixelSize DrawWindow(Window window, string name, double? height = null)
    {
        if (height is { } fixedHeight)
            window.Height = fixedHeight;
        window.Show();
        Settle();
        var size = Save(window.CaptureRenderedFrame()!, name);
        window.Close();
        return size;
    }
}
