using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.Presentation.ViewModels;

/// <summary>One book tile in the grid view of the Bible: short name big, full name small, group colour.</summary>
public sealed partial class BibleBookTile : ObservableObject
{
    public BibleBookTile(BibleBookInfo info)
    {
        Info = info;
        Abbreviation = BibleBookGroups.Abbreviation(info);
        var group = BibleBookGroups.GroupOf(info.Number);
        GroupName = BibleBookGroups.GroupName(group);
        // «#RRGGBB» for the border and the letters, and the same colour at
        // low opacity («#AARRGGBB») for the fill; the screen makes the brushes.
        AccentColor = BibleBookGroups.ColorHex(group);
        FillColor = "#30" + AccentColor[1..];
    }

    public BibleBookInfo Info { get; }
    public int Number => Info.Number;
    public string Name => Info.Name;
    public string Abbreviation { get; }
    public string GroupName { get; }
    public string AccentColor { get; }
    public string FillColor { get; }

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>How the tile shows the book (see <see cref="BibleGridFormat"/>).</summary>
    [ObservableProperty]
    private BibleGridFormat _format;

}

/// <summary>One numbered button in the verse grid.</summary>
public sealed partial class VerseOption(int number) : ObservableObject
{
    public int Number { get; } = number;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>How the book tiles of the Bible grid look.</summary>
public enum BibleGridFormat
{
    /// <summary>Only the abbreviation, small: all the books fit.</summary>
    Abbreviation,

    /// <summary>The abbreviation big and the name small under it.</summary>
    Both,

    /// <summary>The full name.</summary>
    Names,
}
