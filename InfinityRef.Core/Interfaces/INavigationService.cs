using InfinityRef.Core.Models;

namespace InfinityRef.Core.Interfaces
{
    public interface INavigationService
    {
        void OpenCanvas(Canvas canvas);
        void OpenSubCanvas(CanvasContainer container);
        void GoBack();
    }
}
