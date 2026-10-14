using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Remote;
using EcclesiaCast.App.Services;
using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;
using EcclesiaCast.Core.Diagnostics;
using EcclesiaCast.Core.Displays;
using EcclesiaCast.Core.Logos;
using EcclesiaCast.Core.Media;
using EcclesiaCast.Core.Performance;
using EcclesiaCast.Core.Playlists;
using EcclesiaCast.Core.Presentation;
using EcclesiaCast.Core.Songs;
using EcclesiaCast.Core.Stage;
using EcclesiaCast.Core.Timing;
using EcclesiaCast.Core.Themes;
using EcclesiaCast.Data.Persistence;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

public sealed partial class MainViewModel
{
    // ── Tiempos: secciones de la reunión y cronómetro ────────────

    private const string MeetingSectionsKey = "times.sections";

    private MeetingClocks Clocks => Projection.Clocks;

    /// <summary>The parts of the meeting with their minutes, editable in the Tiempos tab.</summary>
    public ObservableCollection<MeetingSectionRow> MeetingSections { get; } = [];

    /// <summary>Name of the section the timer is on.</summary>
    [ObservableProperty]
    private string _currentSectionName = "Agregá una sección";

    /// <summary>What is left of that section; «-2:15» once it is over.</summary>
    [ObservableProperty]
    private string _sectionClockText = "—";

    /// <summary>The section went over its time: the number turns red.</summary>
    [ObservableProperty]
    private bool _isSectionOvertime;

    [ObservableProperty]
    private bool _isSectionRunning;

    /// <summary>The service stopwatch (the stage's «Cronómetro» box shows it too).</summary>
    [ObservableProperty]
    private string _stopwatchText = "0:00";

    [ObservableProperty]
    private bool _isStopwatchRunning;

