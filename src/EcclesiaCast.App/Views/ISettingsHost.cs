using System.Windows;

namespace EcclesiaCast.App.Views;

/// <summary>What «Ajustes» asks of the window that opened it (the operator's window).</summary>
public interface ISettingsHost
{
    /// <summary>True while the «avanzado» layout (no shortcut hints) is on.</summary>
    bool IsAdvancedUi { get; }

    void SetUiMode(bool advanced);

    void RestartSound();

    /// <summary>Opens the transitions window over <paramref name="owner"/>.</summary>
    void OpenTransitions(Window owner);

    /// <summary>The program's colour picker; null when the operator cancelled.</summary>
    string? PickColor(string current);

    /// <summary>Opens «Seguir la canción».</summary>
    void OpenListening();
}
