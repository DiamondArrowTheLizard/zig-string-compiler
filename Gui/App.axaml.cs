using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Gui.Services;
using Gui.ViewModels;
using Gui.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Gui;

public partial class App : Application
{
    public IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<HelpViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<ParserResultsViewModel>();
        services.AddTransient<LexerResultsViewModel>();
        services.AddTransient<SemanticResultsViewModel>();
        services.AddTransient<RegexSearchViewModel>();
        services.AddTransient<SemanticResultsViewModel>();
        services.AddTransient<RegexSearchViewModel>();
        services.AddTransient<IrOptimizationViewModel>();

        services.AddTransient<MainWindow>(provider => new MainWindow
        {
            DataContext = provider.GetRequiredService<MainWindowViewModel>()
        });
    }
}