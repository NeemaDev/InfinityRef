using InfinityRef.Core.Models;

namespace InfinityRef.Core.Interfaces
{
    public interface INavigationService
    {
        Canvas ActiveCanvas { get; }
        event EventHandler? ActiveCanvasChanged;

        void OpenCanvas(Canvas canvas);
        void OpenSubCanvas(CanvasContainer container);
        void GoBack();
    }
}
