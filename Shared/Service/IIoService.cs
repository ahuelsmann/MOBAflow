namespace Moba.Shared.Service;

using System.Threading.Tasks;
using Moba.Backend.Model;

public interface IIoService
{
    Task<(Solution? solution, string? path, string? error)> LoadAsync();
    Task<(bool success, string? path, string? error)> SaveAsync(Solution solution, string? currentPath);
}