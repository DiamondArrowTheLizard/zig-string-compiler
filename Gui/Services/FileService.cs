using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Gui.Services;

public class FileService : IFileService
{
    private readonly Window _targetWindow;

    public FileService()
    {
        _targetWindow = new Window(); 
    }

    public void SetTargetWindow(Window window)
    {
        
    }

    public async Task<string?> OpenFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(_targetWindow);
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false,
            FileTypeFilter = new[] { FilePickerFileTypes.TextPlain, FilePickerFileTypes.All }
        });

        if (files.Count == 0) return null;

        var file = files[0];
        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<bool> SaveFileAsync(string? currentPath, string content)
    {
        if (string.IsNullOrEmpty(currentPath))
            return await SaveFileAsAsync(content);

        try
        {
            await File.WriteAllTextAsync(currentPath, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveFileAsAsync(string content)
    {
        var topLevel = TopLevel.GetTopLevel(_targetWindow);
        if (topLevel is null) return false;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Text File",
            DefaultExtension = "txt",
            FileTypeChoices = new[] { FilePickerFileTypes.TextPlain }
        });

        if (file is null) return false;

        await using var stream = await file.OpenWriteAsync();
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
        return true;
    }
}