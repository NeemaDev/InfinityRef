using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityRef
{
    public partial class MainPage
    {
#if WINDOWS
        private bool isMousePanning = false;
        private bool isSpaceDown = false;
        private SKPoint mousePanStart;
#endif

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {

        }

        /// <summary>
        /// Handles updates to a pinch gesture, including starting, running, and completing the gesture.
        /// </summary>
        /// <remarks>This method processes pinch gestures by delegating to the <see
        /// cref="canvasInteractionService"/> to manage scaling and translation of the canvas. It updates the canvas
        /// view during the gesture and ensures the visual changes are applied. <para> The gesture status determines the
        /// behavior: <list type="bullet"> <item><description><see cref="GestureStatus.Started"/>: Initializes the pinch
        /// operation by recording the current scale and translation.</description></item> <item><description><see
        /// cref="GestureStatus.Running"/>: Updates the scale and translation based on the pinch center and scale
        /// origin, and refreshes the canvas.</description></item> <item><description><see
        /// cref="GestureStatus.Completed"/> or <see cref="GestureStatus.Canceled"/>: No additional action is
        /// taken.</description></item> </list> </para></remarks>
        /// <param name="sender">The source of the gesture event, typically the control that initiated the gesture.</param>
        /// <param name="e">The event arguments containing details about the pinch gesture, such as its status, scale, and origin.</param>
        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started:
                    // Rememer the initial scale and translation.
                    canvasInteractionService.StartPinch(canvasInteractionService.CurrentScale, canvasInteractionService.CanvasTranslate);
                    break;
                case GestureStatus.Running:
                    // Calculate the new translation based on the pinch center and scale origin.
                    var viewSize = CanvasView.CanvasSize;
                    var pinchCenter = new SKPoint((float)(viewSize.Width * e.ScaleOrigin.X), (float)(viewSize.Height * e.ScaleOrigin.Y));

                    canvasInteractionService.UpdatePinch((float)e.Scale, pinchCenter, viewSize);

                    CanvasView.InvalidateSurface(); // Refresh the canvas to apply the new scale and translation.
                    break;
                case GestureStatus.Completed:
                    break;
                case GestureStatus.Canceled:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles touch events on the canvas.
        /// </summary>
        /// <remarks>This method processes touch events and updates the last tap location when the touch
        /// action is a press. The event is marked as handled to prevent further propagation.</remarks>
        /// <param name="sender">The source of the touch event, typically the canvas.</param>
        /// <param name="e">The touch event arguments containing details about the touch action.</param>
        private void OnCanvasTouch(object sender, SKTouchEventArgs e)
        {
            if (e.InContact)
            {
                activeTouches[e.Id] = e.Location;
            }
            else
            {
                activeTouches.Remove(e.Id);
            }

            // Handle two finger panning.
            if (activeTouches.Count == 2)
            {
                var points = activeTouches.Values.ToArray();
                if (!isTouchPanning)
                {
                    isTouchPanning = true;
                    canvasInteractionService.StartPan(canvasInteractionService.CanvasTranslate);
                }

                // For simplicity, use the average movement of both fingers
                var avgCurrent = new SKPoint((points[0].X + points[1].X) / 2, (points[0].Y + points[1].Y) / 2);

                if (e.ActionType == SKTouchAction.Moved)
                {
                    // Calculate movement delta from last tap point
                    var move = avgCurrent - lastTapPoint;
                    canvasInteractionService.UpdatePan(move);
                    CanvasView.InvalidateSurface();
                }
            }
            else
            {
                isTouchPanning = false;
            }

            // Handle "clicking" on the canvas with the finger.
            if (e.ActionType == SKTouchAction.Pressed)
            {
                lastTapPoint = e.Location;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the tap event on the canvas at the specified point.
        /// </summary>
        private void OnCanvasTapped(object sender, EventArgs e)
        {
            HandleHitTest(lastTapPoint);
        }

#if WINDOWS

        private void OnPointerWheelChanged(object? sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            // Get wheel delta (positve for zoom in, negative for zoom out).
            var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
            var zoomFactor = delta > 0 ? 1.1f : 0.9f;

            // Pointer position relative to canvas.
            var pointer = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)sender)?.Position;
            
            if(pointer.HasValue){
                var x = (float)pointer.Value.X;
                var y = (float)pointer.Value.Y;

                canvasInteractionService.UpdateWheelZoom(zoomFactor, new SKPoint(x, y));
                CanvasView.InvalidateSurface();
            }

            e.Handled = true; // Mark the event as handled to prevent further propagation.
        }

        private void OnPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var props = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)sender).Properties;
            if (props.IsMiddleButtonPressed || (isSpaceDown && props.IsLeftButtonPressed))
            {
                isMousePanning = true;
                var pos = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)sender).Position;
                var startPoint = new SKPoint((float)pos.X, (float)pos.Y);

                // Store the start point for delta calculation
                mousePanStart = startPoint;

                // Delegate to service
                canvasInteractionService.StartPan(canvasInteractionService.CanvasTranslate);
                ((Microsoft.UI.Xaml.UIElement)sender).CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            isMousePanning = false;
            ((Microsoft.UI.Xaml.UIElement)sender).ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void OnPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (isMousePanning)
            {
                var pos = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)sender).Position;
                var currentPoint = new SKPoint((float)pos.X, (float)pos.Y);
                var delta = new SKPoint(currentPoint.X - mousePanStart.X, currentPoint.Y - mousePanStart.Y);
                canvasInteractionService.UpdatePan(delta);
                CanvasView.InvalidateSurface();
                e.Handled = true;
            }
        }

        private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
                isSpaceDown = true;
        }

        private void OnKeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
                isSpaceDown = false;
        }
#endif

    }
}
