using SkiaSharp;

namespace InfinityRef.Core.Models
{
    public class CanvasContainer : Layer
    {
        public Canvas Canvas { get; set; }

        public CanvasContainer()
        {
            Canvas = new Canvas();
        }

        public void OnTap()
        {

        }

        public void OnDrag(SKPoint delta)
        {

        }
    }
}
