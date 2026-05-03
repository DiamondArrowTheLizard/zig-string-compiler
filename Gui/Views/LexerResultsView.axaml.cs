using Avalonia.Controls;
using Avalonia.Interactivity;
using Gui.Models;
using System;

namespace Gui.Views;

public partial class LexerResultsView : UserControl
{
    public event Action<TokenInfo>? NavigateTo;

    public LexerResultsView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.LexerResultsViewModel vm && vm.SelectedItem is TokenInfo token)
        {
            NavigateTo?.Invoke(token);
        }
    }
}