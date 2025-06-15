using InfinityRef.Core.Models;

namespace InfinityRef.Core.Interfaces
{
    public interface INavigationService
    {
        Task OpenCanvas(Canvas canvas);
        Task OpenSubCanvas(CanvasContainer container);
        Task GoBack();
    }
}
