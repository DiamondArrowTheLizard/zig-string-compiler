using CommunityToolkit.Mvvm.ComponentModel;
using Gui.Models;
using System.Collections.ObjectModel;

namespace Gui.ViewModels;

public partial class LexerResultsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TokenInfo> _items = new();

    [ObservableProperty]
    private TokenInfo? _selectedItem;
}