using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using InfinityRef.UI.Interfaces;
using InfinityRef.UI.Services;
using InfinityRef.UI.ViewModels;
using SkiaSharp;

namespace InfinityRef
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel mainViewModel;
        private readonly IDragDropService dragDropService;
        private readonly INavigationService navigationService;
        private readonly CanvasInteractionService canvasInteractionService;
        private Dictionary<long, SKPoint> activeTouches = new Dictionary<long, SKPoint>();
        private bool isTouchPanning = false;

        private List<(Layer layer, SKRect bounds)> hitTestBuffer = new List<(Layer layer, SKRect bounds)>();
        private SKPoint lastTapPoint;

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
                IsAntialias = true,
            };

            using var typeface = SKTypeface.Default;
            using var font = new SKFont(typeface, textSize);

            var align = SKTextAlign.Left;

            // -- horizontal ticks + labels along the top edge --
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

            // -- vertical ticks + labels along the left edge --
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
    }
}
