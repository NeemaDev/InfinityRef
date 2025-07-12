using InfinityRef.Core.Models;

namespace InfinityRef.Core.Interfaces
{
    public interface INavigationService
    {
        event EventHandler? ActiveCanvasChanged;

        Canvas ActiveCanvas { get; }

        void OpenCanvas(Canvas canvas);
        void OpenSubCanvas(CanvasContainer container);
        void GoBack();
    }
}
