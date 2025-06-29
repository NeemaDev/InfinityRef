using InfinityRef.Core.Models;
using InfinityRef.UI.Interfaces;

namespace InfinityRef.UI.Services
{
    public class NoOperationDragDropService : IDragDropService
    {
        public Task<(bool IsValid, string Source)> AnalyzeDragOverAsync(DragEventArgs e)
        {
            throw new NotImplementedException();
        }

        Task<(bool Success, byte[]? ImageData, Position2D DropPoint)> IDragDropService.HandleDropAsync(DropEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
