using InfinityRef.Core.Models;
using InfinityRef.UI.Interfaces;
using InfinityRef.UI.ViewModels;
using SkiaSharp;

namespace InfinityRef.UI.Services
{
    public class DragDropService : IDragDropService
    {
        private readonly IDragDropService platformService;
        private readonly MainViewModel mainViewModel;
        private readonly CanvasInteractionService canvasInteractionService;

        public DragDropService(IDragDropService platformService,
                                  MainViewModel mainViewModel,
                                  CanvasInteractionService canvasInteractionService)
        {
            this.platformService = platformService;
            this.mainViewModel = mainViewModel;
            this.canvasInteractionService = canvasInteractionService;
        }

        public async Task<(bool IsValid, string Source)> AnalyzeDragOverAsync(DragEventArgs e)
        {
            // Delegate to platform-specific service
            return await platformService.AnalyzeDragOverAsync(e);
        }

        public async Task<(bool Success, byte[]? ImageData, Position2D DropPoint)> HandleDropAsync(DropEventArgs e)
        {
            // Delegate to platform-specific service
            var result = await platformService.HandleDropAsync(e);

            if (!result.Success || result.ImageData is null)
            {
                return result;
            }

            // Convert drop point to canvas coordinates
            var devicePoint = new SKPoint(result.DropPoint.X, result.DropPoint.Y);
            var canvasPoint = canvasInteractionService.DeviceToCanvas(devicePoint);

            // Expand canvas bounds if needed
            var canvas = mainViewModel.CurrentCanvas;
            if (canvas.Layers.Count > 0)
            {
                canvas.UpdateBounds();
            }

            using var bitmap = SKBitmap.Decode(result.ImageData);
            float imgWidth = bitmap?.Width ?? 0;
            float imgHeight = bitmap?.Height ?? 0;

            canvas.UpdateBounds(canvasPoint.X, canvasPoint.Y, imgWidth, imgHeight);

            // Place the image at the drop point in canvas coordinates
            await mainViewModel.HandleDrop(result.ImageData, new Position2D(canvasPoint.X, canvasPoint.Y));

            // Return the result with the converted drop point
            return (true, result.ImageData, new Position2D(canvasPoint.X, canvasPoint.Y));
        }
        public bool IsSupportedType(string path)
        {
            return platformService.IsSupportedType(path);
        }

    }
}
