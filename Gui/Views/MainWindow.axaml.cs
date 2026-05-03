using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using Gui.Models;
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
        try
        {
            int line = (int)token.Line;                // 1-based
            int startCol = (int)token.StartColumn;     // 0-based
            int endCol = (int)token.EndColumn;         // 0-based (index of last char)
            int length = endCol - startCol + 1;
            int col1 = startCol + 1;                   // convert to 1-based

            int startOffset = _editor.Document.GetOffset(line, col1);
            int endOffset = startOffset + length;
            SelectAndMove(startOffset, endOffset);
        }
        catch (ArgumentOutOfRangeException) { }
    }

    private void OnParserNavigate(ParserErrorInfo error)
    {
        try
        {
            int line = error.Line;                     // 1-based
            int col = error.Column;                    // 1-based
            int length = error.Fragment?.Length ?? 0;
            if (line > 0 && col > 0 && length > 0)
            {
                int startOffset = _editor.Document.GetOffset(line, col);
                int endOffset = startOffset + length;
                SelectAndMove(startOffset, endOffset);
            }
        }
        catch (ArgumentOutOfRangeException) { }
    }

    private void SelectAndMove(int startOffset, int endOffset)
    {
        _editor.CaretOffset = startOffset;
        _editor.TextArea.Selection = Selection.Create(_editor.TextArea, startOffset, endOffset);
        _editor.Focus();
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