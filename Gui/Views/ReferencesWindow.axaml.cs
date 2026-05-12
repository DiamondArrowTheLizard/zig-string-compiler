using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Gui.Views;

public partial class ReferencesWindow : Window
{
    public ReferencesWindow()
    {
        InitializeComponent();
    }

    private void OpenLink_Click(object? sender, RoutedEventArgs e)
    {
        var url = "https://dispace.edu.nstu.ru/didesk/course/show/8594";
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
    }
}