using InfinityRef.Core.Models;
using SkiaSharp;

namespace InfinityRef.Core.Interfaces
{
    public interface ILayerStackService
    {
        void AddLayer((Layer layer, SKRect bounds) layerBounds);
        void RemoveLayer(Layer layer);
        void ReplaceStack(IEnumerable<(Layer layer, SKRect bound)> buffer);
        Layer? HitTest(SKPoint point);
        void ClearSelection();
        void ClearStack();
    }
}
