namespace Steel_structures_nodes_public_project.Maui.Controls;

/// <summary>
/// Отображает строковое значение с кнопкой ⧉ для копирования в буфер обмена.
/// Pure-code ContentView без XAML.
/// </summary>
public class CopyableValue : ContentView
{
    private readonly Label  _label;
    private readonly Button _copyBtn;

    // BindableProperty для привязки текста из внешнего XAML
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text),
            typeof(string),
            typeof(CopyableValue),
            string.Empty,
            propertyChanged: (b, _, nv) =>
                ((CopyableValue)b)._label.Text = (string?)nv ?? string.Empty);

    /// <summary>Отображаемое и копируемое значение.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public CopyableValue()
    {
        _label = new Label
        {
            FontSize          = 14,
            FontAttributes    = FontAttributes.Bold,
            TextColor         = Color.FromArgb("#1A1A1A"),
            VerticalTextAlignment = TextAlignment.Center,
        };

        _copyBtn = new Button
        {
            Text            = "⧉",
            FontSize        = 18,
            WidthRequest    = 34,
            HeightRequest   = 34,
            BackgroundColor = Colors.Transparent,
            TextColor       = Color.FromArgb("#2B579A"),
            Padding         = new Thickness(0),
            VerticalOptions = LayoutOptions.Center,
        };
        _copyBtn.Clicked += OnCopyClicked;

        var grid = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(36))],
            ColumnSpacing     = 2,
            VerticalOptions   = LayoutOptions.Center,
        };
        grid.Add(_label,   0, 0);
        grid.Add(_copyBtn, 1, 0);

        Content = grid;
    }

    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(Text)) return;

        await Clipboard.Default.SetTextAsync(Text);

        // Краткая визуальная обратная связь
        _copyBtn.Text = "✓";
        await Task.Delay(700);
        _copyBtn.Text = "⧉";
    }
}
