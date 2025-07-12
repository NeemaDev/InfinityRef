using InfinityRef.UI.Rendering;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityRef
{
    public partial class MainPage
    {
        /// <summary>
        /// Handles the paint surface event to render the current canvas layers onto the provided SkiaSharp canvas.
        /// </summary>
        /// <remarks>This method clears the canvas to a transparent background before rendering each layer
        /// from the current canvas in the <see cref="MainViewModel"/>. The layers are drawn in the order they appear
        /// in the collection.</remarks>
        /// <param name="sender">The source of the event. Typically the control triggering the paint operation.</param>
        /// <param name="e">The event arguments containing the SkiaSharp surface to be painted.</param>
        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            hitTestBuffer.Clear();

            // Move origin by panning.
            canvas.Translate(canvasInteractionService.CanvasTranslate.X, canvasInteractionService.CanvasTranslate.Y);

            // Apply zoom.
            canvas.Scale(canvasInteractionService.CurrentScale, canvasInteractionService.CurrentScale);

            // Calculate pixel-per-dip factors.
            var viewWidthDip = (float)CanvasView.Width;
            var viewHeightDip = (float)CanvasView.Height;
            float pixelPerDipX = (viewWidthDip > 0) ? e.Info.Width / viewWidthDip : 1f;
            float pixelPerDipY = (viewHeightDip > 0) ? e.Info.Height / viewHeightDip : 1f;

            // Draw each layer and stash its rectangle for hit testing.
            foreach (var layer in mainViewModel.CurrentCanvas.Layers)
            {
                var bounds = LayerRenderer.Draw(layer, canvas, (pixelPerDipX, pixelPerDipY));
                hitTestBuffer.Add((layer, bounds));
            }

            // only temporary.
            DrawRulers(canvas, e.Info.Width, e.Info.Height);
        }
    }
}
