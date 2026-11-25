using System.Windows;
using System.Windows.Controls.Primitives;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// Makes the drop-down of a menu row open to the RIGHT of the row, like any
/// Windows submenu. WPF's own «Right» placement mirrors itself when Windows is
/// set for left-handed menus (<see cref="SystemParameters.MenuDropAlignment"/>,
/// common on tablets and touch laptops), and the submenu opened to the left,
/// cut off and under the tooltip. With a custom placement the right side is
/// tried first and the left only if the screen has no room.
/// </summary>
public static class SubmenuPlacement
{
    public static readonly DependencyProperty AlwaysRightProperty = DependencyProperty.RegisterAttached(
        "AlwaysRight", typeof(bool), typeof(SubmenuPlacement), new PropertyMetadata(false, OnChanged));

    public static bool GetAlwaysRight(DependencyObject d) => (bool)d.GetValue(AlwaysRightProperty);

    public static void SetAlwaysRight(DependencyObject d, bool value) => d.SetValue(AlwaysRightProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Popup popup || e.NewValue is not true)
            return;

        popup.Placement = PlacementMode.Custom;
        popup.CustomPopupPlacementCallback = Place;
    }

    private static CustomPopupPlacement[] Place(Size popupSize, Size targetSize, Point offset) =>
    [
        // To the right of the row, its first item level with it.
        new(new Point(targetSize.Width + 2, -5), PopupPrimaryAxis.Horizontal),
        // No room on the right: to the left of the row.
        new(new Point(-popupSize.Width - 2, -5), PopupPrimaryAxis.Horizontal),
    ];
}