    private readonly System.Windows.Threading.DispatcherTimer _timesTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(500),
    };

    private bool _loadingSections;

    private MeetingSectionRow? CurrentSection => MeetingSections.FirstOrDefault(r => r.IsCurrent);

    private void LoadMeetingSections()
    {
        _loadingSections = true;
        MeetingSections.Clear();
        foreach (var section in MeetingPlan.Deserialize(_settings.Get(MeetingSectionsKey)))
            AddSectionRow(new MeetingSectionRow(section));
        _loadingSections = false;

        if (MeetingSections.Count > 0)
            ChooseSection(MeetingSections[0]);
        else
            ChooseSection(null);

        _timesTimer.Tick += (_, _) => UpdateTimes();
        _timesTimer.Start();
        UpdateTimes();
    }

    private void AddSectionRow(MeetingSectionRow row)
    {
        row.PropertyChanged += OnSectionRowChanged;
        MeetingSections.Add(row);
    }

    private void OnSectionRowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_loadingSections || sender is not MeetingSectionRow row)
            return;

        switch (e.PropertyName)
        {
            case nameof(MeetingSectionRow.IsCurrent) when row.IsCurrent:
                ChooseSection(row);
                break;

            case nameof(MeetingSectionRow.Name):
            case nameof(MeetingSectionRow.Minutes):
                SaveMeetingSections();
                // A section that is not counting takes its new time at once;
                // one that is counting keeps going until it is set to zero.
                if (row.IsCurrent && !Clocks.Section.IsRunning)
                    ChooseSection(row);
                else if (row.IsCurrent)
                    CurrentSectionName = row.Name;
                break;
        }
    }

    private void SaveMeetingSections() =>
        _settings.Set(MeetingSectionsKey, MeetingPlan.Serialize(MeetingSections.Select(r => r.ToSection())));

    /// <summary>Puts the timer on a section: its planned time, stopped.</summary>
    private void ChooseSection(MeetingSectionRow? row)
    {
        _loadingSections = true;
        foreach (var other in MeetingSections)
            other.IsCurrent = ReferenceEquals(other, row);
        _loadingSections = false;

        if (row is null)
        {
            Clocks.HasSection = false;
            Clocks.SectionName = string.Empty;
            Clocks.Section.Load(TimeSpan.Zero);
            CurrentSectionName = "Agregá una sección";
        }
        else
        {
            var section = row.ToSection();
            Clocks.Section.Load(TimeSpan.FromMinutes(section.Minutes));
            Clocks.HasSection = true;
            Clocks.SectionName = section.Name;
            CurrentSectionName = section.Name;
        }

        UpdateTimes();
    }

    /// <summary>Reads the clocks into the texts the tab shows; the stage screens read the clocks themselves.</summary>
    private void UpdateTimes()
    {
        var now = DateTimeOffset.Now;
        IsSectionRunning = Clocks.Section.IsRunning;
        IsSectionOvertime = Clocks.HasSection && Clocks.Section.IsOvertime(now);
        SectionClockText = Clocks.HasSection ? TimeText.Clock(Clocks.Section.Remaining(now)) : "—";
        IsStopwatchRunning = Clocks.Stopwatch.IsRunning;
        StopwatchText = TimeText.Elapsed(Clocks.Stopwatch.Elapsed(now));
    }

    [RelayCommand]
    private void ToggleSectionTimer()
    {
        if (!Clocks.HasSection)
            return;

        if (Clocks.Section.IsRunning)
            Clocks.Section.Pause(DateTimeOffset.Now);
        else
            Clocks.Section.Start(DateTimeOffset.Now);
        UpdateTimes();
    }

    /// <summary>Back to the planned time, stopped.</summary>
    [RelayCommand]
    private void ResetSectionTimer()
    {
        if (CurrentSection is { } row)
            ChooseSection(row);
    }

    [RelayCommand]
    private void AddMinuteToSection()
    {
        Clocks.Section.AddMinutes(1);
        UpdateTimes();
    }

    [RelayCommand]
    private void TakeMinuteFromSection()
    {
        Clocks.Section.AddMinutes(-1);
        UpdateTimes();
    }

    /// <summary>On to the next part of the meeting; it keeps counting if the last one was counting.</summary>
    [RelayCommand]
    private void NextSectionTimer()
    {
        if (CurrentSection is not { } row)
            return;

        var index = MeetingSections.IndexOf(row);
        if (MeetingPlan.NextIndex(index, MeetingSections.Count) is not int next)
        {
            StatusText = $"«{row.Name}» es la última sección.";
            return;
        }

        var wasRunning = Clocks.Section.IsRunning;
        ChooseSection(MeetingSections[next]);
        if (wasRunning)
            Clocks.Section.Start(DateTimeOffset.Now);
        UpdateTimes();
    }

    [RelayCommand]
    private void ChooseSectionRow(MeetingSectionRow? row)
    {
        if (row is not null)
            ChooseSection(row);
    }

    [RelayCommand]
    private void AddSection()
    {
        var row = new MeetingSectionRow(new MeetingSection("Sección nueva", 10));
        AddSectionRow(row);
        SaveMeetingSections();
        if (MeetingSections.Count == 1)
            ChooseSection(row);
    }

    [RelayCommand]
    private void RemoveSection(MeetingSectionRow? row)
    {
        if (row is null)
            return;

        var wasCurrent = row.IsCurrent;
        var index = MeetingSections.IndexOf(row);
        row.PropertyChanged -= OnSectionRowChanged;
        MeetingSections.Remove(row);
        SaveMeetingSections();

        if (wasCurrent)
            ChooseSection(MeetingSections.Count == 0 ? null : MeetingSections[Math.Min(index, MeetingSections.Count - 1)]);
    }

    [RelayCommand]
    private void MoveSectionUp(MeetingSectionRow? row) => MoveSection(row, -1);

    [RelayCommand]
    private void MoveSectionDown(MeetingSectionRow? row) => MoveSection(row, +1);

    private void MoveSection(MeetingSectionRow? row, int by)
    {
        if (row is null)
            return;

        var from = MeetingSections.IndexOf(row);
        var to = from + by;
        if (from < 0 || to < 0 || to >= MeetingSections.Count)
            return;

        MeetingSections.Move(from, to);
        SaveMeetingSections();
    }

    [RelayCommand]
    private void ToggleStopwatch()
    {
        if (Clocks.Stopwatch.IsRunning)
            Clocks.Stopwatch.Pause(DateTimeOffset.Now);
        else
            Clocks.Stopwatch.Start(DateTimeOffset.Now);
        UpdateTimes();
    }

    /// <summary>Zero and stopped, ready to start when the service does.</summary>
    [RelayCommand]
    private void ResetStopwatch()
    {
        Clocks.Stopwatch.Reset();
        UpdateTimes();
    }
}
