using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using InfinityRef.Core.Navigation;

namespace InfinityRef.Core.Services
{
    public class NavigationService : INavigationService
    {
        private readonly CanvasStackHandler stackHandler;
        private Canvas activeCanvas;

        public NavigationService(CanvasStackHandler handler, Canvas initialCanvas)
        {
            stackHandler = handler;
            activeCanvas = initialCanvas ?? throw new ArgumentNullException(nameof(initialCanvas), "Initial canvas cannot be null.");
        }

        public void GoBack()
        {
            var previousCanvas = stackHandler.Pop();
            if (previousCanvas != null)
            {
                activeCanvas = previousCanvas;
            }
        }

        public void OpenCanvas(Canvas canvas)
        {
            activeCanvas = canvas;
        }

        public void OpenSubCanvas(CanvasContainer container)
        {
            if (activeCanvas != null)
            {
                stackHandler.Push(activeCanvas);
                activeCanvas = container.Canvas;
            }
        }
    }
}
