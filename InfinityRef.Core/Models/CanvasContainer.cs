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

        public override void Draw(SKCanvas canvas)
        {
            base.Draw(canvas);
        }

        public void OnTap()
        {

        }

        public void OnDrag(SKPoint delta)
        {

        }
    }
}
