namespace Moba.Shared.Service;

using Moba.Backend.Model;

using System.Threading.Tasks;

public interface IIoService
{
    Task<(Solution? solution, string? path, string? error)> LoadAsync();
    Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath);
}