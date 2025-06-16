using SkiaSharp;

namespace InfinityRef.Core.Models
{
    public class ImageLayer : Layer
    {
        public SKBitmap Bitmap { get; set; }
        public bool FlipHorizontal { get; set; }
        public bool FlipVertical { get; set; }
        public bool Grayscale { get; set; }

        public override void Draw(SKCanvas canvas)
        {
            base.Draw(canvas);
        }
    }
}
