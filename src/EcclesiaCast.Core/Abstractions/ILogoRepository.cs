using EcclesiaCast.Core.Logos;

namespace EcclesiaCast.Core.Abstractions;

public interface ILogoRepository
{
    /// <summary>All logos, in the order the operator arranged them.</summary>
    IReadOnlyList<Logo> GetAll();

    Logo? Get(int id);

    /// <summary>Inserts (Id 0) or updates, and returns the stored copy.</summary>
    Logo Save(Logo logo);

    void Delete(int id);
}
