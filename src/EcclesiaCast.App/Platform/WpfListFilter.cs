using System.Collections.ObjectModel;
using System.Windows.Data;

namespace EcclesiaCast.App.Platform;

/// <summary>The default view of a collection (what a WPF list binds to), behind <see cref="IListFilter"/>.</summary>
public sealed class WpfListFilter : IListFilter
{
    public void Apply<T>(ObservableCollection<T> list, Func<T, bool>? filter)
    {
        var view = CollectionViewSource.GetDefaultView(list);
        if (filter is null)
        {
            if (view.Filter is not null)
                view.Filter = null;
            return;
        }

        view.Filter = item => item is T typed && filter(typed);
    }
}
