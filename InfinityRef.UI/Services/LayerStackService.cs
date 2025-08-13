using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using SkiaSharp;

namespace InfinityRef.UI.Services
{
    public class LayerStackService : ILayerStackService
    {
        private List<(Layer layer, SKRect bounds)> stack = new List<(Layer layer, SKRect bounds)>();

        public void AddLayer((Layer layer, SKRect bounds) layerBounds)
        {
            stack.Add(layerBounds);
        }

        public void ClearSelection()
        {
            stack.ForEach(t => t.layer.IsSelected = false);
        }

        public void ClearStack()
        {
            stack.Clear();
        }

        public Layer? HitTest(SKPoint point)
        {
            var layerHits = stack.Where(tuple => tuple.bounds.Contains(point));

            if (layerHits.Any())
            {
                // Return the last layer hit, which is the topmost layer in the stack.
                return layerHits.Last().layer;
            }

            return null;
        }

        public void RemoveLayer(Layer layer)
        {
            var layerBounds = stack.Find(layerbounds => layerbounds.layer == layer);

            if (layerBounds.layer != null)
            {
                stack.Remove(layerBounds);
            }
        }

        public void ReplaceStack(IEnumerable<(Layer layer, SKRect bound)> stack)
        {
            this.stack = stack.ToList();
        }
    }
}
