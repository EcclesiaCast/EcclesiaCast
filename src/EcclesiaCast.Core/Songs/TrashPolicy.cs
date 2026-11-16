namespace EcclesiaCast.Core.Songs;

/// <summary>How long the trash keeps what was deleted, and how much time is left for an item.</summary>
public static class TrashPolicy
{
    public const int RetentionDays = 30;

    /// <summary>The date before which a trashed item is erased for good.</summary>
    public static DateTime PurgeLimit(DateTime nowUtc) => nowUtc.AddDays(-RetentionDays);

    /// <summary>Whole days left before the item is erased (0 when it goes today).</summary>
    public static int DaysLeft(DateTime deletedAtUtc, DateTime nowUtc)
    {
        var left = deletedAtUtc.AddDays(RetentionDays) - nowUtc;
        return Math.Max(0, (int)Math.Ceiling(left.TotalDays));
    }
}
