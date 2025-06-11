using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfinityRef.Core.Models
{
    public abstract class Layer
    {
        private string Id { get; set; }
        private SKRect Bounds { get; set; }
        private bool IsVisible { get; set; }

        public virtual void Draw(SKCanvas canvas)
        {

        }

        public bool HitTest(SKPoint point)
        {
            return Bounds.Contains(point);
        }
    }
}
