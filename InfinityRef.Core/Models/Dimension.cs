namespace InfinityRef.Core.Models
{
    public struct Dimension
    {
        public Dimension(float width, float height)
        {
            Width = width;
            Height = height;
        }

        public float Width { get; }
        public float Height { get; }

        public override string ToString() => $"({Width}, {Height})";
    }
}
