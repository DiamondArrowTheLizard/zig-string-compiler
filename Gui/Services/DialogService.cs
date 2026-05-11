using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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

    private Window? GetMainWindow()
    {
        return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }

    public Task ShowMessageAsync(string title, string message)
    {
        var owner = GetMainWindow();
        if (owner == null)
            return Task.CompletedTask;

        var dialog = new ConfirmationDialog(title, $"{message}\n\nНажмите любую клавишу для закрытия.");
        return dialog.ShowDialog<bool>(owner);
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var owner = GetMainWindow();
        if (owner == null)
            return false;

        var dialog = new ConfirmationDialog(title, message);
        return await dialog.ShowDialog<bool>(owner);
    }
}