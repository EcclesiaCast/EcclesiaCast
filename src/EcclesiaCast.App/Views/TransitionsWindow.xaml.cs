using System.Windows;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Presentation;

namespace EcclesiaCast.App.Views;

/// <summary>
/// «Proyección → Transiciones…»: how many seconds the logo, a change of
/// medium, the black screen and switching the output off take.
/// </summary>
public partial class TransitionsWindow : Window
{
    private readonly ISettingsStore _settings;

    public TransitionsWindow(ISettingsStore settings)
    {
        InitializeComponent();
        _settings = settings;
        Fill(TransitionSettings.Current);
    }

    private void Fill(OutputTransitions values)
    {
        LogoField.SetSilently(values.LogoSeconds);
        MediaField.SetSilently(values.MediaSeconds);
        BlackField.SetSilently(values.BlackSeconds);
        OutputField.SetSilently(values.OutputSeconds);
    }

    private void Defaults_Click(object sender, RoutedEventArgs e) => Fill(OutputTransitions.Default);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        TransitionSettings.Set(
            new OutputTransitions(LogoField.Value, MediaField.Value, BlackField.Value, OutputField.Value),
            _settings);
        DialogResult = true;
    }
}
