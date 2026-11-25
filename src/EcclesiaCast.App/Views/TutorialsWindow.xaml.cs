using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.Core.Ui;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Ayuda → «Tutoriales…»: one card per tutorial. Choosing one closes this
/// window and gives it back in <see cref="Chosen"/>; the main window then runs
/// it over the real program.
/// </summary>
public partial class TutorialsWindow : Window
{
    public TutorialsWindow(IReadOnlyList<Tutorial> tutorials)
    {
        InitializeComponent();
        Cards.ItemsSource = tutorials;
    }

    public Tutorial? Chosen { get; private set; }

    private void Card_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: Tutorial tutorial })
        {
            Chosen = tutorial;
            DialogResult = true;
        }
    }
}
