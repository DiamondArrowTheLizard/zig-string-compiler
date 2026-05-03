using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using Gui.Models;
using Gui.ViewModels;
using System;
using System.Threading.Tasks;

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
        }
        _editor.TextArea.Caret.PositionChanged += OnCaretChanged;

        Closing += OnClosing;
    }

    private void OnCaretChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var caret = _editor.TextArea.Caret;
            vm.StatusText = $"Ln {caret.Line}, Col {caret.Column}";
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            bool canClose = await vm.ConfirmDiscardChanges();
            e.Cancel = !canClose;
        }
    }

    private void OnLexerNavigate(TokenInfo token)
    {
        NavigateToPosition((int)token.Line, (int)token.StartColumn, (int)token.EndColumn);
    }

    private void OnParserNavigate(ParserErrorInfo error)
    {
        if (error.Line > 0 && error.Column > 0)
        {
            int endColumn = Math.Min(error.Column + (error.Fragment?.Length ?? 1) - 1, 1000);
            NavigateToPosition(error.Line, error.Column, endColumn);
        }
    }

    private void NavigateToPosition(int line, int startColumn, int endColumn)
    {
        try
        {
            int lineIdx = Math.Max(1, line);
            int colIdx = Math.Max(1, startColumn);
            int endColIdx = Math.Max(1, endColumn);
            int startOffset = _editor.Document.GetOffset(lineIdx, colIdx);
            int endOffset = _editor.Document.GetOffset(lineIdx, endColIdx);
            _editor.CaretOffset = startOffset;
            _editor.TextArea.Selection = Selection.Create(_editor.TextArea, startOffset, endOffset);
            _editor.Focus();
        }
        catch (ArgumentOutOfRangeException) { }
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