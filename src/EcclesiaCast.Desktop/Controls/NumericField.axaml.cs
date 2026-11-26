using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using EcclesiaCast.Presentation.Services;

namespace EcclesiaCast.Desktop.Controls;

/// <summary>
/// A number you can type, step with − / +, spin with the mouse wheel or nudge
/// with ↑ / ↓. Replaces the sliders in the editors, where the operator wants an
/// exact value (a font size of 92, an interline of 1.15) and not a guess.
/// Holding Shift multiplies the step by ten.
/// </summary>
public sealed partial class NumericField : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<NumericField, double>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<NumericField, double>(nameof(Minimum));

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<NumericField, double>(nameof(Maximum), 1000d);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<NumericField, double>(nameof(Step), 1d);

    /// <summary>Decimal places shown (0 for sizes, 2 for line spacing).</summary>
    public static readonly StyledProperty<int> DecimalsProperty =
        AvaloniaProperty.Register<NumericField, int>(nameof(Decimals));

    /// <summary>True while <see cref="ShowValue"/> writes the box, so typing isn't fought.</summary>
    private bool _writing;

    private bool _silent;

    /// <summary>The Shift held in the last key or press: ten steps at once.</summary>
    private bool _big;

    public NumericField()
    {
        InitializeComponent();

        Up.Click += (_, _) => Bump(+1);
        Down.Click += (_, _) => Bump(-1);
        AddHandler(PointerPressedEvent, (_, e) => _big = e.KeyModifiers.HasFlag(KeyModifiers.Shift),
            RoutingStrategies.Tunnel, handledEventsToo: true);
        Entry.AddHandler(KeyDownEvent, Entry_KeyDown, RoutingStrategies.Tunnel);
        Entry.TextChanged += Entry_TextChanged;
        Entry.LostFocus += (_, _) => ShowValue();
        Entry.PointerWheelChanged += Entry_Wheel;
        ShowValue();
    }

    /// <summary>Raised after the value changes, however it changed.</summary>
    public event EventHandler? ValueChanged;

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public int Decimals
    {
        get => GetValue(DecimalsProperty);
        set => SetValue(DecimalsProperty, value);
    }

    /// <summary>Sets the value without raising <see cref="ValueChanged"/> (loading a form).</summary>
    public void SetSilently(double value)
    {
        _silent = true;
        Value = Clamp(value);
        _silent = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DecimalsProperty)
        {
            ShowValue();
        }
        else if (change.Property == ValueProperty)
        {
            var value = change.GetNewValue<double>();
            var clamped = Clamp(value);
            if (Math.Abs(clamped - value) > 1e-9)
            {
                Value = clamped; // vuelve a entrar y termina acá
                return;
            }

            if (!_writing)
                ShowValue();
            if (!_silent)
                ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private double Clamp(double value) => NumberFieldText.Clamp(value, Minimum, Maximum, Decimals);

    private void ShowValue()
    {
        // Runs while the XAML sets the properties too, before the box exists.
        if (Entry is null)
            return;

        var text = NumberFieldText.Format(Value, Decimals);
        if (Entry.Text == text)
            return;

        _writing = true;
        Entry.Text = text;
        Entry.CaretIndex = text.Length;
        _writing = false;
    }

    // ── Entrada por teclado ──────────────────────────────────────

    private void Entry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        // Avalonia raises this later than WPF: the text this field wrote
        // itself is told apart by being the value as it is written.
        if (_writing || Entry.Text == NumberFieldText.Format(Value, Decimals))
            return;

        // Half-typed values ("-", "1,") just wait; the box keeps what was typed
        // until it parses, so nothing is rewritten under the caret.
        if (!NumberFieldText.TryParse(Entry.Text ?? string.Empty, out var parsed))
            return;

        _writing = true;
        Value = parsed;
        _writing = false;
    }

    private void Entry_KeyDown(object? sender, KeyEventArgs e)
    {
        _big = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
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
                // As in WPF: Enter rewrites the box and goes no further.
                ShowValue();
                e.Handled = true;
                break;
        }
    }

    private void Entry_Wheel(object? sender, PointerWheelEventArgs e)
    {
        if (!Entry.IsKeyboardFocusWithin)
            return;
        _big = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        Bump(e.Delta.Y > 0 ? +1 : -1);
        e.Handled = true;
    }

    private void Bump(int direction)
    {
        Value = Clamp(Value + direction * Step * (_big ? 10 : 1));
        ShowValue();
    }
}
