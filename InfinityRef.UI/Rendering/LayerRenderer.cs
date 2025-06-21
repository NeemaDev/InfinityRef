using InfinityRef.Core.Models;
using SkiaSharp;

namespace InfinityRef.UI.Rendering
{
    public static class LayerRenderer
    {
        public static void Draw(Layer layer, SKCanvas canvas)
        {
            switch (layer)
            {
                case ImageLayer imageLayer:
                    DrawImageLayer(imageLayer, canvas);
                    break;
                case TextLayer textLayer:
                    DrawTextLayer(textLayer, canvas);
                    break;
                default:
                    break;
            }


        }

        private static void DrawImageLayer(ImageLayer imageLayer, SKCanvas canvas)
        {
            // Decode bytes into an SKBitmap.
            using var bitmap = TryDecode(imageLayer.ImageBytes);
            if (bitmap != null)
            {
                // Determine destination rectangle (here: top-left at 0,0)
                var dest = new SKRect(0, 0, bitmap.Width, bitmap.Height);

                // Apply grayscale or other effects.
                using var paint = new SKPaint();
                if (imageLayer.Grayscale)
                {
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                    {
                       .33f, .33f, .33f, 0, 0,
                       .33f, .33f, .33f, 0, 0,
                       .33f, .33f, .33f, 0, 0,
                        0,    0,    0,   1, 0
                    });
                }

                // Handle flipping by scaling around the bitmap’s center.
                canvas.Save();
                if (imageLayer.FlipHorizontal || imageLayer.FlipVertical)
                {
                    float sx = imageLayer.FlipHorizontal ? -1 : 1;
                    float sy = imageLayer.FlipVertical ? -1 : 1;
                    canvas.Scale(sx, sy, bitmap.Width / 2f, bitmap.Height / 2f);
                }

                // Draw it.
                canvas.DrawBitmap(bitmap, dest, paint);
                canvas.Restore();
            }
        }

        private static void DrawTextLayer(TextLayer layer, SKCanvas canvas)
        {
            using var paint = new SKPaint
            {
                Color = new SKColor(layer.Color.R, layer.Color.G, layer.Color.B, layer.Color.A),
                IsAntialias = true,
            };

            using var typeface = SKTypeface.FromFamilyName(layer.FontFamily);
            using var font = new SKFont(typeface, layer.FontSize);
            var align = layer.TextAlign switch
            {
                Enums.HorizontalTextAlignment.Left => SKTextAlign.Left,
                Enums.HorizontalTextAlignment.Center => SKTextAlign.Center,
                Enums.HorizontalTextAlignment.Right => SKTextAlign.Right,
                _ => SKTextAlign.Left
            };

            // 2) choose where to draw
            //    here: top-left corner of canvas
            var x = 0f;
            var y = layer.FontSize;  // draw baseline at fontSize

            // 3) draw it
            canvas.DrawText(layer.Text, x, y, align, font, paint);
        }

        static SKBitmap? TryDecode(byte[] bytes)
        {
            try
            {
                return SKBitmap.Decode(bytes);
            }
            catch (ArgumentNullException)
            {
                // Codec was null → not an image.
                return null;
            }
        }
    }
}
