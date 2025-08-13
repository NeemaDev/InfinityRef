namespace InfinityRef.Core.Models
{
    public class Canvas
    {
        public double MinX { get; set; } = 0;
        public double MaxX { get; set; } = 0;
        public double MinY { get; set; } = 0;
        public double MaxY { get; set; } = 0;

        public List<Layer> Layers { get; } = new List<Layer>();

        public void UpdateBounds()
        {
            if (Layers.Count != 0)
            {
                MinX = Layers.Min(l => l.Position.X);
                MinY = Layers.Min(l => l.Position.Y);
                MaxX = Layers.Max(l => l.Position.X);
                MaxY = Layers.Max(l => l.Position.Y);
            }
            else
            {
                MinX = MinY = MaxX = MaxY = 0;
            }
        }

        public void UpdateBounds(float x, float y, float width, float height)
        {
            MinX = x < MinX ? x : MinX;
            MinY = y < MinY ? y : MinY;
            MaxX = x + width > MaxX ? x + width : MaxX;
            MaxY = y + height > MaxY ? y + height : MaxY;
        }
    }
}
