using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using InfinityRef.UI.Interfaces;
using InfinityRef.UI.Rendering;
using InfinityRef.UI.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Maui;
#if WINDOWS
  using Windows.Storage;                           // StorageFile
  using System.Runtime.InteropServices;
  using System.Runtime.InteropServices.WindowsRuntime;
  using Microsoft.Maui.ApplicationModel.DataTransfer;
  using System.Text.RegularExpressions;

#elif MACCATALYST
  // macOS UIHostingController-based drag‐drop gives UniformTypeIdentifiers
  using UniformTypeIdentifiers;
#endif

namespace InfinityRef
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel mainViewModel;
        private readonly IDragDropService dragDropService;
        private readonly INavigationService navigationService;

        List<(Layer layer, SKRect bounds)> hitTestBuffer = new();
        SKPoint lastTapPoint;

        public MainPage(MainViewModel viewModel, INavigationService navigationService, IDragDropService dragDropService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            mainViewModel = viewModel;
            this.dragDropService = dragDropService;
            this.navigationService = navigationService;

            // Subscribe to canvas changes.
            navigationService.ActiveCanvasChanged += (_, __) => HookCanvas(navigationService.ActiveCanvas);

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

            if (!result.Success || result.ImageData is null)
            {
                return;
            }
            else
            {
                await mainViewModel.HandleDrop(result.ImageData, result.DropPoint);
                CanvasView.InvalidateSurface(); // Triggers skisharps repaint routine.
            }

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

            // Draw each layer and stash its rectangle for hit testing.
            foreach (var layer in mainViewModel.CurrentCanvas.Layers)
            {
                var bounds = LayerRenderer.Draw(layer, canvas);
                hitTestBuffer.Add((layer, bounds));
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
