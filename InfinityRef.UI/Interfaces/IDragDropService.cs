using InfinityRef.Core.Models;

namespace InfinityRef.UI.Interfaces
{
    public interface IDragDropService
    {
        Task<(bool Success, byte[]? ImageData, Position2D DropPoint)> HandleDropAsync(DropEventArgs e);
        Task<(bool IsValid, string Source)> AnalyzeDragOverAsync(DragEventArgs e);


        static readonly HashSet<string> SupportedImageFormats = new()
        {
            "png", "jpeg", "jpg", "gif", "bmp", "webp"
        };

        bool IsSupportedType(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                return SupportedImageFormats.Contains(ext);
            }
            return false;
        }
    }
}
