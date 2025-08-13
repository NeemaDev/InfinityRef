namespace InfinityRef.Core.Models
{
    public abstract class Layer
    {
        public Position2D Position { get; set; }
        public Dimension Dimension { get; set; }
        public bool IsSelected { get; set; }
    }
}
