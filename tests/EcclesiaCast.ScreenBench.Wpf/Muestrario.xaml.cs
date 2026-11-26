namespace EcclesiaCast.ScreenBench.Wpf;

public partial class Muestrario
{
    public Muestrario()
    {
        InitializeComponent();

        // VersionChip lights up through DataTriggers on these two.
        ChipPlain.DataContext = new Chip(false, false);
        ChipPrimary.DataContext = new Chip(true, false);
        ChipSecondary.DataContext = new Chip(false, true);
    }

    public sealed record Chip(bool IsPrimary, bool IsSecondary);
}
