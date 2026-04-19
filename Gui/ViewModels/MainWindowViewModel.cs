using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Lexer;
using Gui.Models;
using Gui.Services;
using Gui.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gui.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly IServiceProvider _serviceProvider;
    private TextEditor? _editor;

    [ObservableProperty]
    private string _editorText = string.Empty;

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private ObservableCollection<TokenInfo> _tokens = new();

    [ObservableProperty]
    private TokenInfo? _selectedToken;

    [ObservableProperty]
    private string _statusText = "Ready";

    public TextDocument Document { get; } = new();

    public MainWindowViewModel(IFileService fileService, IDialogService dialogService, IServiceProvider serviceProvider)
    {
        _fileService = fileService;
        _dialogService = dialogService;
        _serviceProvider = serviceProvider;

        Document.TextChanged += (s, e) =>
        {
            EditorText = Document.Text;
            IsModified = true;
        };
    }

    public void SetEditor(TextEditor editor)
    {
        _editor = editor;
    }

    partial void OnSelectedTokenChanged(TokenInfo? value)
    {
        if (value != null)
        {
            TokenSelected?.Invoke(value);
        }
    }

    public event Action<TokenInfo>? TokenSelected;

    [RelayCommand]
    private async Task NewFile()
    {
        if (!await ConfirmDiscardChanges()) return;
        Document.Text = string.Empty;
        CurrentFilePath = string.Empty;
        IsModified = false;
        Tokens.Clear();
        StatusText = "New file created";
    }

    [RelayCommand]
    private async Task OpenFile()
    {
        if (!await ConfirmDiscardChanges()) return;
        var content = await _fileService.OpenFileAsync();
        if (content != null)
        {
            Document.Text = content;
            CurrentFilePath = string.Empty;
            IsModified = false;
            Tokens.Clear();
            StatusText = "File opened";
        }
    }

    [RelayCommand]
    private async Task SaveFile()
    {
        if (await _fileService.SaveFileAsync(CurrentFilePath, Document.Text))
        {
            IsModified = false;
            StatusText = "File saved";
        }
        else
        {
            await _dialogService.ShowMessageAsync("Error", "Could not save file.");
        }
    }

    [RelayCommand]
    private async Task SaveFileAs()
    {
        if (await _fileService.SaveFileAsAsync(Document.Text))
        {
            IsModified = false;
            StatusText = "File saved";
        }
    }

    [RelayCommand]
    private async Task Exit()
    {
        if (await ConfirmDiscardChanges())
            Environment.Exit(0);
    }

    [RelayCommand]
    private void Undo()
    {
        Document.UndoStack.Undo();
    }

    [RelayCommand]
    private void Redo()
    {
        Document.UndoStack.Redo();
    }

    [RelayCommand]
    private void Cut()
    {
        _editor?.Cut();
    }

    [RelayCommand]
    private void Copy()
    {
        _editor?.Copy();
    }

    [RelayCommand]
    private void Paste()
    {
        _editor?.Paste();
    }

    [RelayCommand]
    private void Delete()
    {
        if (_editor != null && _editor.TextArea.Selection.Length > 0)
        {
            _editor.TextArea.Selection.ReplaceSelectionWithText(string.Empty);
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        _editor?.SelectAll();
    }

    [RelayCommand]
    private void RunParser()
    {
        Tokens.Clear();
        var lexer = new Lexer();
        using var reader = new StringReader(Document.Text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lexer.ParseLine(line);
        }

        foreach (var node in lexer.Nodes)
        {
            Tokens.Add(new TokenInfo(
                node.TokenCurrent,
                node.TokenDesc ?? "UNKNOWN",
                node.WordCurrent ?? "",
                node.Line,
                node.WordStart,
                node.WordEnd
            ));
        }

        StatusText = $"Parsed {Tokens.Count} tokens.";
    }

    [RelayCommand]
    private void ShowHelp()
    {
        var helpVm = _serviceProvider.GetRequiredService<HelpViewModel>();
        var helpWindow = new HelpWindow { DataContext = helpVm };
        helpWindow.Show();
    }

    [RelayCommand]
    private void ShowAbout()
    {
        var aboutVm = _serviceProvider.GetRequiredService<AboutViewModel>();
        var aboutWindow = new AboutWindow { DataContext = aboutVm };

        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow != null)
            aboutWindow.ShowDialog(mainWindow);
        else
            aboutWindow.Show();
    }

    [RelayCommand]
    private void ShowTask() => ShowInfo("Постановка задачи", "Описание задания...");
    [RelayCommand]
    private void ShowGrammar() => ShowInfo("Грамматика", "Описание грамматики...");
    [RelayCommand]
    private void ShowGrammarClass() => ShowInfo("Классификация грамматики", "...");
    [RelayCommand]
    private void ShowMethod() => ShowInfo("Метод анализа", "...");
    [RelayCommand]
    private void ShowTest() => ShowInfo("Тестовый пример", "...");
    [RelayCommand]
    private void ShowReferences() => ShowInfo("Список литературы", "...");
    [RelayCommand]
    private void ShowSource() => ShowInfo("Исходный код программы", "...");

    private void ShowInfo(string title, string message) =>
        _ = _dialogService.ShowMessageAsync(title, message);

    private async Task<bool> ConfirmDiscardChanges()
    {
        if (!IsModified) return true;
        return await _dialogService.ShowConfirmationAsync("Unsaved Changes",
            "You have unsaved changes. Do you want to discard them?");
    }
}