using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gui.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    public ConfirmationDialog(string title, string message) : this()
    {
        Title = title;
        DataContext = new ConfirmationViewModel(this, message);
    }
}

internal partial class ConfirmationViewModel : ObservableObject
{
    private readonly Window _window;
    [ObservableProperty] private string _message;

    public ConfirmationViewModel(Window window, string message)
    {
        _window = window;
        _message = message;
    }

    [RelayCommand]
    private void Yes()
    {
        _window.Close(true);
    }

    [RelayCommand]
    private void No()
    {
        _window.Close(false);
    }
}