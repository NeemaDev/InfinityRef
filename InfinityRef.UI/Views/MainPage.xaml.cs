using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using InfinityRef.UI.Interfaces;
using InfinityRef.UI.Rendering;
using InfinityRef.UI.Services;
using InfinityRef.UI.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
#if WINDOWS
  using Windows.Storage;                           // StorageFile
  using System.Runtime.InteropServices;
  using System.Runtime.InteropServices.WindowsRuntime;
  using Microsoft.Maui.ApplicationModel.DataTransfer;
  using System.Text.RegularExpressions;
  using Microsoft.UI.Input;
#elif MACCATALYST
  // macOS UIHostingController-based drag‐drop gives UniformTypeIdentifiers
  using UniformTypeIdentifiers;
#endif

namespace InfinityRef
{
    public partial class MainPage : ContentPage
    {
#if WINDOWS
        private bool isMousePanning = false;
        private bool isSpaceDown = false;
        private SKPoint mousePanStart;
        private SKPoint mousePanOrigin;
#endif

        private readonly MainViewModel mainViewModel;
        private readonly IDragDropService dragDropService;
        private readonly INavigationService navigationService;
        private readonly CanvasInteractionService canvasInteractionService;
        private Dictionary<long, SKPoint> activeTouches = new();
        private bool isTouchPanning = false;

        List<(Layer layer, SKRect bounds)> hitTestBuffer = new();
        SKPoint lastTapPoint;

        public MainPage(MainViewModel viewModel,
                        INavigationService navigationService,
                        DragDropService dragDropService,
                        CanvasInteractionService canvasInteractionService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            mainViewModel = viewModel;
            this.dragDropService = dragDropService;
            this.navigationService = navigationService;
            this.canvasInteractionService = canvasInteractionService;

            // Subscribe to canvas changes.
            navigationService.ActiveCanvasChanged += (_, __) => HookCanvas(navigationService.ActiveCanvas);
            CanvasView.HandlerChanged += OnHandlerChanged; // Triggered when view is created, controll is added/removed, orientation change, theme change, etc.

            // Set the first canvas.
            HookCanvas(navigationService.ActiveCanvas);
        }

        /// <summary>
        /// Associates the specified <see cref="Canvas"/> with the application, setting it as the active canvas.
        /// </summary>
        /// <remarks>This method updates the application's state to use the provided <see cref="Canvas"/> 
        /// as the active canvas. Ensure that the <paramref name="canvas"/> is properly initialized before calling this
        /// method.</remarks>
        /// <param name="canvas">The <see cref="Canvas"/> to be set as the active canvas. This parameter cannot be <see langword="null"/>.</param>
        private void HookCanvas(Canvas canvas)
        {
            mainViewModel.SetActiveCanvas(canvas);
        }

        /// <summary>
        /// Handles the drop event and processes the dropped data asynchronously.
        /// </summary>
        /// <remarks>This method uses the drag-and-drop service to process the dropped data. If the
        /// operation is successful and a valid bitmap is provided, the bitmap is passed to the main view model for
        /// further handling, and the canvas view is invalidated to refresh its display.</remarks>
        /// <param name="sender">The source of the drop event.</param>
        /// <param name="dropEvent">The event data containing information about the drop operation.</param>
        private async void OnDrop(object sender, DropEventArgs dropEvent)
        {
            var result = await dragDropService.HandleDropAsync(dropEvent);
            CanvasView.InvalidateSurface();
        }

