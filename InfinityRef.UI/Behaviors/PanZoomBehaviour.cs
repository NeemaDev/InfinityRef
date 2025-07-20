using InfinityRef.UI.Interfaces;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
#endif

namespace InfinityRef.Behaviors
{
    public class PanZoomBehaviour : Behavior<SKCanvasView>
    {
        public static readonly BindableProperty CanvasInteractionServiceProperty
            = BindableProperty.Create(nameof(CanvasInteractionService), typeof(ICanvasInteractionService), typeof(PanZoomBehaviour), default(ICanvasInteractionService));

#if WINDOWS
        private bool isMousePanning = false;
        private bool isSpaceDown = false;
        private SKPoint mousePanStart;
#endif
        private PinchGestureRecognizer? pinchGesture;
        private PanGestureRecognizer? panGesture;
        private SKCanvasView? canvasView;

        public ICanvasInteractionService CanvasInteractionService
        {
            get => (ICanvasInteractionService)GetValue(CanvasInteractionServiceProperty);
            set => SetValue(CanvasInteractionServiceProperty, value);
        }

        protected override void OnAttachedTo(SKCanvasView canvas)
        {
            base.OnAttachedTo(canvas);
            canvasView = canvas;

            // Pan.
            panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += (s, e) =>
            {
                if (e.StatusType == GestureStatus.Running)
                {
                    CanvasInteractionService.UpdatePan(new SKPoint((float)e.TotalX, (float)e.TotalY));
                    canvas.InvalidateSurface();
                }
            };

            canvas.GestureRecognizers.Add(panGesture);

            // Pinch.
            pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += (s, e) =>
            {
                switch (e.Status)
                {
                    case GestureStatus.Started:
                        // Rememer the initial scale and translation.
                        CanvasInteractionService.StartPinch(CanvasInteractionService.CurrentScale, CanvasInteractionService.CanvasTranslate);
                        break;
                    case GestureStatus.Running:
                        // Calculate the new translation based on the pinch center and scale origin.
                        var viewSize = canvas.CanvasSize;
                        var pinchCenter = new SKPoint((float)(viewSize.Width * e.ScaleOrigin.X), (float)(viewSize.Height * e.ScaleOrigin.Y));

                        CanvasInteractionService.UpdatePinch((float)e.Scale, pinchCenter, viewSize);
                        canvas.InvalidateSurface(); // Refresh the canvas to apply the new scale and translation.
                        break;
                    case GestureStatus.Completed:
                        break;
                    case GestureStatus.Canceled:
                    default:
                        break;
                }
            };

            canvas.GestureRecognizers.Add(pinchGesture);

#if WINDOWS
            // Mouse wheel and middle-button drag.
            canvas.HandlerChanged += (s, e) =>
            {
                if (canvas?.Handler?.PlatformView is UIElement uiElement)
                {
                    uiElement.IsTabStop = true; // Make focusable
                    uiElement.PointerWheelChanged += OnPointerWheelChanged;
                    uiElement.PointerPressed += OnPointerPressed;
                    uiElement.PointerMoved += OnPointerMoved;
                    uiElement.PointerReleased += OnPointerReleased;
                    uiElement.KeyDown += OnKeyDown;
                    uiElement.KeyUp += OnKeyUp;

                    // Give focus.
                    uiElement.Focus(FocusState.Programmatic);
                }
            };
#endif
        }

        protected override void OnDetachingFrom(SKCanvasView canvas)
        {
            canvas.GestureRecognizers.Remove(panGesture);
            canvas.GestureRecognizers.Remove(pinchGesture);
#if WINDOWS
            if (canvas?.Handler?.PlatformView is UIElement uiElement)
            {
                uiElement.PointerWheelChanged -= OnPointerWheelChanged;
                uiElement.PointerPressed -= OnPointerPressed;
                uiElement.PointerMoved -= OnPointerMoved;
                uiElement.PointerReleased -= OnPointerReleased;
                uiElement.KeyDown -= OnKeyDown;
                uiElement.KeyUp -= OnKeyUp;
            }
#endif
            if (canvas != null)
            {
                base.OnDetachingFrom(canvas);
            }
        }

#if WINDOWS
        private void OnPointerWheelChanged(object? sender, PointerRoutedEventArgs e)
        {
            if (canvasView != null)
            {
                // Get wheel delta (positve for zoom in, negative for zoom out).
                var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
                var zoomFactor = delta > 0 ? 1.1f : 0.9f;

                // Pointer position relative to canvas.
                if (sender != null)
                {
                    var pointer = e.GetCurrentPoint((UIElement)sender)?.Position;

                    if (pointer.HasValue)
                    {
                        var x = (float)pointer.Value.X;
                        var y = (float)pointer.Value.Y;

                        CanvasInteractionService.UpdateWheelZoom(zoomFactor, new SKPoint(x, y));
                        canvasView.InvalidateSurface();
                    }
                }

                e.Handled = true; // Mark the event as handled to prevent further propagation.
            }
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var props = e.GetCurrentPoint((UIElement)sender).Properties;
            if (props.IsMiddleButtonPressed || (isSpaceDown && props.IsLeftButtonPressed))
            {
                isMousePanning = true;
                var pos = e.GetCurrentPoint((UIElement)sender).Position;
                var startPoint = new SKPoint((float)pos.X, (float)pos.Y);

                // Store the start point for delta calculation
                mousePanStart = startPoint;

                // Delegate to service
                CanvasInteractionService.StartPan(CanvasInteractionService.CanvasTranslate);
                ((UIElement)sender).CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (canvasView != null && isMousePanning)
            {
                var pos = e.GetCurrentPoint((UIElement)sender).Position;
                var currentPoint = new SKPoint((float)pos.X, (float)pos.Y);
                var delta = new SKPoint(currentPoint.X - mousePanStart.X, currentPoint.Y - mousePanStart.Y);
                CanvasInteractionService.UpdatePan(delta);
                canvasView.InvalidateSurface();
                e.Handled = true;
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isMousePanning = false;
            ((UIElement)sender).ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                isSpaceDown = true;
            }
        }

        private void OnKeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                isSpaceDown = false;
            }
        }
#endif
    }
}
