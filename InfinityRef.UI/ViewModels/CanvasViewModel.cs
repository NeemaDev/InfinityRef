using InfinityRef.Core.Interfaces;
using InfinityRef.Core.Models;
using SkiaSharp;

namespace InfinityRef.UI.ViewModels
{
    public class CanvasViewModel
    {
        private Canvas canvas;
        private ILayerStackService layerStackService;

        public CanvasViewModel(Canvas canvas, ILayerStackService layerStackService)
        {
            this.canvas = canvas;
            this.layerStackService = layerStackService;
        }


        public int LayerCount => canvas.Layers.Count;
        public List<Layer> Layers => canvas.Layers;

        public void AddLayer(Layer layer)
        {
            canvas.Layers.Add(layer);
        }

        public Layer? GetLayer(SKPoint point)
         => layerStackService.HitTest(point);

        public void ClearSelection()
            => layerStackService.ClearSelection();

        public void RemoveLayer(Layer layer)
        {
            canvas.Layers.Remove(layer);
        }

        public void UpdateBounds()
            => canvas.UpdateBounds();

        public void UpdateBounds(float x, float y, float width, float height)
         => canvas.UpdateBounds(x, y, width, height);

        public void ClearLayerStack()
            => layerStackService.ClearStack();

        public void AddLayerBounds((Layer layer, SKRect bounds) layerBounds)
            => layerStackService.AddLayer(layerBounds);
    }
}
