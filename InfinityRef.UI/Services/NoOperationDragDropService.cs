using InfinityRef.UI.Interfaces;

namespace InfinityRef.UI.Services
{
    public class NoOperationDragDropService : IDragDropService
    {
        public Task<(bool IsValid, string Source)> AnalyzeDragOverAsync(DragEventArgs e)
        {
            throw new NotImplementedException();
        }

        public Task<(bool Success, byte[]? ImageData)> HandleDropAsync(DropEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
