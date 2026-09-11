using System.Windows;
using System.Windows.Controls;
using EcclesiaCast.Core.Playlists;

namespace EcclesiaCast.App.Views;

/// <summary>
/// Sets up a list that fills itself from the library: what was added
/// recently, what the church has stopped singing, everything by one artist,
/// everything that mentions a word.
/// </summary>
public partial class SmartPlaylistWindow : Window
{
    public SmartPlaylistWindow(string name, PlaylistRule rule, string? value)
    {
        InitializeComponent();

        NameBox.Text = name;
        RuleBox.SelectedIndex = rule == PlaylistRule.None ? 0 : (int)rule - 1;

        if (rule is PlaylistRule.ByArtist or PlaylistRule.Containing)
            TextBoxValue.Text = value ?? string.Empty;
        else if (!string.IsNullOrWhiteSpace(value))
            DaysBox.Text = value;

        ShowRowsForRule();
        NameBox.Focus();
        NameBox.SelectAll();
    }

    public string ResultName { get; private set; } = string.Empty;

    public PlaylistRule ResultRule { get; private set; } = PlaylistRule.RecentlyAdded;

    public string ResultValue { get; private set; } = string.Empty;

    private PlaylistRule SelectedRule => (PlaylistRule)(Math.Max(0, RuleBox.SelectedIndex) + 1);

    private void Rule_Changed(object sender, SelectionChangedEventArgs e) => ShowRowsForRule();

    private void ShowRowsForRule()
    {
        if (DaysRow is null || TextRow is null)
            return;

        var byText = SelectedRule is PlaylistRule.ByArtist or PlaylistRule.Containing;
        DaysRow.Visibility = byText ? Visibility.Collapsed : Visibility.Visible;
        TextRow.Visibility = byText ? Visibility.Visible : Visibility.Collapsed;
        TextLabel.Text = SelectedRule == PlaylistRule.ByArtist ? "Artista" : "Palabra";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0)
        {
            Fail("Ponele un nombre a la lista.");
            return;
        }

        var rule = SelectedRule;
        string value;

        if (rule is PlaylistRule.ByArtist or PlaylistRule.Containing)
        {
            value = TextBoxValue.Text.Trim();
            if (value.Length == 0)
            {
                Fail(rule == PlaylistRule.ByArtist
                    ? "Escribí el artista que querés juntar."
                    : "Escribí la palabra que tienen que decir las canciones.");
                return;
            }
        }
        else
        {
            if (!int.TryParse(DaysBox.Text.Trim(), out var days) || days <= 0)
            {
                Fail("Poné cuántos días, por ejemplo 30.");
                return;
            }

            value = days.ToString();
        }

        ResultName = name;
        ResultRule = rule;
        ResultValue = value;
        DialogResult = true;
    }

    private void Fail(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
