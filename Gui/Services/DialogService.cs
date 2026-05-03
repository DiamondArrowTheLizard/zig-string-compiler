using Gui.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Gui.Services;

public class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public DialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ConfirmationDialog(title, $"{message}\n\nPress any button to close.");
        return dialog.ShowDialog<bool>(_serviceProvider.GetRequiredService<MainWindow>());
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var dialog = new ConfirmationDialog(title, message);
        var result = await dialog.ShowDialog<bool>(_serviceProvider.GetRequiredService<MainWindow>());
        return result;
    }
}