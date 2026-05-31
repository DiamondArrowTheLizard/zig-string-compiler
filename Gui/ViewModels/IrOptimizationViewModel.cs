using CommunityToolkit.Mvvm.ComponentModel;

namespace Gui.ViewModels;

public partial class IrOptimizationViewModel : ObservableObject
{
    [ObservableProperty]
    private string _inputIrText = string.Empty;

    [ObservableProperty]
    private string _optimizedIrText = string.Empty;
}