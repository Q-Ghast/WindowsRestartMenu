using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace StartMenu_App.Controls;

public sealed partial class HighlightedTextBlock : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(HighlightedTextBlock),
        new PropertyMetadata(string.Empty, OnContentChanged));

    public static readonly DependencyProperty QueryProperty = DependencyProperty.Register(
        nameof(Query),
        typeof(string),
        typeof(HighlightedTextBlock),
        new PropertyMetadata(string.Empty, OnContentChanged));

    public HighlightedTextBlock()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Query
    {
        get => (string)GetValue(QueryProperty);
        set => SetValue(QueryProperty, value);
    }

    private static void OnContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((HighlightedTextBlock)sender).Rebuild();
    }

    private void Rebuild()
    {
        Label.Inlines.Clear();
        Label.Text = string.Empty;

        var text = Text ?? string.Empty;
        var query = (Query ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return;
        }

        if (query.Length == 0)
        {
            Label.Text = text;
            return;
        }

        var highlightBrush = GetHighlightBrush();
        var index = 0;
        var found = false;

        while (index < text.Length)
        {
            var hit = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase);
            if (hit < 0)
            {
                AddRun(text.Substring(index));
                break;
            }

            if (hit > index)
            {
                AddRun(text.Substring(index, hit - index));
            }

            Label.Inlines.Add(new Run
            {
                Text = text.Substring(hit, query.Length),
                Foreground = highlightBrush
            });

            index = hit + query.Length;
            found = true;
        }

        if (!found)
        {
            Label.Inlines.Clear();
            Label.Text = text;
        }
    }

    private void AddRun(string text)
    {
        if (text.Length > 0)
        {
            Label.Inlines.Add(new Run { Text = text });
        }
    }

    private static Brush GetHighlightBrush()
    {
        // A fixed Windows accent blue reads well on both light and dark acrylic.
        return new SolidColorBrush(Color.FromArgb(255, 0, 120, 212));
    }
}
