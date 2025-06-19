using SkiaSharp;

namespace InfinityRef.Core.Models
{
    public class Canvas
    {
        private SKMatrix Transform { get; set; }
        private List<Layer> Layers { get; set; } = new();

        public int LayerCount => Layers.Count;

        public void AddLayer(Layer layer)
        {
            Layers.Add(layer);
        }

        public void RemoveLayer(Layer layer)
        {
            Layers.Remove(layer);
        }
    }
}
