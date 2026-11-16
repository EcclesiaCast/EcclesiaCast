using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EcclesiaCast.App.Services.Listening;
using EcclesiaCast.Core.Diagnostics;
using EcclesiaCast.Core.Listening;
using Serilog;

namespace EcclesiaCast.App.ViewModels;

/// <summary>
/// «Seguir la canción» (beta): the program listens to the sound input and
/// moves the slides of the song that is open. It only ever acts on a song
/// that already has a slide on screen, and the operator's hand always wins.
/// </summary>
public sealed partial class MainViewModel
{
    private const string ListenDeviceKey = "listen.device";
    private const string ListenModeKey = "listen.mode";
    // A new name since the pilot counts what the recogniser takes by itself: the
    // margin saved for the slow one (5 words) would now be far too early.
    private const string ListenLeadKey = "listen.lead2";

    /// <summary>How the program acts on what it hears: 0 only marks the slide, 1 follows, 2 moves on ahead.</summary>
    public const int ListenSuggest = 0, ListenFollow = 1, ListenAnticipate = 2;

    private SongListener? _listener;
    private FollowPilot? _pilot;
    private string _pilotSong = string.Empty;
    private DispatcherTimer? _listenMeter;
    private CancellationTokenSource? _modelDownload;
    private bool _listenOnCard;

    /// <summary>
    /// «Seguir la canción» is still in trial and hidden from everyone except
    /// where the file «seguir-la-cancion.txt» sits in the data folder. Looked
    /// at once, when the program starts: it takes closing and opening the
    /// program to turn it on or off. Everything that shows or starts the
    /// function depends on this.
    /// </summary>
    public bool IsListeningAvailable { get; } =
        HiddenFeatures.IsEnabled(App.DataDir, HiddenFeatures.FollowTheSong);

    public ObservableCollection<SoundInputDevice> ListenDevices { get; } = [];

    [ObservableProperty]
    private SoundInputDevice? _selectedListenDevice;

    [ObservableProperty]
    private int _listenModeIndex = ListenSuggest;

