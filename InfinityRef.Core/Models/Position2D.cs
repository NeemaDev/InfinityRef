namespace InfinityRef.Core.Models
{
    public struct Position2D
    {
        public Position2D(float x, float y)
        {
            X = x;
            Y = y;
            IsEmpty = false;
        }

        public Position2D(float x, float y, bool empty)
        {
            X = x;
            Y = y;
            IsEmpty = empty;
        }

        public float X { get; }
        public float Y { get; }
        public bool IsEmpty { get; }

        public static Position2D Empty => new Position2D(0, 0, true);

        public override string ToString() => $"({X}, {Y})";
    }
}