        /// <summary>
        /// Handles the drag-over event to provide feedback about whether the dragged content can be dropped.
        /// </summary>
        /// <remarks>This method analyzes the dragged content and updates the UI to indicate whether the
        /// drop is valid. On Windows, it modifies the drag UI to display a caption and glyph based on the validity of
        /// the dragged content.</remarks>
        /// <param name="sender">The source of the event, typically the UI element where the drag-over is occurring.</param>
        /// <param name="dragEvent">The <see cref="DragEventArgs"/> containing data about the drag-over operation.</param>
        private async void DragOver(object sender, DragEventArgs dragEvent)
        {
            var result = await dragDropService.AnalyzeDragOverAsync(dragEvent);

#if WINDOWS
            try
            {
                var dragArgs = dragEvent.PlatformArgs?.DragEventArgs;
                if (dragArgs != null)
                {
                    var ui = dragArgs.DragUIOverride;
                    if(ui != null)
                    {
                        ui.Caption = result.IsValid ? "Drop the image!" : "Unsupported format";
                        ui.IsCaptionVisible = !result.IsValid;
                        ui.IsGlyphVisible = result.IsValid;
                    }
                }
            }catch (COMException)
            {
                // Silent catch. Occurs too often.
            }
#endif
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {

        }

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

        private void DrawRulers(SKCanvas canvas, int width, int height)
        {
            const float spacing = 50;   // 50px between ticks
            const float tickLen = 10;   // tick length in px
            const float textSize = 12;   // font size in px
            const float textGap = 2;    // gap between tick end and text

            using var tickPaint = new SKPaint
            {
                Color = SKColors.DarkGray,
                StrokeWidth = 1,
                IsAntialias = true
            };

            using var typeface = SKTypeface.Default;
            using var font = new SKFont(typeface, textSize);

            var align = SKTextAlign.Left;

            //  -- horizontal ticks + labels along the top edge --
            for (float x = 0; x <= width; x += spacing)
            {
                // draw the tick
                canvas.DrawLine(x, 0, x, tickLen, tickPaint);

                // label = the pixel-x coordinate
                var label = ((int)x).ToString();

                // draw centered under the tick
                float textX = x;
                float textY = tickLen + textGap + textSize;
                canvas.DrawText(label, textX, textY, align, font, tickPaint);
            }

            //  -- vertical ticks + labels along the left edge --
            // align text to left so we offset by half the text width
            for (float y = 0; y <= height; y += spacing)
            {
                // draw the tick
                canvas.DrawLine(0, y, tickLen, y, tickPaint);

                // label = the pixel-y coordinate
                var label = ((int)y).ToString();

                // draw beside the tick, vertically centered on the line
                float textX = tickLen + textGap;
                // we offset baseline so text sits −(textSize/2) above the y
                float textY = y + (textSize / 2);
                canvas.DrawText(label, textX, textY, align, font, tickPaint);
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

        /// <summary>
        /// Handles changes to the handler associated with the <see cref="CanvasView"/>.
        /// </summary>
        /// <remarks>This method is triggered when the handler for the <see cref="CanvasView"/> changes.
        /// On Windows, it attaches the <see langword="PointerWheelChanged"/> event to the platform-specific <see
        /// cref="Microsoft.UI.Xaml.UIElement"/> associated with the handler.</remarks>
        /// <param name="sender">The source of the event. This parameter may be <see langword="null"/>.</param>
        /// <param name="e">An <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnHandlerChanged(object? sender, EventArgs e)
        {
            // On Windows, the PlatformView is a WinUI UIElement
#if WINDOWS
            if (CanvasView?.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement uiElem)
            {
                uiElem.PointerWheelChanged += OnPointerWheelChanged;
                uiElem.PointerPressed += OnPointerPressed;
                uiElem.PointerReleased += OnPointerReleased;
                uiElem.PointerMoved += OnPointerMoved;
                uiElem.KeyDown += OnKeyDown;
                uiElem.KeyUp += OnKeyUp;

                // Make focusable and set focus
                uiElem.IsTabStop = true;
                uiElem.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                uiElem.PointerEntered += (s, args) =>
                {
                    uiElem.Focus(Microsoft.UI.Xaml.FocusState.Pointer);
                };
            }
#endif
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
#endif

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
        /// Handles a hit test operation to determine whether a user tap intersects with any layers.
        /// </summary>
        /// <remarks>This method checks the layers in reverse order of their addition to determine if the
        /// tap intersects with their bounds. If a layer is hit, it is selected, and the canvas is refreshed to reflect
        /// the selection. If no layers are hit, all selections are cleared, and the canvas is refreshed.</remarks>
        /// <param name="lastTapPoint">The point where the user last tapped, represented as an <see cref="SKPoint"/>.</param>
        private void HandleHitTest(SKPoint lastTapPoint)
        {
            var hit = false;

            foreach (var (layer, bounds) in hitTestBuffer.AsEnumerable().Reverse())
            {
                if (bounds.Contains(lastTapPoint))
                {
                    SelectLayer(layer);
                    hit = true;
                    CanvasView.InvalidateSurface(); // Refresh the canvas to reflect the selection.
                    return;
                }
            }

            if (!hit)
            {
                DeselectAll();
                CanvasView.InvalidateSurface();
            }
        }

        private void SelectLayer(Layer layer)
        {
            layer.IsSelected = true;
            mainViewModel.CurrentCanvas.Layers.Where(l => l != layer) // Deselect all other layers
                .ToList()
                .ForEach(l => l.IsSelected = false);
        }

        private void DeselectAll()
        {
            foreach (var (layer, _) in hitTestBuffer)
            {
                layer.IsSelected = false;
            }
        }

#if WINDOWS
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
