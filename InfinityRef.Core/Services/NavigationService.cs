using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using InfinityRef.Core.Navigation;

namespace InfinityRef.Core.Services
{
    public class NavigationService : INavigationService
    {
        private readonly CanvasStackHandler stackHandler;

        public NavigationService(CanvasStackHandler handler)
        {
            stackHandler = handler;
            ActiveCanvas = new Canvas();
        }

        public Canvas ActiveCanvas { get; private set; } = new();

        public event EventHandler? ActiveCanvasChanged;

        public void GoBack()
        {
            var previousCanvas = stackHandler.Pop();
            if (previousCanvas != null)
            {
                ActiveCanvas = previousCanvas;
            }
        }

        public void OpenCanvas(Canvas canvas)
        {
            ActiveCanvas = canvas;
            ActiveCanvasChanged?.Invoke(this, EventArgs.Empty);
        }

        public void OpenSubCanvas(CanvasContainer container)
        {
            if (ActiveCanvas != null)
            {
                stackHandler.Push(ActiveCanvas);
                ActiveCanvas = container.Canvas;
            }
        }
    }
}
