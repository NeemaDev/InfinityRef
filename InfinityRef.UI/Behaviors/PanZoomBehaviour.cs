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
                if (e.StatusType == GestureStatus.Started)
                {
                    CanvasInteractionService.StartPan(CanvasInteractionService.CanvasTranslate);
                }
                else if (e.StatusType == GestureStatus.Running)
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
#endif
    }
}
