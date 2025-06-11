using SkiaSharp;

namespace InfinityRef.Core.Models
{
    public class ImageLayer : Layer
    {
        private SKBitmap Bitmap { get; set; }
        private bool FlipHorizontal { get; set; }
        private bool FlipVertical { get; set; }
        private bool Grayscale { get; set; }

        public override void Draw(SKCanvas canvas)
        {
            base.Draw(canvas);
        }
    }
}
