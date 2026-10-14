using EcclesiaCast.Core.Abstractions;
using EcclesiaCast.Core.Bible;

namespace EcclesiaCast.App.Services;

/// <summary>Asks the operator which Bible versions go into a package, or come out of one.</summary>
public interface IBiblePackageDialog
{
    /// <summary>The ids of the installed versions to put in a package, or null if cancelled.</summary>
    IReadOnlyList<int>? ChooseToExport(IReadOnlyList<BibleVersionInfo> versions);

    /// <summary>The versions of a package to bring in, or null if cancelled.</summary>
    IReadOnlyList<BiblePackageEntry>? ChooseToImport(string packageFileName, IReadOnlyList<BiblePackageChoice> choices);
}
