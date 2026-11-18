using System.Collections.ObjectModel;

namespace EcclesiaCast.Presentation.Platform;

/// <summary>
/// Hides some of the items of a list the screen shows without removing them
/// from the list: the list a screen binds to keeps all its items, the filter
/// only decides which ones are drawn.
/// </summary>
public interface IListFilter
{
    /// <summary>Shows only the items that pass <paramref name="filter"/>; null shows them all again.</summary>
    void Apply<T>(ObservableCollection<T> list, Func<T, bool>? filter);
}
