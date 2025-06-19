using InfinityRef.UI.Interfaces;
using SkiaSharp;

namespace InfinityRef.UI.Services
{
    public class NoOperationDragDropService : IDragDropService
    {
        public Task<(bool IsValid, string Source)> AnalyzeDragOverAsync(DragEventArgs e)
        {
            throw new NotImplementedException();
        }

        public Task<(bool Success, SKBitmap? Bitmap)> HandleDropAsync(DropEventArgs _)
        {
            return Task.FromResult<(bool, SKBitmap?)>((false, null));
        }
    }
}
