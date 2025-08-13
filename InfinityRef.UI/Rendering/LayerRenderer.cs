using InfinityRef.Core.Models;
using SkiaSharp;

namespace InfinityRef.UI.Rendering
{
    /// <summary>
    /// Provides functionality for rendering various types of layers onto a canvas.
    /// </summary>
    /// <remarks>The <see cref="LayerRenderer"/> class supports rendering different layer types, such as image
    /// layers and text layers,  onto an <see cref="SKCanvas"/>. The appropriate rendering method is selected based on
    /// the type of the provided layer. Unsupported layer types result in no rendering and return an empty
    /// rectangle.</remarks>
    public static class LayerRenderer
    {
        /// <summary>
        /// Draws the specified layer onto the provided canvas, applying the given scale factors.
        /// </summary>
        /// <param name="layer">The layer to be drawn. Must be of a supported type, such as <see cref="ImageLayer"/> or <see
        /// cref="TextLayer"/>.</param>
        /// <param name="canvas">The canvas on which the layer will be drawn. Cannot be <see langword="null"/>.</param>
        /// <param name="scaleFactors">A tuple containing the horizontal and vertical scale factors to apply during drawing.</param>
        /// <returns>A <see cref="SKRect"/> representing the bounding rectangle of the drawn content.  Returns <see
        /// cref="SKRect.Empty"/> if the layer type is unsupported or no content is drawn.</returns>
        public static SKRect Draw(Layer layer, SKCanvas canvas, (float, float) scaleFactors)
        {
            switch (layer)
            {
                case ImageLayer imageLayer:
                    return DrawImageLayer(imageLayer, canvas, scaleFactors);
                case TextLayer textLayer:
                    return DrawTextLayer(textLayer, canvas, scaleFactors);
                default:
                    return SKRect.Empty; // Unsupported layer type, do nothing.
            }
        }

        /// <summary>
        /// Renders an image layer onto the specified canvas, applying optional transformations and effects.
        /// </summary>
        /// <remarks>This method decodes the image data from the <see cref="ImageLayer.ImageBytes"/>
        /// property and draws it onto the canvas. Optional effects such as grayscale and flipping (horizontal or
        /// vertical) are applied based on the properties of the <paramref name="imageLayer"/>. If the layer is marked
        /// as selected, a selection rectangle is drawn around the image.</remarks>
        /// <param name="imageLayer">The image layer to render, containing image data and transformation settings.</param>
        /// <param name="canvas">The canvas on which the image layer will be drawn.</param>
        private static SKRect DrawImageLayer(ImageLayer imageLayer, SKCanvas canvas, (float x, float y) scaleFactors)
        {
            // Decode bytes into an SKBitmap.
            using var bitmap = TryDecode(imageLayer.ImageBytes);
            if (bitmap != null)
            {
                // Determine destination rectangle. SKRect uses left, top, right, bottom coordinates.
                var scaledX = imageLayer.Position.X * scaleFactors.x;
                var scaledY = imageLayer.Position.Y * scaleFactors.y;

                var dest = new SKRect(scaledX, scaledY, scaledX + bitmap.Width, scaledY + bitmap.Height);

                // Apply grayscale or other effects.
                using var paint = new SKPaint();
                if (imageLayer.Grayscale)
                {
                    paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
                    {
                       .33f, .33f, .33f, 0, 0,
                       .33f, .33f, .33f, 0, 0,
                       .33f, .33f, .33f, 0, 0,
                       0, 0, 0, 1, 0,
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

                if (imageLayer.IsSelected)
                {
                    DrawSelectionRectangle(canvas, dest);
                }

                canvas.Restore();

                return dest; // Return the rectangle where the image was drawn.
            }

            return SKRect.Empty; // Return an empty rectangle if the image could not be decoded.
        }

        /// <summary>
        /// Renders a text layer onto the specified canvas.
        /// </summary>
        /// <remarks>The method uses the properties of the <paramref name="layer"/> parameter, such as
        /// font family, font size, text alignment, and color, to configure the text rendering. The text is drawn
        /// starting at the top-left corner of the canvas, with the baseline positioned at the font size
        /// height.</remarks>
        /// <param name="layer">The <see cref="TextLayer"/> object containing the text, font, color, and alignment information to be drawn.</param>
        /// <param name="canvas">The <see cref="SKCanvas"/> on which the text will be rendered. This cannot be <see langword="null"/>.</param>
        private static SKRect DrawTextLayer(TextLayer layer, SKCanvas canvas, (float x, float y) scaleFactors)
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

            var x = 0f;
            var y = layer.FontSize;  // draw baseline at fontSize
            var width = font.MeasureText(layer.Text, paint);
            var height = layer.FontSize;
            var boundingBoxPadding = 10f; // Padding around the text bounding box

            // Define the destination rectangle for the text.
            var scaledX = layer.Position.X * scaleFactors.x;
            var scaledY = layer.Position.Y * scaleFactors.y;

            var dest = new SKRect(scaledX, scaledY, width + (2 * boundingBoxPadding), height + (2 * boundingBoxPadding));
            canvas.DrawText(layer.Text, x, y, align, font, paint);

            if (layer.IsSelected)
            {
                // Draw selection rectangle around the text.
                DrawSelectionRectangle(canvas, dest);
            }

            return dest;
        }

        /// <summary>
        /// Attempts to decode a byte array into an <see cref="SKBitmap"/> image.
        /// </summary>
        /// <remarks>This method returns <see langword="null"/> if the provided byte array does not
        /// contain valid image data or if the decoding process fails due to invalid input.</remarks>
        /// <param name="bytes">The byte array containing the image data to decode. Must not be <see langword="null"/>.</param>
        /// <returns>An <see cref="SKBitmap"/> representing the decoded image if the operation succeeds; otherwise, <see
        /// langword="null"/>.</returns>
        private static SKBitmap? TryDecode(byte[] bytes)
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

        /// <summary>
        /// Draws a selection rectangle on the specified canvas using a purple border.
        /// </summary>
        /// <remarks>The selection rectangle is drawn with a purple stroke, a width of 4 pixels, and
        /// anti-aliasing enabled.</remarks>
        /// <param name="canvas">The <see cref="SKCanvas"/> on which the selection rectangle will be drawn. Cannot be <see langword="null"/>.</param>
        /// <param name="dest">The <see cref="SKRect"/> defining the bounds of the selection rectangle.</param>
        private static void DrawSelectionRectangle(SKCanvas canvas, SKRect dest)
        {
            var skPrimary = SKColors.Purple; // Default color if not found in resources.

            var theme = Application.Current?.RequestedTheme;
            var colorResource = theme == AppTheme.Dark ? "PrimaryDark" : "Primary";

            if (Application.Current?.Resources?.TryGetValue(colorResource, out var res) == true && res is Color primaryColor)
            {
                skPrimary = new SKColor(
                    (byte)(primaryColor.Red * 255),
                    (byte)(primaryColor.Green * 255),
                    (byte)(primaryColor.Blue * 255),
                    (byte)(primaryColor.Alpha * 255));
            }

            using var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = skPrimary,
                StrokeWidth = 4,
                IsAntialias = true,
            };

            canvas.DrawRect(dest, borderPaint);
        }
    }
}
