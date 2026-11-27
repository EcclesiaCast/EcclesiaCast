using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Desktop.Services;
using EcclesiaCast.Presentation.Services;
using EcclesiaCast.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace EcclesiaCast.Desktop.Tests;

/// <summary>
/// «Ajustes» (batch 16) over the real program, with a temporary library: it
/// needs the whole main view model.
/// </summary>
[Collection("Programa")]
public sealed class SettingsWindowTests
{
    private static Views.SettingsWindow OpenSettings(ProgramTests.TestProgram program)
    {
        var dialog = program.Services.GetRequiredService<SettingsDialog>();
        dialog.Show();
        Dispatcher.UIThread.RunJobs();
        return dialog.Window!;
    }

    private static T Named<T>(Window window, string name) where T : Control => window.FindControl<T>(name)!;

    private static void Choose(Window window, string pill)
    {
        Named<RadioButton>(window, pill).IsChecked = true;
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaTheory]
    [InlineData("ScreensPill", "ajustes")]
    [InlineData("VideosPill", "ajustes-videos")]
    [InlineData("UpdatesPill", "ajustes-contador")]
    public void The_settings_are_drawn(string pill, string name)
    {
        using var program = new ProgramTests.TestProgram();
        var settings = program.Services.GetRequiredService<SettingsDialog>();
        var vm = program.Services.GetRequiredService<MainViewModel>();
        if (name == "ajustes-videos")
        {
            // With the flat colour chosen, so its sample is in the picture.
            vm.NewVideoFitIndex = (int)Core.Media.NewVideoFitMode.Color;
            vm.NewVideoFitColor = "#1E3A5F";
        }

        var window = new Views.SettingsWindow(
            new SettingsViewModel(vm, program.Services.GetRequiredService<Presentation.Platform.IPlatformInfo>()),
            program.Services.GetRequiredService<UiModeChoice>(),
            program.Services.GetRequiredService<IColorPicker>(),
            program.Services.GetRequiredService<MenuDialogs>());
        Named<RadioButton>(window, pill).IsChecked = true;

        ScreenBenchTests.DrawWindow(window, name, 480 - 39);
        Assert.Null(settings.Window);
    }

    [AvaloniaFact]
    public void There_is_only_one_and_it_opens_again_after_closing()
    {
        using var program = new ProgramTests.TestProgram();
        var dialog = program.Services.GetRequiredService<SettingsDialog>();
        var first = OpenSettings(program);
        try
        {
            dialog.Show();
            Assert.Same(first, dialog.Window);
        }
        finally
        {
            first.Close();
        }

        Assert.Null(dialog.Window);
        var second = OpenSettings(program);
        Assert.NotSame(first, second);
        second.Close();
    }

    [AvaloniaTheory]
    [InlineData(PhysicalKey.Enter, null)]
    [InlineData(PhysicalKey.Escape, null)]
    [InlineData(PhysicalKey.Enter, "ScreensPill")]
    [InlineData(PhysicalKey.Enter, "OutputScreens")]
    [InlineData(PhysicalKey.Escape, "ScreensPill")]
    public void Enter_and_escape_close_the_settings(PhysicalKey key, string? focused)
    {
        using var program = new ProgramTests.TestProgram();
        var window = OpenSettings(program);
        var closed = false;
        window.Closed += (_, _) => closed = true;
        if (focused is not null)
        {
            Assert.True(Named<Control>(window, focused).Focus());
            Dispatcher.UIThread.RunJobs();
        }

        DialogKeyTests.Hit(window, key);

        Assert.True(closed);
    }

    [AvaloniaFact]
    public void What_is_changed_in_the_settings_reaches_the_program()
    {
        using var program = new ProgramTests.TestProgram();
        var vm = program.Services.GetRequiredService<MainViewModel>();
        var window = OpenSettings(program);

        // Where the program does not update itself, only the count of use is left.
        Assert.False(Named<StackPanel>(window, "UpdateCheck").IsVisible);
        Assert.Equal("Contador de uso", Named<RadioButton>(window, "UpdatesPill").Content);
        Assert.False(Named<RadioButton>(window, "ListeningPill").IsVisible);

        Choose(window, "PerformancePill");
        Named<CheckBox>(window, "LowPerformanceBox").IsChecked = true;
        Assert.True(vm.IsLowPerformance);

        Choose(window, "UpdatesPill");
        Named<CheckBox>(window, "UsageCountBox").IsChecked = true;
        Dispatcher.UIThread.RunJobs();
        Named<TextBox>(window, "ChurchName").Text = "Iglesia de prueba";
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Iglesia de prueba", vm.UsageChurchName);

        Choose(window, "LookPill");
        var advanced = Named<RadioButton>(window, "AdvancedRadio");
        advanced.IsChecked = true;
        advanced.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.True(program.Services.GetRequiredService<UiModeChoice>().IsAdvanced);
        Assert.True(new MainWindowSettings(program.Services.GetRequiredService<ISettingsStore>()).ReadAdvancedUi());
        Assert.Equal(false, Avalonia.Application.Current!.Resources["LearningHintVisible"]);

        program.Services.GetRequiredService<UiModeChoice>().Set(advanced: false);
        window.Close();
    }
}
