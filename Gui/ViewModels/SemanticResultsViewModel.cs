using CommunityToolkit.Mvvm.ComponentModel;
using Gui.Models;
using System.Collections.ObjectModel;

namespace Gui.ViewModels;

public partial class SemanticResultsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _astTreeText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SemanticErrorInfo> _errors = new();
}