using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.App.ViewModels;

/// <summary>One book tile in the grid view of the Bible: short name big, full name small, group colour.</summary>
public sealed partial class BibleBookTile : ObservableObject
{
    public BibleBookTile(BibleBookInfo info)
    {
        Info = info;
        Abbreviation = BibleBookGroups.Abbreviation(info);
        var group = BibleBookGroups.GroupOf(info.Number);
        GroupName = BibleBookGroups.GroupName(group);
        var color = (Color)ColorConverter.ConvertFromString(BibleBookGroups.ColorHex(group));
        AccentBrush = Freeze(new SolidColorBrush(color));
        FillBrush = Freeze(new SolidColorBrush(Color.FromArgb(0x30, color.R, color.G, color.B)));
    }

    public BibleBookInfo Info { get; }
    public int Number => Info.Number;
    public string Name => Info.Name;
    public string Abbreviation { get; }
    public string GroupName { get; }
    public Brush AccentBrush { get; }
    public Brush FillBrush { get; }

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>How the tile shows the book (see <see cref="BibleGridFormat"/>).</summary>
    [ObservableProperty]
    private BibleGridFormat _format;

    private static SolidColorBrush Freeze(SolidColorBrush brush)
    {
        brush.Freeze();
        return brush;
    }
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
