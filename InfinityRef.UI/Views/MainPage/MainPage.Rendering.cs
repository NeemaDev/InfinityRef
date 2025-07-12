using InfinityRef.UI.Rendering;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityRef
{
    public partial class MainPage
    {

        /// <summary>
        /// Handles the paint event for the canvas surface, rendering the current layers and applying transformations
        /// such as panning and zooming.
        /// </summary>
        /// <remarks>This method clears the canvas, applies transformations based on the current panning
        /// and zoom settings, and renders all layers defined in the current canvas. It also calculates pixel-per-dip
        /// factors to ensure proper scaling and stores layer bounds for hit testing.</remarks>
        /// <param name="sender">The source of the event, typically the canvas view.</param>
        /// <param name="e">The event arguments containing information about the surface to be painted, including the canvas and its
        /// dimensions.</param>
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
