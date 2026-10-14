namespace EcclesiaCast.Core.Ui;

/// <summary>
/// The setting behind «Diseño de aprendizaje» and «Diseño avanzado»: what it
/// is called in the database and what each choice is stored as. Only the
/// exact word "complex" means the advanced layout; anything else (including a
/// setting nobody has saved yet) is the learning layout the program has
/// always had, so an operator who never opens the menu sees no change.
/// </summary>
public static class OperatorLayout
{
    /// <summary>Setting that holds the layout: "learning" or "complex".</summary>
    public const string SettingKey = "ui.mode";

    private const string AdvancedValue = "complex";
    private const string LearningValue = "learning";

    public static bool IsAdvanced(string? savedSetting) => savedSetting == AdvancedValue;

    public static string ToSetting(bool advanced) => advanced ? AdvancedValue : LearningValue;
}
