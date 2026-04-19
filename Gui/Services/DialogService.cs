using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace Gui.Services;

public class DialogService : IDialogService
{
    private Window? _owner;

    public void SetOwner(Window owner) => _owner = owner;

    public async Task ShowMessageAsync(string title, string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
        if (_owner != null)
            await box.ShowWindowDialogAsync(_owner);
        else
            await box.ShowAsync();
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.YesNo);
        var result = _owner != null
            ? await box.ShowWindowDialogAsync(_owner)
            : await box.ShowAsync();
        return result == ButtonResult.Yes;
    }

    public Task<string?> ShowInputAsync(string title, string message)
    {
        // Placeholder – not implemented
        return Task.FromResult<string?>(null);
    }
}