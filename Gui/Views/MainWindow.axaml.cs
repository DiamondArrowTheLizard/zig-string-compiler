using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using Gui.ViewModels;
using System;

namespace Gui.Views;

public partial class MainWindow : Window
{
    private readonly TextEditor _editor;

    public MainWindow()
    {
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("Editor")!;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetEditor(_editor);
            vm.TokenSelected += OnTokenSelected;
        }

        _editor.TextArea.Caret.PositionChanged += (s, e) =>
        {
            if (DataContext is MainWindowViewModel v)
            {
                var caret = _editor.TextArea.Caret;
                v.StatusText = $"Ln {caret.Line}, Col {caret.Column}";
            }
        };
    }

    private void OnTokenSelected(Models.TokenInfo token)
    {
        try
        {
            var line = (int)token.Line;
            var column = (int)token.StartColumn;
            var endColumn = (int)token.EndColumn;
            var startOffset = _editor.Document.GetOffset(line, column);
            var endOffset = _editor.Document.GetOffset(line, endColumn);
            _editor.CaretOffset = startOffset;
            _editor.TextArea.Selection = Selection.Create(_editor.TextArea, startOffset, endOffset);
            _editor.Focus();
        }
        catch (ArgumentOutOfRangeException)
        {

        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F5 && DataContext is MainWindowViewModel vm)
        {
            vm.RunParserCommand.Execute(null);
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
}