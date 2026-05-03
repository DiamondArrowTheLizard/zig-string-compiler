using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
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
    private bool _userWantsToQuit;

    public MainWindow()
    {
        InitializeComponent();
        _editor = this.FindControl<TextEditor>("Editor")!;
        DataContextChanged += OnDataContextChanged;
        _editor.TextArea.Caret.PositionChanged += OnCaretChanged;
        Closing += OnClosing;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.SetEditor(_editor);
        }
    }

    private void OnCaretChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var caret = _editor.TextArea.Caret;
            vm.StatusText = $"Ln {caret.Line}, Col {caret.Column}";
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!_userWantsToQuit)
        {
            e.Cancel = true;
            Task.Run(() => ShowCloseConfirmation());
        }
    }

    private async Task ShowCloseConfirmation()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (DataContext is MainWindowViewModel vm && vm.IsModified)
            {
                var dialog = new SaveBeforeCloseDialog();
                var result = await dialog.ShowDialog<SaveBeforeCloseResult>(this);

                switch (result)
                {
                    case SaveBeforeCloseResult.Save:
                        bool saved = await vm.SaveChangesAsync();
                        if (saved)
                        {
                            _userWantsToQuit = true;
                            Close();
                        }
                        break;
                    case SaveBeforeCloseResult.Discard:
                        _userWantsToQuit = true;
                        Close();
                        break;
                    case SaveBeforeCloseResult.Cancel:
                        // остаёмся в приложении
                        break;
                }
            }
            else
            {
                _userWantsToQuit = true;
                Close();
            }
        });
    }

    private void OnLexerNavigate(TokenInfo token)
    {
        try
        {
            int line = (int)token.Line;
            int startCol = (int)token.StartColumn;
            int endCol = (int)token.EndColumn;
            int length = endCol - startCol + 1;
            int col1 = startCol + 1;

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
            int line = error.Line;
            int col = error.Column;
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