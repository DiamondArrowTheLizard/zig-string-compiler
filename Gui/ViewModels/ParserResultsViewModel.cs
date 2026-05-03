using CommunityToolkit.Mvvm.ComponentModel;
using Gui.Models;
using System.Collections.ObjectModel;

namespace Gui.ViewModels;

public partial class ParserResultsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ParserErrorInfo> _items = new();

    [ObservableProperty]
    private ParserErrorInfo? _selectedItem;
}