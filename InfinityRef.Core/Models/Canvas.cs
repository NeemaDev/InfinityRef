using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfinityRef.Core.Models
{
    public class Canvas
    {
        private string Name { get; set; }
        private SKMatrix Transform { get; set; }
        private List<Layer> Layers { get; set; } = new();

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
