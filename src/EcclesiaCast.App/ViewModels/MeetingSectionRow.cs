using CommunityToolkit.Mvvm.ComponentModel;
using EcclesiaCast.Core.Timing;

namespace EcclesiaCast.App.ViewModels;

/// <summary>One row of the sections list in the Tiempos tab: editable name and minutes.</summary>
public sealed partial class MeetingSectionRow(MeetingSection section) : ObservableObject
{
    [ObservableProperty]
    private string _name = section.Name;

    /// <summary>A number box works with decimals; the section keeps whole minutes.</summary>
    [ObservableProperty]
    private double _minutes = section.Minutes;

    /// <summary>The section the timer is on (one at a time).</summary>
    [ObservableProperty]
    private bool _isCurrent;

    public MeetingSection ToSection() =>
        MeetingPlan.Clean(new MeetingSection(Name, (int)Math.Round(Minutes)));
}
