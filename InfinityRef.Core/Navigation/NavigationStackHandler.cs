using InfinityRef.Core.Models;

namespace InfinityRef.Core.Navigation
{
    public class NavigationStackHandler
    {
        private Stack<Canvas> navigationStack;
        private Canvas ActiveCanvas { get; set; }

        public void OpenCanvas(Canvas canvas)
        {

        }

        public void OpenSubCanvas(CanvasContainer container)
        {

        }

        public void GoBack()
        {
            if (navigationStack.Count > 0)
            {
                ActiveCanvas = navigationStack.Pop();
            }
        }
    }
}
