using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Lexer;
using Core.Parser;
using Core.Ast;
using Gui.Models;
using Gui.Services;
using Gui.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Semantic;

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
    private LexerResultsViewModel _lexerResults = new();

    [ObservableProperty]
    private ParserResultsViewModel _parserResults = new();

    [ObservableProperty]
    private SemanticResultsViewModel _semanticResults = new();

    [ObservableProperty]
    private RegexSearchViewModel _regexSearch = new();

    [ObservableProperty]
    private IrOptimizationViewModel _irOptimization = new();

    [ObservableProperty]
    private string _statusText = "Готов";

    public TextDocument Document { get; } = new();

    public Func<Task<bool>>? ConfirmDiscardRequested { get; set; }

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

    public async Task<bool> ConfirmDiscardChanges()
    {
        if (!IsModified) return true;
        if (ConfirmDiscardRequested != null)
            return await ConfirmDiscardRequested();
        return true;
    }


    public async Task<bool> SaveChangesAsync()
    {
        if (await _fileService.SaveFileAsync(CurrentFilePath, Document.Text))
        {
            IsModified = false;
            StatusText = "Файл Сохранён";
            return true;
        }
        else
        {
            await _dialogService.ShowMessageAsync("Error", "Файл не был сохранён.");
            return false;
        }
    }

    [RelayCommand]
    private async Task NewFile()
    {
        if (!await ConfirmDiscardChanges()) return;
        Document.Text = string.Empty;
        CurrentFilePath = string.Empty;
        IsModified = false;
        LexerResults.Items.Clear();
        ParserResults.Items.Clear();
        StatusText = "Создан новый файл";
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
            LexerResults.Items.Clear();
            ParserResults.Items.Clear();
            StatusText = "Открыт файл";
        }
    }

    [RelayCommand]
    private async Task SaveFile()
    {
        await SaveChangesAsync();
    }

    [RelayCommand]
    private async Task SaveFileAs()
    {
        if (await _fileService.SaveFileAsAsync(Document.Text))
        {
            IsModified = false;
            StatusText = "Файл Сохранён";
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
        LexerResults.Items.Clear();
        ParserResults.Items.Clear();

        var lexer = new Lexer();
        using var reader = new StringReader(Document.Text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lexer.ParseLine(line);
        }

        var allTokens = lexer.Nodes.Select(n => new TokenInfo(
            n.TokenCurrent,
            n.TokenDesc ?? "Неизвестный токен",
            n.WordCurrent ?? "",
            n.Line,
            n.WordStart,
            n.WordEnd
        )).ToList();

        var errorTokens = allTokens.Where(t =>
            t.Type == Core.Lexer.Token.Unknown ||
            t.Type == Core.Lexer.Token.UnknownNoConst).ToList();

        foreach (var token in (errorTokens.Any() ? errorTokens : allTokens))
        {
            LexerResults.Items.Add(token);
        }

        var parser = new Parser();
        var result = parser.Parse(lexer.Nodes, lexer.Dictionary);
        foreach (var error in result.Errors)
        {
            ParserResults.Items.Add(new ParserErrorInfo(error.Fragment, error.Location, error.Description));
        }

        StatusText = $"Токены: {allTokens.Count}, Лексические ошибки: {errorTokens.Count}, Ошибок парсера: {ParserResults.Items.Count}";

        if (result.Success)
        {
            var astBuilder = new AstBuilder();
            var programAst = astBuilder.Build(lexer.Nodes);

            var semanticAnalyzer = new SemanticAnalyzer();
            var semanticResult = semanticAnalyzer.Analyze(programAst);

            SemanticResults.AstTreeText = programAst.ToTreeString();

            SemanticResults.Errors.Clear();
            foreach (var error in semanticResult.Errors)
            {
                SemanticResults.Errors.Add(new SemanticErrorInfo(error.Location, error.Description));
            }

            if (semanticResult.Success)
            {
                StatusText = "Анализ успешно завершен. Ошибок нет.";

                var irGenerator = new Core.Ir.IrGenerator();
                var initialIr = irGenerator.Generate(programAst.Declarations);
                IrOptimization.InputIrText = string.Join(System.Environment.NewLine, initialIr);

                var irOptimizer = new Core.Ir.IrOptimizer();
                var optimizedIr = irOptimizer.Optimize(initialIr);
                IrOptimization.OptimizedIrText = string.Join(System.Environment.NewLine, optimizedIr);
            }
            else
            {
                StatusText = $"Обнаружены семантические ошибки ({semanticResult.Errors.Count}).";
            }
        }
        else
        {
            SemanticResults.AstTreeText = "Ошибка синтаксического анализа. Дерево AST не может быть построено.";
            SemanticResults.Errors.Clear();
        }
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
    private void ShowTask()
    {
        var taskWindow = new TaskWindow();

        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow != null)
            taskWindow.ShowDialog(mainWindow);
        else
            taskWindow.Show();
    }

    [RelayCommand]
    private void ShowGrammar()
    {
        var window = new GrammarWindow();
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow != null) window.ShowDialog(mainWindow);
        else window.Show();
    }

    [RelayCommand]
    private void ShowGrammarClass()
    {
        var window = new GrammarClassWindow();
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (mainWindow != null)
            window.ShowDialog(mainWindow);
        else
            window.Show();
    }

    [RelayCommand]
    private void ShowMethod()
    {
        var window = new MethodWindow();
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (mainWindow != null)
            window.ShowDialog(mainWindow);
        else
            window.Show();
    }

    [RelayCommand]
    private void ShowTest()
    {
        var window = new TestWindow();
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (mainWindow != null)
            window.ShowDialog(mainWindow);
        else
            window.Show();
    }

    [RelayCommand]
    private void ShowReferences()
    {
        var window = new ReferencesWindow();
        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (mainWindow != null)
            window.ShowDialog(mainWindow);
        else
            window.Show();
    }

    [RelayCommand]
    private void ShowSource()
    {
        var sourceWindow = new SourceCodeWindow();

        var mainWindow = (App.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (mainWindow != null)
            sourceWindow.ShowDialog(mainWindow);
        else
            sourceWindow.Show();
    }

    private void ShowInfo(string title, string message) =>
        _ = _dialogService.ShowMessageAsync(title, message);
}