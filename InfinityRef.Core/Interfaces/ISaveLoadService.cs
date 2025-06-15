using InfinityRef.Core.Models;

namespace InfinityRef.Core.Interfaces
{
    public interface ISaveLoadService
    {
        Task SaveCanvas(Canvas canvas, string filename);
        Task<Canvas?> LoadCanvas(string filename);
    }
}
