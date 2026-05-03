using Avalonia.Controls;
using Avalonia.Interactivity;
using Gui.Models;
using System;

namespace Gui.Views;

public partial class ParserResultsView : UserControl
{
    public event Action<ParserErrorInfo>? NavigateTo;

    public ParserResultsView()
    {
        InitializeComponent();
    }

    private void OnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ParserResultsViewModel vm && vm.SelectedItem is ParserErrorInfo error)
        {
            NavigateTo?.Invoke(error);
        }
    }
}