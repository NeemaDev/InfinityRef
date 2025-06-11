using SkiaSharp;

namespace InfinityRef.Core.Models
{
    public class CanvasContainer : Layer
    {
        private Canvas Canvas { get; set; }
        private SKBitmap FolderIcon { get; set; }

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
