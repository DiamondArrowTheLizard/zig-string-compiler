using System.Threading.Tasks;

namespace Gui.Services;

public interface IFileService
{
    Task<string?> OpenFileAsync();
    Task<bool> SaveFileAsync(string? currentPath, string content);
    Task<bool> SaveFileAsAsync(string content);
}