    /// <summary>In «adelantarse»: how many words before the end of the slide the next one goes up.</summary>
    [ObservableProperty]
    private double _listenLeadWords = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListenButtonText))]
    private bool _isListening;

    [ObservableProperty]
    private string _listenStatus = "Apagado.";

    /// <summary>The last words the recogniser made out, for the operator to see that it hears.</summary>
    [ObservableProperty]
    private string _listenHeard = string.Empty;

    /// <summary>The level of the input, 0 to 100.</summary>
    [ObservableProperty]
    private double _listenLevel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsListenModel))]
    private bool _hasListenModel = SpeechEar.HasModel;

    public bool NeedsListenModel => !HasListenModel;

    [ObservableProperty]
    private bool _isDownloadingListenModel;

    [ObservableProperty]
    private string _listenModelText =
        $"Hace falta bajar una sola vez el reconocedor de voz ({SpeechEar.ModelSizeText}). Queda en esta PC; lo que escucha no sale de acá.";

    public string ListenButtonText => IsListening ? "Dejar de escuchar" : "Empezar a escuchar";

    partial void OnListenModeIndexChanged(int value)
    {
        _settings.Set(ListenModeKey, value.ToString(CultureInfo.InvariantCulture));
        _pilot = null; // built again with the new mode
    }

    partial void OnListenLeadWordsChanged(double value)
    {
        _settings.Set(ListenLeadKey, value.ToString(CultureInfo.InvariantCulture));
        _pilot = null;
    }

    partial void OnSelectedListenDeviceChanged(SoundInputDevice? value)
    {
        if (value is null)
            return;
        _settings.Set(ListenDeviceKey, value.Id + (value.Loopback ? "|loop" : string.Empty));
        if (IsListening)
            StartListening();
    }

    /// <summary>Fills the list of inputs and puts back what was chosen last time. Called when the window opens.</summary>
    [RelayCommand]
    private void RefreshListenDevices()
    {
        if (!IsListeningAvailable)
            return;

        var saved = _settings.Get(ListenDeviceKey) ?? string.Empty;
        var keep = SelectedListenDevice;

        ListenDevices.Clear();
        foreach (var device in SoundInput.Devices())
            ListenDevices.Add(device);


        SelectedListenDevice =
            ListenDevices.FirstOrDefault(d => keep is not null && d.Id == keep.Id && d.Loopback == keep.Loopback)
            ?? ListenDevices.FirstOrDefault(d => d.Id + (d.Loopback ? "|loop" : string.Empty) == saved)
            ?? ListenDevices.FirstOrDefault();

        if (int.TryParse(_settings.Get(ListenModeKey), out var mode) && mode is >= 0 and <= 2 && !IsListening)
            ListenModeIndex = mode;
        if (double.TryParse(_settings.Get(ListenLeadKey), NumberStyles.Float, CultureInfo.InvariantCulture, out var lead)
            && lead is >= 0 and <= 12)
            ListenLeadWords = lead;
        HasListenModel = SpeechEar.HasModel;
    }

    [RelayCommand]
    private async Task DownloadListenModel()
    {
        if (!IsListeningAvailable)
            return;

        if (IsDownloadingListenModel)
        {
            _modelDownload?.Cancel();
            return;
        }

        IsDownloadingListenModel = true;
        _modelDownload = new CancellationTokenSource();
        try
        {
            var progress = new Progress<double>(mb => ListenModelText = $"Bajando el reconocedor de voz… {mb:0} MB");
            await SpeechEar.DownloadModelAsync(progress, _modelDownload.Token);
            HasListenModel = true;
            ListenModelText = "Reconocedor de voz listo.";
        }
        catch (OperationCanceledException)
        {
            ListenModelText = "Descarga cancelada.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo bajar el modelo de voz");
            ListenModelText = $"No se pudo bajar: {ex.Message}";
        }
        finally
        {
            IsDownloadingListenModel = false;
        }
    }

    [RelayCommand]
    private void ToggleListening()
    {
        if (IsListening)
            StopListening("Apagado.");
        else
            StartListening();
    }

    private void StartListening()
    {
        // Hidden function: no microphone, no recogniser, no download.
        if (!IsListeningAvailable)
            return;

        if (!SpeechEar.HasModel)
        {
            ListenStatus = "Primero hay que bajar el reconocedor de voz.";
            return;
        }

        if (SelectedListenDevice is null)
            RefreshListenDevices();
        if (SelectedListenDevice is not { } device)
        {
            ListenStatus = "No hay ninguna entrada de sonido.";
            return;
        }

        try
        {
            if (_listener is null)
            {
                _listener = new SongListener(System.Windows.Application.Current.Dispatcher);
                _listener.Heard += OnHeard;
                _listener.Failed += reason => StopListening(reason);
                _listener.Ready += onCard =>
                {
                    _listenOnCard = onCard;
                    if (IsListening)
                        ListenStatus = "Escuchando.";
                };
            }

            _pilot = null;
            _listener.Start(device);
            IsListening = true;
            ListenHeard = string.Empty;
            ListenStatus = "Cargando el reconocedor de voz…";
            Log.Information("Seguir la canción: encendido ({Device}, modo {Mode})", device.Name, ListenModeIndex);

            _listenMeter ??= CreateListenMeter();
            _listenMeter.Start();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "No se pudo empezar a escuchar");
            StopListening($"No se pudo abrir esa entrada: {ex.Message}");
        }
    }

    private void StopListening(string status)
    {
        _listener?.Stop();
        _listenMeter?.Stop();
        _pilot = null;
        IsListening = false;
        ListenLevel = 0;
        ListenStatus = status;
        Log.Information("Seguir la canción: apagado ({Status})", status);
    }

    private DispatcherTimer CreateListenMeter()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        timer.Tick += (_, _) =>
        {
            // Sound levels are heard in steps, not in a straight line.
            var peak = _listener?.TakeLevel() ?? 0;
            ListenLevel = peak <= 0.001 ? 0 : Math.Clamp(100 + 20 * Math.Log10(peak) * 2, 0, 100);
        };
        return timer;
    }

    /// <summary>The recogniser listened to another stretch: decide whether a slide moves.</summary>
    private void OnHeard(HeardWindow heard)
    {
        if (!IsListening)
            return;

        ListenHeard = heard.Text.Length > 0 ? heard.Text
            : heard.Level < SongListener.Silence ? "(no llega sonido por esta entrada: revisá que no esté silenciada en Windows)"
            : "(no distingo palabras)";
        var slow = heard.Cost > 2.5
            ? _listenOnCard ? " · esta PC va justa: el reconocedor tarda" : " · va lento: esta PC no tiene placa de video que lo ayude"
            : string.Empty;

        // Only a song with one of its slides on screen is followed: the pilot
        // never puts up the first one, changes of song or touches the Bible.
        var live = LiveSlideIndex;
        if (!IsGridOnAir || live < 0 || live >= Slides.Count || Slides[live].SectionId == 0)
        {
            _pilot = null;
            ListenStatus = "Escuchando. Poné la primera diapositiva de una canción y sigo desde ahí." + slow;
            return;
        }

        var now = DateTimeOffset.Now;
        var pilot = PilotFor(live);

        // The operator moved by hand since the last time: start from there.
        if (pilot.LiveSlide != live)
        {
            pilot.OperatorMoved(live, now);
            Log.Information("Seguir la canción: el operador puso la diapositiva {Slide}", live + 1);
        }

        var decision = pilot.HearWindow(heard.Text, now, TimeSpan.FromSeconds(heard.Cost));
        Log.Debug("Seguir la canción: «{Text}» ({Cost:0.0} s, nivel {Level:0.000}) → {Position}",
            heard.Text, heard.Cost, heard.Level, decision.Position);

        var where = decision.Position is { } at && at.Confidence >= 0.3
            ? $"Voy por la diapositiva {at.Slide + 1}"
            : "No ubico la letra todavía";

        if (decision.GoTo is not { } target)
        {
            ListenStatus = where + "." + slow;
            return;
        }

        if (ListenModeIndex == ListenSuggest)
        {
            SetPreviewIndex(target);

            // Nothing went up: the song is still where the operator has it.
            pilot.OperatorMoved(live, now, byHand: false);
            ListenStatus = $"Sugiero la diapositiva {target + 1} (Enter la pone)." + slow;
            Log.Information("Seguir la canción: sugiere la diapositiva {Slide} (en vivo: {Live})", target + 1, live + 1);
            return;
        }

        Log.Information("Seguir la canción: pone la diapositiva {Slide} (estaba la {Live})", target + 1, live + 1);
        GoLiveSlide(target);
        ListenStatus = $"Puse la diapositiva {target + 1}." + slow;
    }

    /// <summary>The pilot for the song on the grid; a new one when the song, its slides or the mode changed.</summary>
    private FollowPilot PilotFor(int live)
    {
        var texts = Slides.Select(s => s.Slide.MainText ?? string.Empty).ToList();
        var song = string.Join('\u0001', texts);
        if (_pilot is not null && song == _pilotSong)
            return _pilot;

        _pilotSong = song;
        _pilot = new FollowPilot(texts, live, new FollowOptions
        {
            Mode = ListenModeIndex == ListenFollow ? FollowMode.Follow : FollowMode.Anticipate,
            LeadWords = (int)Math.Round(ListenLeadWords),
        });
        return _pilot;
    }

    /// <summary>Called when the program closes.</summary>
    public void ShutDownListening()
    {
        _modelDownload?.Cancel();
        _listener?.Dispose();
        _listener = null;
    }
}
