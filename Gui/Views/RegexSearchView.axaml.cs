using Avalonia.Controls;
using AvaloniaEdit;
using Gui.Models;
using Gui.ViewModels;
using System.Text.RegularExpressions;

namespace Gui.Views;

public partial class RegexSearchView : UserControl
{
    private TextEditor? _editor;
    private readonly RegexColorizer _colorizer = new();

    public RegexSearchView()
    {
        InitializeComponent();
        this.FindControl<Button>("BtnSearch")!.Click += OnSearchClick;
    }

    public void SetEditor(TextEditor editor)
    {
        _editor = editor;
        _editor.TextArea.TextView.LineTransformers.Add(_colorizer);
    }

    private void OnSearchClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_editor == null || DataContext is not RegexSearchViewModel vm || vm.SelectedPreset == null)
            return;

        string text = _editor.Text;
        vm.Matches.Clear();
        _colorizer.SegmentsToHighlight.Clear();

        if (string.IsNullOrEmpty(text))
        {
            vm.SummaryText = "Редактор пуст.";
            _editor.TextArea.TextView.Redraw();
            return;
        }

        var regex = new Regex(vm.SelectedPreset.RegexPattern, RegexOptions.IgnoreCase);
        var matches = regex.Matches(text);

        foreach (Match match in matches)
        {
            var location = _editor.Document.GetLocation(match.Index);
            
            var info = new RegexMatchInfo(
                match.Value,
                match.Index,
                match.Length,
                location.Line,
                location.Column
            );

            vm.Matches.Add(info);
            _colorizer.SegmentsToHighlight.Add((match.Index, match.Length));
        }

        vm.SummaryText = $"Поиск завершен. Найдено совпадений: {matches.Count}";
        
        _editor.TextArea.TextView.Redraw();
    }
}