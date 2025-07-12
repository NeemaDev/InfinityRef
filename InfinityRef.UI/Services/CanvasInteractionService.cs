using SkiaSharp;

namespace InfinityRef.UI.Services
{
    public class CanvasInteractionService
    {
        public float CurrentScale { get; private set; } = 1f;
        public SKPoint CanvasTranslate { get; private set; } = new SKPoint(0, 0);

        private float startScale = 1f;
        private SKPoint startTranslate = new SKPoint(0, 0);

        /// <summary>
        /// Initializes a pinch gesture with the specified scale and translation values.
        /// </summary>
        /// <remarks>This method sets the starting scale and translation values for a pinch gesture, which
        /// can be used to track or manipulate scaling and movement in graphical applications.</remarks>
        /// <param name="scale">The initial scale factor for the pinch gesture. Must be a positive value.</param>
        /// <param name="translate">The initial translation point for the pinch gesture, represented as an <see cref="SKPoint"/>.</param>
        public void StartPinch(float scale, SKPoint translate)
        {
            startScale = scale;
            startTranslate = translate;
        }

        /// <summary>
        /// Updates the current scale and translation of the canvas based on a pinch gesture.
        /// </summary>
        /// <remarks>This method adjusts the canvas scale and translation to reflect the pinch gesture.
        /// The scale is clamped between 0.5 and 4 to ensure reasonable zoom levels. The translation is calculated to
        /// keep the pinch center visually consistent during scaling.</remarks>
        /// <param name="pinchScale">The scale factor of the pinch gesture. Typically greater than 1 for zooming in and less than 1 for zooming
        /// out.</param>
        /// <param name="pinchCenter">The center point of the pinch gesture, in view coordinates.</param>
        /// <param name="viewSize">The size of the view in which the pinch gesture is performed.</param>
        public void UpdatePinch(float pinchScale, SKPoint pinchCenter, SKSize viewSize)
        {
            CurrentScale = (float)Math.Clamp(startScale * pinchScale, 0.5f, 4f);
            var dx = pinchCenter.X * (1 - CurrentScale);
            var dy = pinchCenter.Y * (1 - CurrentScale);
            CanvasTranslate = new SKPoint(startTranslate.X + dx, startTranslate.Y + dy);
        }

        /// <summary>
        /// Initiates a panning operation by setting the starting translation point.
        /// </summary>
        /// <remarks>This method sets the starting point for a panning operation, which can be used to
        /// calculate subsequent translation offsets during user interaction or rendering.</remarks>
        /// <param name="translate">The initial translation point, represented as an <see cref="SKPoint"/>.</param>
        public void StartPan(SKPoint translate)
        {
            startTranslate = translate;
        }

        /// <summary>
        /// Updates the current pan position of the canvas based on the specified movement vector.
        /// </summary>
        /// <remarks>This method adjusts the canvas translation by adding the specified movement vector to
        /// the starting translation. It is typically used to implement panning functionality in graphical
        /// applications.</remarks>
        /// <param name="move">A <see cref="SKPoint"/> representing the movement vector to apply to the canvas. The X and Y values specify
        /// the amount to shift the canvas horizontally and vertically, respectively.</param>
        public void UpdatePan(SKPoint move)
        {
            CanvasTranslate = new SKPoint(startTranslate.X + move.X, startTranslate.Y + move.Y);
        }

        /// <summary>
        /// Updates the zoom level of the canvas based on the specified zoom factor and pointer position.
        /// </summary>
        /// <remarks>The method adjusts the current scale of the canvas within a constrained range of 1e-6
        /// to 100. If the zoom factor results in a significant change to the scale, the canvas translation is updated
        /// to ensure the pointer remains the focal point of the zoom.</remarks>
        /// <param name="zoomFactor">The factor by which to adjust the zoom level. Values greater than 1 zoom in, while values less than 1 zoom
        /// out.</param>
        /// <param name="pointer">The position of the pointer, in canvas coordinates, used as the focal point for the zoom operation.</param>
        public void UpdateWheelZoom(float zoomFactor, SKPoint pointer)
        {
            var oldScale = CurrentScale;
            var newScale = Math.Clamp(CurrentScale * zoomFactor, 1e-6f, 100f);
            if (Math.Abs(newScale - oldScale) > float.Epsilon)
            {
                CurrentScale = newScale;
                CanvasTranslate = new SKPoint(
                    (CanvasTranslate.X - pointer.X) * zoomFactor + pointer.X,
                    (CanvasTranslate.Y - pointer.Y) * zoomFactor + pointer.Y
                );
            }
        }

        /// <summary>
        /// Converts a point from device coordinates to canvas coordinates.
        /// </summary>
        /// <remarks>This method applies the current scale and translation values to transform the device
        /// point into the corresponding canvas point. The result reflects the canvas's current zoom and pan
        /// state.</remarks>
        /// <param name="devicePoint">The point in device coordinates to be converted.</param>
        /// <returns>A <see cref="SKPoint"/> representing the equivalent point in canvas coordinates.</returns>
        public SKPoint DeviceToCanvas(SKPoint devicePoint)
        {
            var x = (devicePoint.X - CanvasTranslate.X) / CurrentScale;
            var y = (devicePoint.Y - CanvasTranslate.Y) / CurrentScale;
            return new SKPoint(x, y);
        }
    }
}
