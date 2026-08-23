using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EcclesiaCast.App.Controls;

/// <summary>
/// A number you can type, step with − / +, spin with the mouse wheel or nudge
/// with ↑ / ↓. Replaces the sliders in the editors, where the operator wants an
/// exact value (a font size of 92, an interline of 1.15) and not a guess.
/// Holding Shift multiplies the step by ten.
/// </summary>
public partial class NumericField : UserControl
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericField),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, e) => ((NumericField)d).OnValueChanged((double)e.NewValue)));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericField),
            new PropertyMetadata(0d));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericField),
            new PropertyMetadata(1000d));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(double), typeof(NumericField),
            new PropertyMetadata(1d));

    public static readonly DependencyProperty DecimalsProperty =
        DependencyProperty.Register(nameof(Decimals), typeof(int), typeof(NumericField),
            new PropertyMetadata(0, (d, _) => ((NumericField)d).ShowValue()));

    /// <summary>Raised after the value changes, however it changed.</summary>
    public static readonly RoutedEvent ValueChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(ValueChanged), RoutingStrategy.Direct,
            typeof(RoutedEventHandler), typeof(NumericField));

    /// <summary>True while <see cref="ShowValue"/> writes the box, so typing isn't fought.</summary>
    private bool _writing;

    public NumericField()
    {
        InitializeComponent();
        ShowValue();
    }

    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    /// <summary>Decimal places shown (0 for sizes, 2 for line spacing).</summary>
    public int Decimals
    {
        get => (int)GetValue(DecimalsProperty);
        set => SetValue(DecimalsProperty, value);
    }

    /// <summary>Sets the value without raising <see cref="ValueChanged"/> (loading a form).</summary>
    public void SetSilently(double value)
    {
        _silent = true;
        Value = Clamp(value);
        _silent = false;
    }

    private bool _silent;

    private void OnValueChanged(double value)
    {
        var clamped = Clamp(value);
        if (Math.Abs(clamped - value) > 1e-9)
        {
            Value = clamped; // vuelve a entrar y termina acá
            return;
        }

        if (!_writing)
            ShowValue();

        if (!_silent)
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent, this));
    }

    private double Clamp(double value) => Math.Clamp(Round(value), Minimum, Maximum);

    private double Round(double value) => Math.Round(value, Math.Clamp(Decimals, 0, 6));

    private void ShowValue()
    {
        if (Entry is null)
            return;

        var text = Value.ToString("F" + Math.Clamp(Decimals, 0, 6), CultureInfo.CurrentCulture);
        if (Entry.Text == text)
            return;

        _writing = true;
        Entry.Text = text;
        Entry.CaretIndex = text.Length;
        _writing = false;
    }

    // ── Entrada por teclado ──────────────────────────────────────

    private void Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_writing)
            return;

        // Half-typed values ("-", "1,") just wait; the box keeps what was typed
        // until it parses, so nothing is rewritten under the caret.
        if (!TryParse(Entry.Text, out var parsed))
            return;

        _writing = true;
        Value = parsed;
        _writing = false;
    }

    private void Entry_LostFocus(object sender, RoutedEventArgs e) => ShowValue();

    private void Entry_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                Bump(+1);
                e.Handled = true;
                break;
            case Key.Down:
                Bump(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                ShowValue();
                e.Handled = true;
                break;
        }
    }

    private void Entry_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Entry.IsKeyboardFocusWithin)
            return;
        Bump(e.Delta > 0 ? +1 : -1);
        e.Handled = true;
    }

    private void Up_Click(object sender, RoutedEventArgs e) => Bump(+1);

    private void Down_Click(object sender, RoutedEventArgs e) => Bump(-1);

    private void Bump(int direction)
    {
        var step = Step * ((Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 10 : 1);
        Value = Clamp(Value + direction * step);
        ShowValue();
    }

    /// <summary>Accepts both decimal separators, so "1.15" and "1,15" both work.</summary>
    private static bool TryParse(string text, out double value)
    {
        text = text.Trim();
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
