using InfinityRef.UI.Interfaces;
using InfinityRef.UI.ViewModels;
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
        private readonly IDragDropService dragDropSercvice;


        public MainPage(MainViewModel viewModel, IDragDropService dragDropService)
        {
            InitializeComponent();
            BindingContext = viewModel;
            mainViewModel = viewModel;
            this.dragDropSercvice = dragDropService;
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
            var result = await dragDropSercvice.HandleDropAsync(dropEvent);

            if (!result.Success || result.Bitmap is null)
            {
                return;
            }
            else
            {
                await mainViewModel.HandleDrop(result.Bitmap);
                CanvasView.InvalidateSurface();
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
            var result = await dragDropSercvice.AnalyzeDragOverAsync(dragEvent);

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

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {

        }
    }
}
